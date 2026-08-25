using System;
using System.Threading.Tasks;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Librariann.Models.Mapping;
using Hangfire;
using Librariann.API.Database;
using Librariann.API.Services;
using Librariann.API.Services.Reading;
using Librariann.Common.Extensions;
using Librariann.Models.DTOs;
using Librariann.Models.Entities;
using Librariann.Models.Entities.Progress;
using Librariann.Models.Entities.User;
using Librariann.Services.Comparators;
using Librariann.Services.Extensions;
using Librariann.Services.Scanner;
using Microsoft.Extensions.Logging;

namespace Librariann.Services;

/// <summary>
/// All APIs are for Tachiyomi extension and app. They have hacks for our implementation and should not be used for any
/// other purposes.
/// </summary>
public class TachiyomiService(
    IUnitOfWork unitOfWork,
    ILogger<TachiyomiService> logger,
    IReaderService readerService)
    : ITachiyomiService
{
    private static readonly CultureInfo EnglishCulture = CultureInfo.CreateSpecificCulture("en-US");

    public async Task<TachiyomiChapterDto?> GetLatestChapter(int seriesId, int userId, CancellationToken ct = default)
    {
        var currentChapter = await readerService.GetContinuePoint(seriesId, userId);

        var prevChapterId =
            await readerService.GetPrevChapterIdAsync(seriesId, currentChapter.VolumeId, currentChapter.Id, userId);

        // If prevChapterId is -1, this means either nothing is read or everything is read.
        if (prevChapterId == -1)
        {
            var series = await unitOfWork.SeriesRepository.GetSeriesDtoByIdAsync(seriesId, userId, ct);
            var userHasProgress = series.PagesRead != 0 && series.PagesRead <= series.Pages;

            // If the user doesn't have progress, then return null, which the extension will catch as 204 (no content) and report nothing as read
            if (!userHasProgress) return null;

            // Else return the max chapter to Tachiyomi so it can consider everything read
            var volumes = (await unitOfWork.VolumeRepository.GetVolumes(seriesId, ct)).ToImmutableList();
            var looseLeafChapterVolume = volumes.GetLooseLeafVolumeOrDefault();
            if (looseLeafChapterVolume == null)
            {
                // Matches the original plain (non-ProjectTo) Map<ChapterDto> call: per-user progress fields were
                // never actually parameterized for in-memory Map() calls, only ProjectTo, so this always used
                // userId 0 regardless of the real caller. Harmless here since only MinNumber is read below.
                var chapterToConvert = volumes
                    [^1].Chapters
                    .OrderBy(c => c.MinNumber, ChapterSortComparerDefaultFirst.Default)
                    .Last();
                // GetVolumes() above only Includes Chapters/Files, not UserProgress (no default initializer on
                // the entity), so default it before mapping to avoid an NRE in ToChapterDto's compiled delegate.
                // Safe: userId 0 never matches a real user anyway, and this Chapter isn't saved afterward.
                chapterToConvert.UserProgress ??= new List<AppUserProgress>();
                var volumeChapter = chapterToConvert.ToChapterDto(0);

                if (volumeChapter.MinNumber.Is(Parser.LooseLeafVolumeNumber))
                {
                    var volume = volumes.First(v => v.Id == volumeChapter.VolumeId);
                    return CreateTachiyomiChapterDto(volume.MinNumber);
                }

                return CreateTachiyomiChapterDto(volumeChapter.MinNumber);
            }

            var lastChapter = looseLeafChapterVolume.Chapters
                .OrderBy(c => c.MinNumber, ChapterSortComparerDefaultLast.Default)
                .Last();

            return lastChapter.ToTachiyomiChapterDto();
        }

        // There is progress, we now need to figure out the highest volume or chapter and return that.
        var prevChapter = (await unitOfWork.ChapterRepository.GetChapterDtoAsync(prevChapterId, userId, ct))!;

        var volumeWithProgress = (await unitOfWork.VolumeRepository.GetVolumeDtoAsync(prevChapter.VolumeId, userId, ct))!;
        // We only encode for single-file volumes
        if (!volumeWithProgress.IsLooseLeaf() && volumeWithProgress.Chapters.Count == 1)
        {
            // The progress is on a volume, encode it as a fake chapterDTO
            return CreateTachiyomiChapterDto(volumeWithProgress.MinNumber);
        }

        // Progress is just on a chapter, return as is
        return prevChapter.ToTachiyomiChapterDto();
    }

    private static TachiyomiChapterDto CreateTachiyomiChapterDto(float number)
    {
        return new TachiyomiChapterDto()
        {
            // Use R to ensure that localization of underlying system doesn't affect the stringification
            // https://docs.microsoft.com/en-us/globalization/locale/number-formatting-in-dotnet-framework
            Number = (number / 10_000f).ToString("R", EnglishCulture),
            Files = new List<MangaFileDto>()
        };
    }

    public async Task<bool> MarkChaptersUntilAsRead(AppUser user, int seriesId, float chapterNumber, bool generateReadingSessions,
        CancellationToken ct = default)
    {

        logger.LogDebug("Marking chapters until {ChapterNumber} for series {SeriesId} for user {UserId}",
            chapterNumber, seriesId, user.Id);

        user.Progresses ??= [];

        var chapters = chapterNumber switch
        {
            // When Tachiyomi sync's progress, if there is no current progress in Tachiyomi, 0.0f is sent.
            // Due to the encoding for volumes, this marks all chapters in volume 0 (loose chapters) as read.
            // Hence we catch and return early, so we ignore the request.
            0.0f => [],
            // This is a hack to track volume number. We need to map it back by x10,000
            < 1.0f => await GetChaptersUntilVolume(seriesId, int.Parse($"{(int)(chapterNumber * 10_000)}", EnglishCulture)),
            _ => await GetChaptersUntilChapter(seriesId, chapterNumber)
        };

        if (chapters.Count == 0) return true;

        var chapterIds = chapters.Select(c => c.Id).ToList();

        var progressDictionary = await unitOfWork.AppUserProgressRepository
            .GetUserProgressForChaptersByChapters(user.Id, seriesId, chapterIds, ct);

        await readerService.MarkChaptersAsRead(user, seriesId, chapters);

        if (generateReadingSessions)
        {
            BackgroundJob.Enqueue<IReadingSessionService>(s
                => s.GenerateReadingSessionForChapters(user.Id, seriesId, progressDictionary, CancellationToken.None));
        }

        try {
            if (!unitOfWork.HasChanges()) return true;
            if (await unitOfWork.CommitAsync(ct)) return true;
        } catch (Exception ex) {
            logger.LogError(ex, "There was an error saving progress from tachiyomi");
            await unitOfWork.RollbackAsync(ct);
        }
        return false;
    }

    private async Task<List<Chapter>> GetChaptersUntilVolume(int seriesId, int volumeNumber)
    {
        var volumes = await unitOfWork.VolumeRepository.GetVolumesForSeriesAsync([seriesId], true);

        return volumes
            .Where(v => v.MinNumber <= volumeNumber && v.MinNumber > 0)
            .OrderBy(v => v.MinNumber)
            .SelectMany(v => v.Chapters)
            .ToList();
    }

    private async Task<List<Chapter>> GetChaptersUntilChapter(int seriesId, float chapterNumber)
    {
        var volumes = await unitOfWork.VolumeRepository.GetVolumesForSeriesAsync([seriesId], true);

        return volumes
            .OrderBy(v => v.MinNumber)
            .SelectMany(v => v.Chapters)
            .Where(c => !c.IsSpecial && c.MaxNumber <= chapterNumber)
            .OrderBy(c => c.MinNumber)
            .ToList();
    }


}

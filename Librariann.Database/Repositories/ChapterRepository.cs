using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Repositories;
using Librariann.Common.Extensions;
using Librariann.Database.Extensions;
using Librariann.Models.Constants;
using Librariann.Models.DTOs;
using Librariann.Models.DTOs.LibrariannPlus.Scrobble;
using Librariann.Models.DTOs.Metadata;
using Librariann.Models.DTOs.Reader;
using Librariann.Models.DTOs.SeriesDetail;
using Librariann.Models.Entities;
using Librariann.Models.Entities.Enums;
using Librariann.Models.Entities.Metadata;
using Librariann.Models.Mapping;
using Librariann.Models.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Librariann.Database.Repositories;


public class ChapterRepository(DataContext context) : IChapterRepository
{
    public void Update(Chapter chapter)
    {
        context.Entry(chapter).State = EntityState.Modified;
    }

    public void Remove(Chapter chapter)
    {
        context.Chapter.Remove(chapter);
    }

    public void Remove(IList<Chapter> chapters)
    {
        context.Chapter.RemoveRange(chapters);
    }

    public async Task<IList<Chapter>> GetChaptersByIdsAsync(IList<int> chapterIds,
        ChapterIncludes includes = ChapterIncludes.None, CancellationToken ct = default)
    {
        return await context.Chapter
            .Where(c => chapterIds.Contains(c.Id))
            .Includes(includes)
            .AsSplitQuery()
            .ToListAsync(ct);
    }

    /// <summary>
    /// Populates a partial IChapterInfoDto
    /// </summary>
    /// <returns></returns>
    public async Task<IChapterInfoDto?> GetChapterInfoDtoAsync(int chapterId, CancellationToken ct = default)
    {
        var data = await context.Chapter
            .Where(c => c.Id == chapterId)
            .Join(context.Volume, c => c.VolumeId, v => v.Id, (chapter, volume) => new
            {
                ChapterNumber = chapter.MinNumber,
                VolumeNumber = volume.Name,
                VolumeId = volume.Id,
                chapter.IsSpecial,
                chapter.TitleName,
                volume.SeriesId,
                chapter.Pages,
            })
            .Join(context.Series, data => data.SeriesId, series => series.Id, (data, series) => new
            {
                data.ChapterNumber,
                data.VolumeNumber,
                data.VolumeId,
                data.IsSpecial,
                data.SeriesId,
                data.Pages,
                data.TitleName,
                SeriesFormat = series.Format,
                SeriesName = series.Name,
                series.LibraryId,
                LibraryType = series.Library.Type
            })
            .AsNoTracking()
            .AsSplitQuery()
            .SingleOrDefaultAsync(ct);

        if (data == null) return null;

        return new ChapterInfoDto
        {
            // Use at most 5 decimal points
            ChapterNumber = data.ChapterNumber.ToString("0.#####", CultureInfo.InvariantCulture),
            VolumeNumber = data.VolumeNumber + string.Empty,
            VolumeId = data.VolumeId,
            IsSpecial = data.IsSpecial,
            SeriesId = data.SeriesId,
            SeriesFormat = data.SeriesFormat,
            SeriesName = data.SeriesName,
            LibraryId = data.LibraryId,
            Pages = data.Pages,
            ChapterTitle = data.TitleName,
            LibraryType = data.LibraryType
        };
    }

    public Task<int> GetChapterTotalPagesAsync(int chapterId, CancellationToken ct = default)
    {
        return context.Chapter
            .Where(c => c.Id == chapterId)
            .Select(c => c.Pages)
            .FirstOrDefaultAsync(ct);
    }
    public async Task<ChapterDto?> GetChapterDtoAsync(int chapterId, int userId, CancellationToken ct = default)
    {
        var chapter = await context.Chapter
            .Includes(ChapterIncludes.Files | ChapterIncludes.People)
            .Select(ChapterMapping.ToChapterDtoExpression(userId))
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == chapterId, ct);

        return chapter;
    }


    /// <summary>
    /// Returns non-tracked files for a given chapterId
    /// </summary>
    /// <param name="chapterId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<IList<MangaFile>> GetFilesForChapterAsync(int chapterId, CancellationToken ct = default)
    {
        return await context.MangaFile
            .Where(c => chapterId == c.ChapterId)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    /// <summary>
    /// Returns a Chapter for an id. Includes linked <see cref="MangaFile"/>s.
    /// </summary>
    /// <param name="chapterId"></param>
    /// <param name="includes"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<Chapter?> GetChapterAsync(int chapterId, ChapterIncludes includes = ChapterIncludes.Files,
        CancellationToken ct = default)
    {
        return await context.Chapter
            .Includes(includes)
            .OrderBy(c => c.SortOrder)
            .FirstOrDefaultAsync(c => c.Id == chapterId, ct);
    }

    /// <summary>
    /// Returns Chapters for a volume id.
    /// </summary>
    /// <param name="volumeId"></param>
    /// <param name="includes"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<IList<Chapter>> GetChaptersAsync(int volumeId, ChapterIncludes includes = ChapterIncludes.None,
        CancellationToken ct = default)
    {
        return await context.Chapter
            .Where(c => c.VolumeId == volumeId)
            .Includes(includes)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Returns the cover image for a chapter id.
    /// </summary>
    /// <param name="chapterId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<string?> GetChapterCoverImageAsync(int chapterId, CancellationToken ct = default)
    {
        return await context.Chapter
            .Where(c => c.Id == chapterId)
            .Select(c => c.CoverImage)
            .SingleOrDefaultAsync(ct);
    }

    public async Task<IList<string>> GetAllCoverImagesAsync(CancellationToken ct = default)
    {
        return (await context.Chapter
            .Select(c => c.CoverImage)
            .Where(t => !string.IsNullOrEmpty(t))
            .ToListAsync(ct))!;
    }

    public async Task<IList<Chapter>> GetAllChaptersWithCoversInDifferentEncoding(EncodeFormat format,
        CancellationToken ct = default)
    {
        var extension = format.GetExtension();
        return await context.Chapter
            .Where(c => !string.IsNullOrEmpty(c.CoverImage)  && !c.CoverImage.EndsWith(extension))
            .ToListAsync(ct);
    }

    /// <summary>
    /// Returns cover images for locked chapters
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<string>> GetCoverImagesForLockedChaptersAsync(CancellationToken ct = default)
    {
        return (await context.Chapter
            .Where(c => c.CoverImageLocked)
            .Select(c => c.CoverImage)
            .Where(t => !string.IsNullOrEmpty(t))
            .ToListAsync(ct))!;
    }

    /// <summary>
    /// Returns non-tracked files for a set of <paramref name="chapterIds"/>
    /// </summary>
    /// <param name="chapterIds">List of chapter Ids</param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<IList<MangaFile>> GetFilesForChaptersAsync(IReadOnlyList<int> chapterIds,
        CancellationToken ct = default)
    {
        return await context.MangaFile
            .Where(c => chapterIds.Contains(c.ChapterId))
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<long> GetFilesizeAsync(int chapterId, CancellationToken ct = default)
    {
        return await context.MangaFile
            .Where(c => c.ChapterId == chapterId)
            .SumAsync(c => c.Bytes, cancellationToken: ct);
    }

    public async Task<Dictionary<int, long>> GetFilesizesAsync(int userId, IList<int> chapterIds,
        CancellationToken ct = default)
    {
        var ageRestriction = await context.AppUser.GetUserAgeRestriction(userId, ct);
        var allowedLibraries = await context.Library.GetUserLibraries(userId).ToListAsync(ct);

        var filteredChapterIds = await context.Chapter
            .RestrictAgainstAgeRestriction(ageRestriction)
            .Where(c => allowedLibraries.Contains(c.Volume.Series.LibraryId))
            .Where(c => chapterIds.Contains(c.Id))
            .Select(c => c.Id)
            .ToListAsync(ct);

        return await filteredChapterIds.BatchToDictionaryAsync(50, batch =>
            context.MangaFile
                .Where(f => batch.Contains(f.ChapterId))
                .ToDictionaryAsync(f => f.ChapterId, f => f.Bytes, cancellationToken: ct));
    }

    /// <summary>
    /// Includes Volumes
    /// </summary>
    /// <param name="seriesId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public IQueryable<Chapter> GetChaptersForSeries(int seriesId, CancellationToken ct = default)
    {
        return context.Chapter
            .Where(c => c.Volume.SeriesId == seriesId)
            .OrderBy(c => c.SortOrder)
            .Include(c => c.Volume);
    }

    public async Task<IList<Chapter>> GetAllChaptersForSeries(int seriesId, CancellationToken ct = default)
    {
        return await context.Chapter
            .Where(c => c.Volume.SeriesId == seriesId)
            .OrderBy(c => c.SortOrder)
            .Include(c => c.Volume)
            .Include(c => c.People)
            .ThenInclude(cp => cp.Person)
            .ToListAsync(ct);
    }

    public async Task<int> GetAverageUserRating(int chapterId, int userId, CancellationToken ct = default)
    {
        var ratings = await context.AppUserChapterRating
            .Where(r => r.ChapterId == chapterId && r.HasBeenRated)
            .ToListAsync(ct);

        if (ratings.Count == 0 || (ratings.Count == 1 && ratings[0].AppUserId == userId))
        {
            return 0;
        }

        var avg = ratings.Average(r => (int?) r.Rating);
        return avg.HasValue ? (int) (avg.Value * 20) : 0;
    }

    public async Task<IList<UserReviewDto>> GetExternalChapterReviewDtos(int chapterId, CancellationToken ct = default)
    {
        return await context.Chapter
            .Where(c => c.Id == chapterId)
            .SelectMany(c => c.ExternalReviews)
            .Select(r => r.ToUserReviewDto())
            .ToListAsync(ct);
    }

    public async Task<IList<ExternalReview>> GetExternalChapterReview(int chapterId, CancellationToken ct = default)
    {
        return await context.Chapter
            .Where(c => c.Id == chapterId)
            .SelectMany(c => c.ExternalReviews)
            .ToListAsync(ct);
    }

    public async Task<IList<RatingDto>> GetExternalChapterRatingDtos(int chapterId, CancellationToken ct = default)
    {
        return await context.Chapter
            .Where(c => c.Id == chapterId)
            .SelectMany(c => c.ExternalRatings)
            .Select(RatingMapping.ToRatingDtoExpression)
            .ToListAsync(ct);
    }

    public async Task<IList<ExternalRating>> GetExternalChapterRatings(int chapterId, CancellationToken ct = default)
    {
        return await context.Chapter
            .Where(c => c.Id == chapterId)
            .SelectMany(c => c.ExternalRatings)
            .ToListAsync(ct);
    }

    public async Task<ChapterDto?> GetCurrentlyReadingChapterAsync(int seriesId, int userId, CancellationToken ct = default)
    {
        var chapterWithProgress = await context.AppUserProgresses
            .Where(p => p.AppUserId == userId)
            .Join(
                context.Chapter
                    .Include(c => c.Volume)
                    .Include(c => c.Files)
                    // Needed because .ToChapterDto(0) below reads c.UserProgress (no default initializer on the
                    // entity); without this Include it's null and NREs when the compiled mapping delegate runs
                    // in-memory. Harmless for the mapped value itself since userId 0 never matches a real user.
                    .Include(c => c.UserProgress),
                p => p.ChapterId,
                c => c.Id,
                (p, c) => new { Chapter = c, p.PagesRead }
            )
            .Where(x => x.Chapter.Volume.SeriesId == seriesId)
            .Where(x => x.Chapter.Volume.Number != ParserConstants.LooseLeafVolumeNumber)
            .Where(x => x.PagesRead > 0 && x.PagesRead < x.Chapter.Pages)
            .OrderBy(x => x.Chapter.Volume.Number)
            .ThenBy(x => x.Chapter.SortOrder)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (chapterWithProgress == null) return null;

        // Map chapter to DTO. Matches the original plain Map<ChapterDto> call (userId 0, immediately overwritten
        // below from the separately-joined progress value - plain Map() never gets real per-user parameterization).
        var dto = chapterWithProgress.Chapter.ToChapterDto(0);
        dto.PagesRead = chapterWithProgress.PagesRead;

        return dto;
    }

    public async Task<ChapterDto?> GetFirstChapterForSeriesAsync(int seriesId, int userId, CancellationToken ct = default)
    {
        // Get the chapter entity with proper ordering
        return await context.Chapter
            .Include(c => c.Volume)
            .Include(c => c.Files)
            .Where(c => c.Volume.SeriesId == seriesId)
            .ApplyDefaultChapterOrdering()
            .AsNoTracking()
            .Select(ChapterMapping.ToChapterDtoExpression(userId))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<ChapterDto?> GetFirstChapterForVolumeAsync(int volumeId, int userId, CancellationToken ct = default)
    {
        // Get the chapter entity with proper ordering
        return await context.Chapter
            .Include(c => c.Volume)
            .Include(c => c.Files)
            .Where(c => c.Volume.Id == volumeId)
            .ApplyDefaultChapterOrdering()
            .AsNoTracking()
            .Select(ChapterMapping.ToChapterDtoExpression(userId))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IList<ChapterDto>> GetChapterDtosAsync(IEnumerable<int> chapterIds, int userId,
        CancellationToken ct = default)
    {
        var chapterIdList = chapterIds.ToList();
        if (chapterIdList.Count == 0) return [];

        return await context.Chapter
            .Where(c => chapterIdList.Contains(c.Id))
            .Select(ChapterMapping.ToChapterDtoExpression(userId))
            .ToListAsync(ct);
    }

    public async Task<int?> GetSeriesIdForChapter(int chapterId, CancellationToken ct = default)
    {
        return await context.Chapter
            .Where(chp => chp.Id == chapterId)
            .Select(chp => chp.Volume.SeriesId)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IList<Chapter>> GetChaptersByExternalIdsAsync(IList<int> librariannIds, IList<string> comicVineIds,
        IList<long> metronIds, IList<int> libraryIds, CancellationToken ct = default)
    {
        if (comicVineIds.Count == 0 && metronIds.Count == 0 && librariannIds.Count == 0) return [];

        var results = new List<Chapter>();
        const int chunkSize = 500;

        foreach (var batch in librariannIds.Chunk(chunkSize))
        {
            var batchList = batch.ToList();
            results.AddRange(await BaseQuery()
                .Where(c => batchList.Contains(c.Id))
                .ToListAsync(ct));
        }

        foreach (var batch in comicVineIds.Chunk(chunkSize))
        {
            var batchList = batch.ToList();
            results.AddRange(await BaseQuery()
                .Where(c => c.ComicVineId != null && batchList.Contains(c.ComicVineId))
                .ToListAsync(ct));
        }

        foreach (var batch in metronIds.Chunk(chunkSize))
        {
            var batchList = batch.ToList();
            results.AddRange(await BaseQuery()
                .Where(c => c.MetronId > 0 && batchList.Contains(c.MetronId))
                .ToListAsync(ct));
        }

        // Dedupe as a chapter could match on multiple providers
        return results.DistinctBy(c => c.Id).ToList();

        IQueryable<Chapter> BaseQuery() => context.Chapter
            .Include(c => c.Volume)
            .ThenInclude(v => v.Series)
            .Where(c => libraryIds.Contains(c.Volume.Series.LibraryId));
    }

    public async Task<IList<Chapter>> GetChaptersByAlternateSeriesAsync(IList<string> normalizedNames, IList<int> libraryIds, CancellationToken ct = default)
    {
        if (normalizedNames.Count == 0) return [];

        // AlternateSeries is rare and not normalized in the DB, so fetch all non-empty ones and filter in-memory
        var chapters = await context.Chapter
            .Include(c => c.Volume)
            .ThenInclude(v => v.Series)
            .Where(c => libraryIds.Contains(c.Volume.Series.LibraryId))
            .Where(c => c.AlternateSeries != null && c.AlternateSeries != string.Empty)
            .ToListAsync(ct);

        var normalizedSet = new HashSet<string>(normalizedNames);
        return chapters
            .Where(c => normalizedSet.Contains(c.AlternateSeries.ToNormalized()))
            .ToList();
    }

    public Task<List<Chapter>> GetChaptersForReadStatusTransitionRuleAsync(int userId, ReadStatusTransitionRule rule, CancellationToken ct = default)
    {
        if (!rule.Enabled || rule.Days <= 0) return Task.FromResult(new List<Chapter>());

        var cutoffDate = DateTime.UtcNow.AddDays(-rule.Days);
        var excludedStatuses = rule.ExcludedPublicationStatus;

        var chapterProgressStats = context.AppUserProgresses
            .Where(p => p.AppUserId == userId && p.PagesRead > 0)
            .GroupBy(p => p.ChapterId)
            .Select(g => new
            {
                ChapterId = g.Key,
                LastProgressUtc = g.Max(p => p.LastModifiedUtc)
            });

        return context.Chapter
            .Join(chapterProgressStats,
                c => c.Id,
                cp => cp.ChapterId,
                (c, cp) => new { Chapter = c, cp.LastProgressUtc })
            .Where(x => x.LastProgressUtc < cutoffDate)
            .Select(x => x.Chapter)
            .Include(c => c.Volume)
            .ThenInclude(v => v.Series)
            .ThenInclude(s => s.Library)
            .Include(c => c.Volume)
            .ThenInclude(v => v.Series)
            .ThenInclude(s => s.ExternalSeriesMetadata)
            .Include(c => c.Volume)
            .ThenInclude(v => v.Series)
            .ThenInclude(s => s.Metadata)
            .ToListAsync(ct);
    }
}

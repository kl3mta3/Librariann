using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Repositories;
using Librariann.API.Services.Plus;
using Librariann.Common.Helpers;
using Librariann.Database.Extensions;
using Librariann.Models.DTOs.LibrariannPlus;
using Librariann.Models.DTOs.LibrariannPlus.Audit;
using Librariann.Models.Entities.Enums;
using Librariann.Models.Entities.Enums.Audit;
using Librariann.Models.Entities.Enums.LibrariannPlus;
using Librariann.Models.Entities.History;
using Microsoft.EntityFrameworkCore;

namespace Librariann.Database.Repositories;

public class LibrariannPlusAuditRepository(DataContext context) : ILibrariannPlusAuditRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Add(LibrariannPlusAuditLog entry) => context.LibrariannPlusAuditLogs.Add(entry);

    public Task<int> GetScrobbleFailureCountAsync(int userId, CancellationToken ct = default)
    {
        return context.LibrariannPlusAuditLogs
            .Where(e => e.Category == LibrariannPlusAuditCategory.Scrobble && e.UserId == userId &&
                        e.Status == AuditStatus.Failure)
            .Where(e => context.ScrobbleEvent
                .Where(se => e.SubjectType != AuditSubjectType.Chapter || e.SubjectId == se.ChapterId)
                .Any(se => !se.IsProcessed && se.AppUserId == userId && se.SeriesId == e.SeriesId)
            ).CountAsync(ct);
    }

    public async Task DeleteOlderThanAsync(DateTime cutoff, CancellationToken ct = default)
    {
        await context.LibrariannPlusAuditLogs
            .Where(e => e.CreatedUtc < cutoff)
            .ExecuteDeleteAsync(ct);
    }

    public async Task<PagedList<LibrariannPlusAuditEntryDto>> GetPagedAsync(
        LibrariannPlusAuditFilterDto filter, UserParams userParams, CancellationToken ct = default)
    {
        var query = BuildBaseQuery(filter);
        return await ProjectAndPage(query, userParams, ct);
    }

    public async Task<PagedList<LibrariannPlusAuditEntryDto>> GetMyActivityAsync(
        int userId, LibrariannPlusAuditFilterDto filter, UserParams userParams, CancellationToken ct = default)
    {
        var query = BuildBaseQuery(filter)
            .Where(e => e.UserId == userId);

        return await ProjectAndPage(query, userParams, ct);
    }

    public async Task<LibrariannPlusAuditStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        var cutoff24H = DateTime.UtcNow.AddHours(-24);

        var events24H = await context.LibrariannPlusAuditLogs
            .CountAsync(e => e.CreatedUtc >= cutoff24H, ct);

        var failures24H = await context.LibrariannPlusAuditLogs
            .CountAsync(e => e.CreatedUtc >= cutoff24H && e.Status == AuditStatus.Failure, ct);

        var unresolvedMatchFailures = await context.LibrariannPlusAuditLogs
            .CountAsync(e => e.EventType == LibrariannPlusEventType.SeriesMatchFailed
                             && e.Status == AuditStatus.Failure, ct);

        var baseEligible = context.Series
            .Where(s => s.Library.AllowMetadataMatching)
            .Where(s => !s.DontMatch);

        var matchedSeriesCount = await baseEligible.WhereMatchedExternalMetadata().CountAsync(ct);

        var totalEligibleSeriesCount = await baseEligible.CountAsync(ct);

        var staleMatchesCount = await baseEligible.WhereStaleExternalMetadata().CountAsync(ct);

        var blacklistedSeriesCount = await baseEligible
            .Where(s => s.IsBlacklisted)
            .CountAsync(ct);

        var scrobbleQueueCount = await context.ScrobbleEvent
            .CountAsync(e => !e.IsProcessed, ct);

        return new LibrariannPlusAuditStatsDto
        {
            Events24H = events24H,
            Failures24H = failures24H,
            UnresolvedMatchFailures = unresolvedMatchFailures,
            MatchedSeriesCount = matchedSeriesCount,
            TotalEligibleSeriesCount = totalEligibleSeriesCount,
            StaleMatchesCount = staleMatchesCount,
            BlacklistedSeriesCount = blacklistedSeriesCount,
            ScrobbleQueueCount = scrobbleQueueCount,
        };
    }

    public async Task<LibrariannPlusMyAuditStatsDto> GetMyStatsAsync(int userId, CancellationToken ct = default)
    {
        var cutoff24H = DateTime.UtcNow.AddHours(-24);

        var events24H = await context.LibrariannPlusAuditLogs
            .Where(e => e.UserId == userId)
            .CountAsync(e => e.CreatedUtc >= cutoff24H, ct);

        var failures24H = await context.LibrariannPlusAuditLogs
            .Where(e => e.UserId == userId)
            .CountAsync(e => e.CreatedUtc >= cutoff24H && e.Status == AuditStatus.Failure, ct);

        var scrobbleQueueCount = await context.ScrobbleEvent
            .Where(e => e.AppUserId == userId)
            .CountAsync(e => !e.IsProcessed, ct);

        return new LibrariannPlusMyAuditStatsDto
        {
            Events24H = events24H,
            Failures24H = failures24H,
            ScrobbleQueueCount = scrobbleQueueCount,
        };
    }

    public async Task<LibrariannPlusAuditSeriesInfoDto> GetSeriesInfoAsync(
        int seriesId, int callingUserId, bool isAdmin, CancellationToken ct = default)
    {
        var series = await context.Series
            .Include(s => s.ExternalSeriesMetadata)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == seriesId, ct);

        if (series == null)
        {
            return new LibrariannPlusAuditSeriesInfoDto { SeriesId = seriesId };
        }

        var recentQuery = context.LibrariannPlusAuditLogs
            .AsNoTracking()
            .Where(e => e.SeriesId == seriesId)
            .Where(e => e.Category != LibrariannPlusAuditCategory.Scrobble
                        || isAdmin
                        || e.UserId == callingUserId)
            .OrderByDescending(e => e.CreatedUtc)
            .Take(20);

        var recentRaw = await recentQuery
            .Select(e => new RawEntry(
                e.Id, e.CreatedUtc, e.Category, e.EventType, e.Status,
                e.SeriesId, series.LibraryId, series.Name,
                e.SubjectType, e.SubjectId,
                e.UserId, e.User != null ? e.User.UserName : null,
                e.Payload, e.ErrorMessage, e.ScrobbleErrorId, e.HasRetried))
            .ToListAsync(ct);

        // Due to Json deserialization, I can't use automapper here and need to do in-mem
        var recentEvents = recentRaw.Select(MapToDto).ToList();

        return new LibrariannPlusAuditSeriesInfoDto
        {
            SeriesId = series.Id,
            LibraryId = series.LibraryId,
            SeriesName = series.Name,
            IsMatched = !series.IsBlacklisted
                && series.ExternalSeriesMetadata != null
                && series.ExternalSeriesMetadata.ValidUntilUtc > DateTime.MinValue,
            MangaBakaId = series.MangaBakaId != 0 ? series.MangaBakaId : null,
            AniListId = series.AniListId != 0 ? series.AniListId : null,
            HardcoverId = series.HardcoverId != 0 ? series.HardcoverId : null,
            CbrId = series.CbrId != 0 ? series.CbrId : null,
            ComicVineId = series.ComicVineId != string.Empty ? series.ComicVineId : null,
            MalId = series.MalId != 0 ? series.MalId : null,
            MetronId = series.MetronId != 0 ? series.MetronId : null,
            IsStandAlone = series.IsStandAlone,
            MetadataProvider = series.ExternalSeriesMetadata?.Provider,
            NextRefreshUtc = series.ExternalSeriesMetadata?.ValidUntilUtc,
            LastRefreshedUtc = series.ExternalSeriesMetadata?.LastModifiedUtc,
            RecentEvents = recentEvents,
        };
    }

    private IQueryable<LibrariannPlusAuditLog> BuildBaseQuery(LibrariannPlusAuditFilterDto filter)
    {
        return context.LibrariannPlusAuditLogs
            .AsNoTracking()
            .WhereIf(filter.Category.HasValue, e => e.Category == filter.Category!.Value)
            .WhereIf(filter.Status.HasValue, e => e.Status == filter.Status!.Value)
            .WhereIf(filter.SubjectType.HasValue, e => e.SubjectType == filter.SubjectType!.Value)
            .WhereIf(filter.UserId.HasValue, e => e.UserId == filter.UserId!.Value)
            .WhereIf(filter.SeriesId.HasValue, e => e.SeriesId == filter.SeriesId!.Value)
            .WhereIf(filter.Provider.HasValue, e =>
                e.Category == LibrariannPlusAuditCategory.Scrobble &&
                // Best way for us to filter right now. In EF.Core 11 we'll get a EF.Functions.JsonContains but unsure
                // if this is also for sqlite
                EF.Functions.Like(e.Payload, $"%\"Provider\":{(int)filter.Provider!.Value}%"))
            .WhereIf(filter.FromUtc.HasValue, e => e.CreatedUtc >= filter.FromUtc!.Value)
            .WhereIf(filter.ToUtc.HasValue, e => e.CreatedUtc <= filter.ToUtc!.Value)
            .WhereIf(!string.IsNullOrEmpty(filter.Search), e =>
                context.Series.Any(s => s.Id == e.SeriesId && s.Name.Contains(filter.Search!)) ||
                (e.User != null && e.User.UserName!.Contains(filter.Search!)) ||
                (e.ErrorMessage != null && e.ErrorMessage.Contains(filter.Search!)))
            .OrderByDescending(e => e.CreatedUtc);
    }

    private async Task<PagedList<LibrariannPlusAuditEntryDto>> ProjectAndPage(
        IQueryable<LibrariannPlusAuditLog> query, UserParams userParams, CancellationToken ct)
    {
        var count = await query.CountAsync(ct);
        var raw = await query
            .Skip((userParams.PageNumber - 1) * userParams.PageSize)
            .Take(userParams.PageSize)
            .Select(e => new RawEntry(
                e.Id, e.CreatedUtc, e.Category, e.EventType, e.Status,
                e.SeriesId,
                context.Series.Where(s => s.Id == e.SeriesId).Select(s => (int?)s.LibraryId).FirstOrDefault(),
                context.Series.Where(s => s.Id == e.SeriesId).Select(s => s.Name).FirstOrDefault(),
                e.SubjectType, e.SubjectId,
                e.UserId, e.User != null ? e.User.UserName : null,
                e.Payload, e.ErrorMessage, e.ScrobbleErrorId, e.HasRetried))
            .ToListAsync(ct);

        var items = raw.Select(MapToDto).ToList();
        return PagedList<LibrariannPlusAuditEntryDto>.Create(items, count, userParams);
    }

    private static LibrariannPlusAuditEntryDto MapToDto(RawEntry e)
    {
        IList<MetadataFieldChangeDto>? diff = null;
        if (e is {Category: LibrariannPlusAuditCategory.Metadata, Payload: not null})
        {
            try
            {
                var wrapper = JsonSerializer.Deserialize<ChangesWrapper>(e.Payload, JsonOptions);
                diff = wrapper?.Changes;
            }
            catch
            {
                // malformed payload
            }
        }

        LibrariannPlusScrobbleDetailsDto? scrobbleDetails = null;
        if (e is {Category: LibrariannPlusAuditCategory.Scrobble, Payload: not null})
        {
            try
            {
                var p = JsonSerializer.Deserialize<AuditLogScrobbleParamsDto>(e.Payload, JsonOptions);
                if (p != null)
                {
                    scrobbleDetails = new LibrariannPlusScrobbleDetailsDto
                    {
                        ScrobbleEventType = p.ScrobbleEventType,
                        ChapterNumber = p.ChapterNumber,
                        VolumeNumber = p.VolumeNumber,
                        PercentRead = p.PercentRead,
                        Rating = p.Rating,
                        ReviewBody = p.ReviewBody,
                        ReadStatus = p.ReadStatus,
                        Provider = p.Provider,
                        LibraryType = p.LibraryType,
                    };
                }
            }
            catch
            {
                // malformed payload
            }
        }

        LibrariannPlusAuditMatchDetailsDto? matchDetails = null;
        if (e is { Category: LibrariannPlusAuditCategory.Match, Payload: not null })
        {
            try
            {
                matchDetails = e.EventType switch
                {
                    LibrariannPlusEventType.SeriesMatched =>
                        LibrariannPlusAuditMatchDetailsDto.From(JsonSerializer.Deserialize<AuditLogMatchedParamsDto>(e.Payload, JsonOptions)),
                    LibrariannPlusEventType.SeriesMatchFixed =>
                        LibrariannPlusAuditMatchDetailsDto.From(JsonSerializer.Deserialize<AuditLogMatchClearedParamsDto>(e.Payload, JsonOptions)),
                    LibrariannPlusEventType.SeriesMatchFailed or LibrariannPlusEventType.SeriesBlacklisted =>
                        LibrariannPlusAuditMatchDetailsDto.From(JsonSerializer.Deserialize<AuditLogMatchFailureParamsDto>(e.Payload, JsonOptions)),
                    LibrariannPlusEventType.SeriesDontMatchSet =>
                        LibrariannPlusAuditMatchDetailsDto.From(JsonSerializer.Deserialize<AuditLogMatchDontMatchParamsDto>(e.Payload, JsonOptions)),
                    LibrariannPlusEventType.SeriesMetadataProviderOverrideSet =>
                        LibrariannPlusAuditMatchDetailsDto.From(JsonSerializer.Deserialize<AuditLogMatchProviderOverrideParamsDto>(e.Payload, JsonOptions)),
                    _ => null
                };
            }
            catch
            {
                // malformed payload
            }
        }

        LibrariannPlusAuditSyncDetailsDto? syncDetails = null;
        if (e is { Category: LibrariannPlusAuditCategory.Sync, Payload: not null })
        {
            try
            {
                switch (e.EventType)
                {
                    case LibrariannPlusEventType.CollectionSynced:
                        syncDetails = LibrariannPlusAuditSyncDetailsDto.From(JsonSerializer.Deserialize<AuditLogCollectionSyncedParamsDto>(e.Payload, JsonOptions));
                        break;
                    case LibrariannPlusEventType.CollectionItemAdded:
                        syncDetails = LibrariannPlusAuditSyncDetailsDto.From(JsonSerializer.Deserialize<AuditLogCollectionItemParamsDto>(e.Payload, JsonOptions));
                        break;
                    case LibrariannPlusEventType.SyncCompleted:
                        syncDetails = LibrariannPlusAuditSyncDetailsDto.From(JsonSerializer.Deserialize<AuditLogWantToReadSyncCompletedParamsDto>(e.Payload, JsonOptions));
                        break;
                    case LibrariannPlusEventType.SyncStarted:
                    {
                        var started = JsonSerializer.Deserialize<AuditLogCollectionStartedParamsDto>(e.Payload, JsonOptions);
                        syncDetails = !string.IsNullOrEmpty(started?.CollectionName)
                            ? LibrariannPlusAuditSyncDetailsDto.From(started)
                            : LibrariannPlusAuditSyncDetailsDto.From(JsonSerializer.Deserialize<AuditLogWantToReadSyncParamsDto>(e.Payload, JsonOptions));
                        break;
                    }
                    case LibrariannPlusEventType.SyncFailed:
                        syncDetails = LibrariannPlusAuditSyncDetailsDto.From(JsonSerializer.Deserialize<AuditLogCollectionFailedParamsDto>(e.Payload, JsonOptions));
                        break;
                }
            }
            catch
            {
                // malformed payload
            }
        }

        LibrariannPlusAuditMetadataExtrasDto? metadataExtras = null;
        if (e is { Category: LibrariannPlusAuditCategory.Metadata, Payload: not null })
        {
            try
            {
                metadataExtras = e.EventType switch
                {
                    LibrariannPlusEventType.CoverUpdated =>
                        LibrariannPlusAuditMetadataExtrasDto.From(JsonSerializer.Deserialize<AuditLogSeriesCoverParamsDto>(e.Payload, JsonOptions)),
                    LibrariannPlusEventType.ChapterCoverUpdated =>
                        LibrariannPlusAuditMetadataExtrasDto.From(JsonSerializer.Deserialize<AuditLogChapterCoverParamsDto>(e.Payload, JsonOptions)),
                    LibrariannPlusEventType.VolumeCoverUpdated =>
                        LibrariannPlusAuditMetadataExtrasDto.From(JsonSerializer.Deserialize<AuditLogVolumeCoverParamsDto>(e.Payload, JsonOptions)),
                    LibrariannPlusEventType.PersonAliasAdded =>
                        LibrariannPlusAuditMetadataExtrasDto.From(JsonSerializer.Deserialize<AuditLogPersonAliasParamsDto>(e.Payload, JsonOptions)),
                    LibrariannPlusEventType.PersonCoverUpdated =>
                        LibrariannPlusAuditMetadataExtrasDto.From(JsonSerializer.Deserialize<AuditLogPersonCoverParamsDto>(e.Payload, JsonOptions)),
                    LibrariannPlusEventType.MetadataFetched =>
                        LibrariannPlusAuditMetadataExtrasDto.From(JsonSerializer.Deserialize<AuditLogMetadataFetchParamsDto>(e.Payload, JsonOptions)),
                    _ => null
                };
            }
            catch
            {
                // malformed payload
            }
        }

        LibrariannPlusAuditSystemDetailsDto? systemDetails = null;
        if (e is { Category: LibrariannPlusAuditCategory.System, Payload: not null })
        {
            try
            {
                systemDetails = e.EventType switch
                {
                    LibrariannPlusEventType.SystemProviderInfoSync => LibrariannPlusAuditSystemDetailsDto.From(
                        JsonSerializer.Deserialize<AuditLogSystemProviderInfoSyncParamsDto>(e.Payload, JsonOptions)),
                    LibrariannPlusEventType.SystemTokenRefresh => LibrariannPlusAuditSystemDetailsDto.From(
                        JsonSerializer.Deserialize<AuditLogSystemTokenRefreshParamsDto>(e.Payload, JsonOptions)),
                    _ => null,
                };
            }
            catch
            {
                // malformed payload
            }
        }

        return new LibrariannPlusAuditEntryDto
        {
            Id = e.Id,
            CreatedUtc = e.CreatedUtc,
            Category = e.Category,
            EventType = e.EventType,
            Status = e.Status,
            SeriesId = e.SeriesId,
            LibraryId = e.LibraryId,
            SeriesName = e.SeriesName,
            SubjectType = e.SubjectType,
            SubjectId = e.SubjectId,
            UserId = e.UserId,
            Username = e.Username,
            Diff = diff,
            ErrorMessage = e.ErrorMessage,
            ScrobbleErrorId = e.ScrobbleErrorId,
            ScrobbleDetails = scrobbleDetails,
            MatchDetails = matchDetails,
            SyncDetails = syncDetails,
            MetadataExtras = metadataExtras,
            SystemDetails = systemDetails,
            CanRetry = e is { Status: AuditStatus.Failure, Category: LibrariannPlusAuditCategory.Scrobble, HasRetried: false }
                       // We are currently unable to retry chapter reads. See ScrobblingService#RetryScrobbleAsync:L1977
                       && scrobbleDetails?.ScrobbleEventType != ScrobbleEventType.ChapterRead,
        };
    }

    public async Task MarkAsRetriedAsync(long id, CancellationToken ct = default)
    {
        await context.LibrariannPlusAuditLogs
            .Where(e => e.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.HasRetried, true), ct);
    }

    private sealed record RawEntry(
        long Id, DateTime CreatedUtc, LibrariannPlusAuditCategory Category,
        LibrariannPlusEventType EventType, AuditStatus Status,
        int? SeriesId, int? LibraryId, string? SeriesName,
        AuditSubjectType SubjectType, int? SubjectId,
        int? UserId, string? Username,
        string? Payload, string? ErrorMessage, int? ScrobbleErrorId, bool HasRetried);

    private sealed class ChangesWrapper
    {
        public List<MetadataFieldChangeDto>? Changes { get; set; }
    }
}

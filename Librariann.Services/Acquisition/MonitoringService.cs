using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Librariann.API.Database;
using Librariann.API.Repositories;
using Librariann.API.Services.Acquisition;
using Librariann.Common;
using Librariann.Models.DTOs.Acquisition;
using Librariann.Models.DTOs.Metadata;
using Librariann.Models.Entities.Acquisition;
using Librariann.Models.Entities.Enums;

namespace Librariann.Services.Acquisition;

public sealed class MonitoringService(IUnitOfWork unitOfWork) : IMonitoringService
{
    public async Task<IReadOnlyCollection<MonitoringTargetDto>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        (await unitOfWork.MonitoringRepository.GetAllAsync(cancellationToken)).Select(ToDto).ToArray();

    public async Task<IReadOnlyCollection<MonitoringSearchRunDto>> GetHistoryAsync(int? targetId, int take,
        CancellationToken cancellationToken = default)
    {
        if (targetId is <= 0) throw new LibrariannException("monitoring-target-does-not-exist");
        take = Math.Clamp(take, 1, 250);
        return (await unitOfWork.MonitoringRepository.GetHistoryAsync(targetId, take, cancellationToken))
            .Select(ToDto).ToArray();
    }

    public async Task<IReadOnlyCollection<WantedItemDto>> GetWantedAsync(int? targetId,
        CancellationToken cancellationToken = default) =>
        (await unitOfWork.MonitoringRepository.GetWantedAsync(targetId, cancellationToken)).Select(ToDto).ToArray();

    public async Task<MonitoringTargetDto> UpsertAsync(int userId, UpsertMonitoringTargetRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        var profile = await unitOfWork.QualityProfileRepository.GetAsync(request.QualityProfileId, cancellationToken)
                      ?? throw new LibrariannException("quality-profile-does-not-exist");
        if (profile.MediaType != request.MediaType) throw new LibrariannException("quality-profile-media-type-mismatch");
        if (request.AutomaticGrabEnabled && !request.DownloadClientId.HasValue)
            throw new LibrariannException("automatic-grab-download-client-required");
        if (request.DownloadClientId.HasValue)
        {
            var client = await unitOfWork.IntegrationProviderRepository.GetAsync(request.DownloadClientId.Value,
                cancellationToken) ?? throw new LibrariannException("download-client-does-not-exist");
            if (!client.IsEnabled || client.Category != IntegrationProviderCategory.DownloadClient ||
                !client.DownloadClientKind.HasValue)
                throw new LibrariannException("download-client-is-not-enabled");
        }

        var title = request.Title.Trim();
        if (request.LibrarySeriesId.HasValue)
        {
            var series = await unitOfWork.SeriesRepository.GetSeriesByIdAsync(request.LibrarySeriesId.Value,
                SeriesIncludes.Library, cancellationToken) ?? throw new LibrariannException("series-does-not-exist");
            var mediaType = ToMediaType(series.Library.Type);
            if (mediaType != request.MediaType) throw new LibrariannException("monitoring-media-type-mismatch");
            title = series.Name;
        }

        var repository = unitOfWork.MonitoringRepository;
        MonitoringTarget target;
        if (request.Id > 0)
        {
            target = await repository.GetAsync(request.Id, cancellationToken)
                     ?? throw new LibrariannException("monitoring-target-does-not-exist");
        }
        else
        {
            target = new MonitoringTarget {CreatedByUserId = userId};
            repository.Add(target);
        }

        var policyChanged = target.QualityProfileId != request.QualityProfileId ||
                            target.Language != request.Language.Trim() || target.Title != title ||
                            target.Author != request.Author.Trim() || target.Isbn != request.Isbn.Trim() ||
                            target.AutomaticGrabEnabled != request.AutomaticGrabEnabled ||
                            target.DownloadClientId != request.DownloadClientId ||
                            target.MinimumAutomaticGrabScore != request.MinimumAutomaticGrabScore;
        target.Kind = request.Kind;
        target.MediaType = request.MediaType;
        target.LibrarySeriesId = request.LibrarySeriesId;
        target.QualityProfileId = request.QualityProfileId;
        target.Title = title;
        target.Author = request.Author.Trim();
        target.Isbn = request.Isbn.Trim();
        target.Language = request.Language.Trim();
        target.ExternalProviderKey = request.ExternalProviderKey.Trim().ToLowerInvariant();
        target.ExternalItemId = request.ExternalItemId.Trim();
        target.MonitorMissing = request.MonitorMissing;
        target.MonitorFuture = request.MonitorFuture;
        target.AutomaticGrabEnabled = request.AutomaticGrabEnabled;
        target.DownloadClientId = request.DownloadClientId;
        target.MinimumAutomaticGrabScore = request.MinimumAutomaticGrabScore;
        target.SearchIntervalHours = request.SearchIntervalHours;
        if ((!target.IsEnabled && request.IsEnabled) || policyChanged) target.NextSearchAtUtc = DateTime.UtcNow;
        target.IsEnabled = request.IsEnabled;
        target.UpdatedAtUtc = DateTime.UtcNow;

        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(target);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var target = await unitOfWork.MonitoringRepository.GetAsync(id, cancellationToken)
                     ?? throw new LibrariannException("monitoring-target-does-not-exist");
        unitOfWork.MonitoringRepository.Remove(target);
        await unitOfWork.CommitAsync(cancellationToken);
    }

    public async Task SearchNowAsync(int id, CancellationToken cancellationToken = default)
    {
        _ = await unitOfWork.MonitoringRepository.GetAsync(id, cancellationToken)
            ?? throw new LibrariannException("monitoring-target-does-not-exist");
        BackgroundJob.Enqueue<IMonitoringJobService>(service =>
            service.SearchTargetAsync(id, CancellationToken.None));
    }

    public async Task SyncCatalogNowAsync(int id, CancellationToken cancellationToken = default)
    {
        _ = await unitOfWork.MonitoringRepository.GetAsync(id, cancellationToken)
            ?? throw new LibrariannException("monitoring-target-does-not-exist");
        BackgroundJob.Enqueue<IMonitoringCatalogService>(service =>
            service.SyncCatalogAsync(id, CancellationToken.None));
    }

    private static void ValidateRequest(UpsertMonitoringTargetRequest request)
    {
        if (!Enum.IsDefined(request.Kind) || !Enum.IsDefined(request.MediaType))
            throw new LibrariannException("invalid-monitoring-target");
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Language))
            throw new LibrariannException("monitoring-title-and-language-required");
        if (request.SearchIntervalHours is < 1 or > 24 * 30)
            throw new LibrariannException("invalid-monitoring-search-interval");
        if (request.MinimumAutomaticGrabScore is < 0 or > 500)
            throw new LibrariannException("invalid-automatic-grab-score");
        var hasProvider = !string.IsNullOrWhiteSpace(request.ExternalProviderKey);
        var hasExternalId = !string.IsNullOrWhiteSpace(request.ExternalItemId);
        if (hasProvider != hasExternalId) throw new LibrariannException("monitoring-external-identity-incomplete");
    }

    private static LibrariannMediaType ToMediaType(LibraryType type) => type switch
    {
        LibraryType.Book or LibraryType.LightNovel => LibrariannMediaType.Book,
        LibraryType.Manga => LibrariannMediaType.Manga,
        _ => LibrariannMediaType.Comic,
    };

    internal static MonitoringTargetDto ToDto(MonitoringTarget target) => new(target.Id, target.CreatedByUserId,
        target.Kind, target.MediaType, target.LibrarySeriesId, target.QualityProfileId, target.Title, target.Author,
        target.Isbn, target.Language, target.ExternalProviderKey, target.ExternalItemId, target.MonitorMissing,
        target.MonitorFuture, target.AutomaticGrabEnabled, target.DownloadClientId, target.MinimumAutomaticGrabScore,
        target.LastAutomaticGrabAtUtc, target.IsEnabled, target.SearchIntervalHours, target.LastSearchAtUtc,
        target.NextSearchAtUtc, target.LastSearchSummary, target.LastCatalogSyncAtUtc, target.CatalogSummary,
        target.CreatedAtUtc, target.UpdatedAtUtc);

    private static WantedItemDto ToDto(WantedItem item) => new(item.Id, item.MonitoringTargetId, item.ProviderKey,
        item.ExternalItemId, item.Title, item.Author, item.Series, item.Sequence, item.PublicationYear, item.Status,
        item.LibrarySeriesId, item.FirstSeenAtUtc, item.LastSeenAtUtc, item.LastSearchAtUtc, item.NextSearchAtUtc,
        item.LastSearchSummary);

    private static MonitoringSearchRunDto ToDto(MonitoringSearchRun run) => new(run.Id, run.MonitoringTargetId,
        run.WantedItemId, run.Status, run.Query, run.ResultCount, run.ApprovedCount, run.BestReleaseTitle, run.BestReleaseScore,
        run.Summary, run.WasGrabbed, run.GrabSummary, run.DecisionSnapshotJson, run.StartedAtUtc, run.CompletedAtUtc);
}

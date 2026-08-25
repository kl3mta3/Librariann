using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Librariann.API.Database;
using Librariann.API.Services.Acquisition;
using Librariann.Models.DTOs.Acquisition;
using Librariann.Models.Entities.Acquisition;
using Microsoft.Extensions.Logging;

namespace Librariann.Services.Acquisition;

public sealed class MonitoringJobService(
    IUnitOfWork unitOfWork,
    IInteractiveSearchService searchService,
    IReleaseGrabService releaseGrabService,
    ILogger<MonitoringJobService> logger) : IMonitoringJobService
{
    private const int MissingItemBatchSize = 10;
    private static readonly ConcurrentDictionary<int, SemaphoreSlim> TargetLocks = new();

    [DisableConcurrentExecution(timeoutInSeconds: 15 * 60)]
    public async Task SearchDueAsync(CancellationToken cancellationToken = default)
    {
        var targets = await unitOfWork.MonitoringRepository.GetDueAsync(cancellationToken);
        foreach (var target in targets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await SearchTargetCoreAsync(target, cancellationToken);
        }
    }

    public async Task SearchTargetAsync(int targetId, CancellationToken cancellationToken = default)
    {
        var target = await unitOfWork.MonitoringRepository.GetAsync(targetId, cancellationToken);
        if (target is null) return;
        await SearchTargetCoreAsync(target, cancellationToken);
    }

    private async Task SearchTargetCoreAsync(MonitoringTarget target, CancellationToken cancellationToken)
    {
        var targetLock = TargetLocks.GetOrAdd(target.Id, _ => new SemaphoreSlim(1, 1));
        if (!await targetLock.WaitAsync(0, cancellationToken)) return;
        try
        {
            if (target.Kind != MonitoringTargetKind.Book && target.MonitorMissing &&
                target.LastCatalogSyncAtUtc.HasValue)
            {
                var dueItems = await unitOfWork.MonitoringRepository.GetDueWantedAsync(target.Id,
                    MissingItemBatchSize + 1, cancellationToken);
                if (dueItems.Count == 0)
                {
                    AdvanceTarget(target, "No missing catalog items are due for search.",
                        DateTime.UtcNow.AddHours(target.SearchIntervalHours));
                    await unitOfWork.CommitAsync(cancellationToken);
                    return;
                }

                var searched = 0;
                var candidates = 0;
                var grabbed = 0;
                foreach (var item in dueItems.Take(MissingItemBatchSize))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var outcome = await SearchAsync(target, item, cancellationToken);
                    if (await TryAutomaticGrabAsync(target, item, outcome, cancellationToken)) grabbed++;
                    unitOfWork.MonitoringRepository.AddRun(outcome.Run);
                    UpdateWantedItem(item, target, outcome.Run);
                    searched++;
                    if (outcome.Run.Status == MonitoringSearchStatus.CandidateFound) candidates++;
                }

                var moreDue = dueItems.Count > MissingItemBatchSize;
                var next = moreDue
                    ? DateTime.UtcNow.AddMinutes(15)
                    : DateTime.UtcNow.AddHours(target.SearchIntervalHours);
                AdvanceTarget(target,
                    $"Searched {searched} missing catalog item(s); {candidates} had approved candidates; {grabbed} grabbed.", next);
                await unitOfWork.CommitAsync(cancellationToken);
                return;
            }

            var targetOutcome = await SearchAsync(target, null, cancellationToken);
            await TryAutomaticGrabAsync(target, null, targetOutcome, cancellationToken);
            RecordRun(target, targetOutcome.Run);
            await unitOfWork.CommitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Monitoring search failed for target {TargetId}", target.Id);
            var completed = DateTime.UtcNow;
            RecordRun(target, new MonitoringSearchRun
            {
                MonitoringTargetId = target.Id,
                Status = MonitoringSearchStatus.ProviderFailure,
                Query = BuildQuery(target, null),
                Summary = "The monitoring search failed. Review provider health and server logs.",
                StartedAtUtc = completed,
                CompletedAtUtc = completed,
            });
            await unitOfWork.CommitAsync(cancellationToken);
        }
        finally
        {
            targetLock.Release();
        }
    }

    private async Task<SearchOutcome> SearchAsync(MonitoringTarget target, WantedItem? wantedItem,
        CancellationToken cancellationToken)
    {
        var started = DateTime.UtcNow;
        var query = BuildQuery(target, wantedItem);
        try
        {
            var expectedTitle = wantedItem?.Title ??
                                (target.Kind == MonitoringTargetKind.Author ? string.Empty : target.Title);
            var expectedAuthor = wantedItem?.Author ??
                                 (target.Kind == MonitoringTargetKind.Author ? target.Title : target.Author);
            var response = await searchService.SearchForAutomationAsync(new InteractiveSearchRequest
            {
                QualityProfileId = target.QualityProfileId,
                Search = new IndexerSearchRequest
                {
                    Query = query,
                    Title = expectedTitle,
                    Author = expectedAuthor,
                    Series = wantedItem?.Series ??
                             (target.Kind == MonitoringTargetKind.Series ? target.Title : string.Empty),
                    Isbn = wantedItem is null ? target.Isbn : string.Empty,
                },
                Evaluation = new ReleaseEvaluationContext
                {
                    ExpectedTitle = expectedTitle,
                    ExpectedAuthor = expectedAuthor,
                },
            }, cancellationToken);

            var approved = response.Results.Where(result => result.IsApproved).ToArray();
            var best = approved.FirstOrDefault();
            var status = best is not null
                ? MonitoringSearchStatus.CandidateFound
                : response.ProviderFailures.Count > 0 && response.Results.Count == 0
                    ? MonitoringSearchStatus.ProviderFailure
                    : MonitoringSearchStatus.NoApprovedCandidate;
            var summary = status switch
            {
                MonitoringSearchStatus.CandidateFound =>
                    $"Found {approved.Length} approved candidate(s); best score {best!.Score}.",
                MonitoringSearchStatus.ProviderFailure =>
                    $"No results; {response.ProviderFailures.Count} provider(s) failed.",
                _ => $"No approved candidates; {response.Results.Count} result(s) evaluated.",
            };
            return new SearchOutcome(new MonitoringSearchRun
            {
                MonitoringTargetId = target.Id,
                WantedItemId = wantedItem?.Id,
                Status = status,
                Query = query,
                ResultCount = response.Results.Count,
                ApprovedCount = approved.Length,
                BestReleaseTitle = best?.Release.Title ?? string.Empty,
                BestReleaseScore = best?.Score,
                Summary = summary,
                DecisionSnapshotJson = SerializeSnapshot(response),
                StartedAtUtc = started,
                CompletedAtUtc = DateTime.UtcNow,
            }, approved);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Monitoring search failed for target {TargetId}, wanted item {WantedItemId}",
                target.Id, wantedItem?.Id);
            return new SearchOutcome(new MonitoringSearchRun
            {
                MonitoringTargetId = target.Id,
                WantedItemId = wantedItem?.Id,
                Status = MonitoringSearchStatus.ProviderFailure,
                Query = query,
                Summary = "The monitoring search failed. Review provider health and server logs.",
                StartedAtUtc = started,
                CompletedAtUtc = DateTime.UtcNow,
            }, []);
        }
    }

    private async Task<bool> TryAutomaticGrabAsync(MonitoringTarget target, WantedItem? wantedItem,
        SearchOutcome outcome, CancellationToken cancellationToken)
    {
        if (!target.AutomaticGrabEnabled || !target.DownloadClientId.HasValue) return false;
        if (await unitOfWork.AcquisitionDownloadRepository.HasActiveForMonitoringAsync(target.Id, wantedItem?.Id,
                cancellationToken))
        {
            outcome.Run.GrabSummary = "An active download already exists for this monitoring item.";
            if (wantedItem is not null) wantedItem.Status = WantedItemStatus.Downloading;
            return false;
        }

        var configuration = await unitOfWork.IntegrationProviderRepository.GetAsync(target.DownloadClientId.Value,
            cancellationToken);
        if (configuration is null || !configuration.IsEnabled || !configuration.DownloadClientKind.HasValue)
        {
            outcome.Run.GrabSummary = "Automatic grab is enabled, but its download client is unavailable.";
            return false;
        }

        var protocol = configuration.DownloadClientKind == DownloadClientKind.Sabnzbd
            ? DownloadProtocol.Usenet
            : DownloadProtocol.Torrent;
        var decision = outcome.Approved.FirstOrDefault(item => item.Score >= target.MinimumAutomaticGrabScore &&
                                                               item.Release.Protocol == protocol &&
                                                               item.Release.DownloadUri is not null);
        if (decision is null)
        {
            if (outcome.Approved.Count > 0)
                outcome.Run.GrabSummary = "No approved candidate met the automatic-grab score and client protocol policy.";
            return false;
        }

        try
        {
            var grabbed = await releaseGrabService.GrabTrustedAsync(target.CreatedByUserId,
                target.DownloadClientId.Value, decision.Release, target.Id, wantedItem?.Id,
                cancellationToken);
            outcome.Run.WasGrabbed = true;
            outcome.Run.GrabSummary = $"Sent {grabbed.ReleaseTitle} to {grabbed.DownloadClientName}.";
            target.LastAutomaticGrabAtUtc = DateTime.UtcNow;
            if (wantedItem is not null) wantedItem.Status = WantedItemStatus.Downloading;
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Automatic grab failed for target {TargetId}, wanted item {WantedItemId}",
                target.Id, wantedItem?.Id);
            outcome.Run.GrabSummary = "Automatic grab failed. Review download-client health and server logs.";
            return false;
        }
    }

    private void RecordRun(MonitoringTarget target, MonitoringSearchRun run)
    {
        AdvanceTarget(target, run.Summary, run.CompletedAtUtc.AddHours(target.SearchIntervalHours),
            run.CompletedAtUtc);
        unitOfWork.MonitoringRepository.AddRun(run);
    }

    private static void UpdateWantedItem(WantedItem item, MonitoringTarget target, MonitoringSearchRun run)
    {
        item.LastSearchAtUtc = run.CompletedAtUtc;
        item.NextSearchAtUtc = run.CompletedAtUtc.AddHours(
            run.Status == MonitoringSearchStatus.ProviderFailure ? 1 : target.SearchIntervalHours);
        item.LastSearchSummary = string.IsNullOrWhiteSpace(run.GrabSummary)
            ? run.Summary
            : $"{run.Summary} {run.GrabSummary}";
    }

    private static void AdvanceTarget(MonitoringTarget target, string summary, DateTime next,
        DateTime? completed = null)
    {
        var at = completed ?? DateTime.UtcNow;
        target.LastSearchAtUtc = at;
        target.NextSearchAtUtc = next;
        target.LastSearchSummary = summary;
        target.UpdatedAtUtc = at;
    }

    private static string BuildQuery(MonitoringTarget target, WantedItem? wantedItem) => string.Join(' ',
        new[]
            {
                wantedItem?.Title ?? target.Title,
                wantedItem?.Author ?? (target.Kind == MonitoringTargetKind.Author ? string.Empty : target.Author),
            }
            .Where(value => !string.IsNullOrWhiteSpace(value)));

    private static string SerializeSnapshot(InteractiveSearchResponse response)
    {
        var snapshot = response.Results.Take(25).Select(result => new
        {
            result.Release.ProviderKey,
            result.Release.ProviderReleaseId,
            result.Release.Title,
            result.Release.Author,
            result.Release.Language,
            result.Release.Format,
            result.Release.Protocol,
            result.Release.SizeBytes,
            result.Release.PublishedAt,
            result.Release.Seeders,
            result.Release.Peers,
            result.Release.IsRetail,
            result.Score,
            result.Rejections,
        });
        return JsonSerializer.Serialize(snapshot);
    }

    private sealed record SearchOutcome(MonitoringSearchRun Run, IReadOnlyCollection<ReleaseDecision> Approved);
}

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Database;
using Librariann.API.Services.Acquisition;
using Librariann.API.Services.Metadata;
using Librariann.Models.DTOs.Metadata;
using Librariann.Models.Entities.Acquisition;
using Microsoft.Extensions.Logging;

namespace Librariann.Services.Acquisition;

public sealed class MonitoringCatalogService(
    IUnitOfWork unitOfWork,
    IMetadataProviderFactory providerFactory,
    ILogger<MonitoringCatalogService> logger) : IMonitoringCatalogService
{
    public async Task SyncAllAsync(CancellationToken cancellationToken = default)
    {
        var targets = await unitOfWork.MonitoringRepository.GetAllAsync(cancellationToken);
        foreach (var target in targets.Where(target => target.IsEnabled &&
                     !string.IsNullOrWhiteSpace(target.ExternalProviderKey) &&
                     !string.IsNullOrWhiteSpace(target.ExternalItemId)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await SyncCatalogAsync(target.Id, cancellationToken);
        }
    }

    public async Task SyncCatalogAsync(int targetId, CancellationToken cancellationToken = default)
    {
        var target = await unitOfWork.MonitoringRepository.GetAsync(targetId, cancellationToken);
        if (target is null) return;
        var configurations = await unitOfWork.IntegrationProviderRepository.GetAllAsync(cancellationToken);
        var configuration = configurations.FirstOrDefault(item => item.IsEnabled &&
            item.Category == IntegrationProviderCategory.Metadata &&
            Normalize(item.ProviderType) == Normalize(target.ExternalProviderKey));
        if (configuration is null)
        {
            await RecordFailureAsync(target, "The configured catalog provider is not enabled.", cancellationToken);
            return;
        }

        try
        {
            using var provider = providerFactory.Create(configuration);
            if (provider is not IMetadataCatalogProvider catalogProvider)
            {
                await RecordFailureAsync(target, "This metadata provider does not support catalog expansion.",
                    cancellationToken);
                return;
            }

            var catalog = await catalogProvider.GetCatalogAsync(new MetadataCatalogRequest(target.Kind,
                target.MediaType, target.ExternalItemId, target.Title, target.Author), cancellationToken);
            var wanted = await unitOfWork.MonitoringRepository.GetWantedAsync(target.Id, cancellationToken);
            var existing = wanted.ToDictionary(item => $"{item.ProviderKey}\n{item.ExternalItemId}",
                StringComparer.OrdinalIgnoreCase);
            var owned = await unitOfWork.MonitoringRepository.GetOwnedTitlesAsync(cancellationToken);
            var ownedByTitle = owned.GroupBy(item => NormalizeTitle(item.Title))
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            var now = DateTime.UtcNow;
            var ownedCount = 0;
            foreach (var catalogItem in catalog)
            {
                var key = $"{catalogItem.ProviderKey}\n{catalogItem.ExternalItemId}";
                if (!existing.TryGetValue(key, out var item))
                {
                    item = new WantedItem
                    {
                        MonitoringTargetId = target.Id,
                        ProviderKey = catalogItem.ProviderKey,
                        ExternalItemId = catalogItem.ExternalItemId,
                        FirstSeenAtUtc = now,
                    };
                    unitOfWork.MonitoringRepository.AddWanted(item);
                }
                var isOwned = ownedByTitle.TryGetValue(NormalizeTitle(catalogItem.Title), out var libraryItem);
                var previousStatus = item.Status;
                item.Title = catalogItem.Title;
                item.Author = catalogItem.Author;
                item.Series = catalogItem.Series;
                item.Sequence = catalogItem.Sequence;
                item.PublicationYear = catalogItem.PublicationYear;
                if (item.Status != WantedItemStatus.Ignored)
                    item.Status = isOwned ? WantedItemStatus.Owned : WantedItemStatus.Missing;
                if (previousStatus != WantedItemStatus.Missing && item.Status == WantedItemStatus.Missing)
                    item.NextSearchAtUtc = now;
                item.LibrarySeriesId = isOwned ? libraryItem!.SeriesId : null;
                item.LastSeenAtUtc = now;
                if (isOwned) ownedCount++;
            }

            target.LastCatalogSyncAtUtc = now;
            target.CatalogSummary = $"Catalog contains {catalog.Count} item(s): {ownedCount} owned, {catalog.Count - ownedCount} missing.";
            target.UpdatedAtUtc = now;
            await unitOfWork.CommitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Catalog sync failed for monitoring target {TargetId}", target.Id);
            await RecordFailureAsync(target, "Catalog sync failed. Review provider health and server logs.",
                cancellationToken);
        }
    }

    private async Task RecordFailureAsync(MonitoringTarget target, string message, CancellationToken cancellationToken)
    {
        target.LastCatalogSyncAtUtc = DateTime.UtcNow;
        target.CatalogSummary = message;
        target.UpdatedAtUtc = DateTime.UtcNow;
        await unitOfWork.CommitAsync(cancellationToken);
    }

    private static string Normalize(string value) => value.Trim().ToLowerInvariant().Replace("_", "-").Replace(" ", "-")
        .Replace("openlibrary", "open-library");

    private static string NormalizeTitle(string value) => new(value.Where(char.IsLetterOrDigit)
        .Select(char.ToLowerInvariant).ToArray());
}

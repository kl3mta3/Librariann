using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Database;
using Librariann.API.Services.Acquisition;
using Librariann.Common;
using Librariann.Models.DTOs.Acquisition;
using Librariann.Models.Entities.Acquisition;
using Microsoft.Extensions.Logging;

namespace Librariann.Services.Acquisition;

public sealed class AcquisitionQueueService(
    IUnitOfWork unitOfWork,
    IDownloadClientFactory downloadClientFactory,
    ILogger<AcquisitionQueueService> logger) : IAcquisitionQueueService
{
    private const int MaximumPollFailures = 5;

    public async Task<IReadOnlyCollection<AcquisitionDownloadDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        (await unitOfWork.AcquisitionDownloadRepository.GetAllAsync(cancellationToken)).Select(ToDto).ToArray();

    public async Task PollAsync(CancellationToken cancellationToken = default)
    {
        var active = await unitOfWork.AcquisitionDownloadRepository.GetActiveAsync(cancellationToken);
        if (active.Count == 0) return;
        var providers = (await unitOfWork.IntegrationProviderRepository.GetAllAsync(cancellationToken))
            .ToDictionary(provider => provider.Id);

        foreach (var download in active)
        {
            cancellationToken.ThrowIfCancellationRequested();
            download.LastPolledAtUtc = DateTime.UtcNow;
            try
            {
                if (!providers.TryGetValue(download.IntegrationProviderConfigurationId, out var provider) || !provider.IsEnabled)
                {
                    MarkFailure(download, "The configured download client is unavailable.");
                    continue;
                }

                using var client = downloadClientFactory.Create(provider);
                var status = await client.GetStatusAsync(download.ExternalId, cancellationToken);
                if (status is null)
                {
                    MarkFailure(download, "The job was not found in the download client.");
                    continue;
                }

                download.ConsecutivePollFailures = 0;
                download.ErrorMessage = status.ErrorMessage ?? string.Empty;
                download.Progress = Math.Clamp(status.Progress, 0, 1);
                if (status.IsComplete)
                {
                    download.Progress = 1;
                    download.CompletedAtUtc ??= DateTime.UtcNow;
                    if (DownloadPathMapper.TryMap(provider, status.OutputPath, out var mappedPath))
                    {
                        download.OutputPath = mappedPath;
                        download.Status = AcquisitionDownloadStatus.ImportPending;
                    }
                    else
                    {
                        download.Status = AcquisitionDownloadStatus.NeedsManualMatch;
                        download.ErrorMessage = "The completed path is outside the configured path mapping.";
                    }
                }
                else
                {
                    download.Status = AcquisitionDownloadStatus.Downloading;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Unable to poll acquisition download {DownloadId}", download.Id);
                MarkFailure(download, "The download client could not be polled.");
            }
        }

        await unitOfWork.CommitAsync(cancellationToken);
    }

    public async Task RetryAsync(int downloadId, CancellationToken cancellationToken = default)
    {
        var download = await unitOfWork.AcquisitionDownloadRepository.GetAsync(downloadId, cancellationToken)
                       ?? throw new LibrariannException("acquisition-download-not-found");
        if (download.Status != AcquisitionDownloadStatus.Failed)
            throw new LibrariannException("acquisition-download-not-failed");

        download.Status = AcquisitionDownloadStatus.Queued;
        download.ConsecutivePollFailures = 0;
        download.ErrorMessage = string.Empty;
        download.LastPolledAtUtc = null;
        await unitOfWork.CommitAsync(cancellationToken);
    }

    public async Task RemoveAsync(int downloadId, bool deleteData, CancellationToken cancellationToken = default)
    {
        var download = await unitOfWork.AcquisitionDownloadRepository.GetAsync(downloadId, cancellationToken)
                       ?? throw new LibrariannException("acquisition-download-not-found");
        if (download.Status == AcquisitionDownloadStatus.Removed || download.ExternalRemovedAtUtc.HasValue) return;

        var provider = await unitOfWork.IntegrationProviderRepository.GetAsync(
                           download.IntegrationProviderConfigurationId, cancellationToken)
                       ?? throw new LibrariannException("acquisition-download-client-not-found");

        using var client = downloadClientFactory.Create(provider);
        await client.RemoveAsync(download.ExternalId, deleteData, cancellationToken);
        download.ExternalRemovedAtUtc = DateTime.UtcNow;
        if (download.Status != AcquisitionDownloadStatus.Imported)
            download.Status = AcquisitionDownloadStatus.Removed;
        download.ErrorMessage = string.Empty;
        await unitOfWork.CommitAsync(cancellationToken);
    }

    private static void MarkFailure(AcquisitionDownload download, string message)
    {
        download.ConsecutivePollFailures++;
        download.ErrorMessage = message;
        if (download.ConsecutivePollFailures >= MaximumPollFailures) download.Status = AcquisitionDownloadStatus.Failed;
    }

    private static AcquisitionDownloadDto ToDto(AcquisitionDownload item) => new(item.Id, item.RequestedByUserId,
        item.IntegrationProviderConfigurationId, item.DownloadClientName, item.ExternalId, item.ReleaseTitle,
        item.Format, item.Protocol, item.Status, item.Progress, item.OutputPath, item.ImportedPath,
        item.ImportedSeriesId, item.ErrorMessage, item.CreatedAtUtc, item.LastPolledAtUtc, item.CompletedAtUtc,
        item.ImportedAtUtc, item.MetadataRefreshQueuedAtUtc, item.ExternalRemovedAtUtc);
}

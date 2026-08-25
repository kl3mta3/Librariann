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

namespace Librariann.Services.Acquisition;

public sealed class ReleaseGrabService(
    IUnitOfWork unitOfWork,
    IReleaseTokenStore tokenStore,
    IDownloadClientFactory downloadClientFactory) : IReleaseGrabService
{
    public async Task<IReadOnlyCollection<DownloadClientOption>> GetAvailableClientsAsync(CancellationToken cancellationToken = default)
    {
        var configurations = await unitOfWork.IntegrationProviderRepository.GetAllAsync(cancellationToken);
        return configurations
            .Where(configuration => configuration.IsEnabled && configuration.Category == IntegrationProviderCategory.DownloadClient && configuration.DownloadClientKind.HasValue)
            .Select(configuration => new DownloadClientOption(configuration.Id, configuration.Name,
                configuration.DownloadClientKind!.Value,
                configuration.DownloadClientKind == DownloadClientKind.Sabnzbd ? DownloadProtocol.Usenet : DownloadProtocol.Torrent))
            .ToArray();
    }

    public async Task<GrabReleaseResponse> GrabAsync(int userId, GrabReleaseRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!tokenStore.TryTake(userId, request.GrabToken, out var release) || release?.DownloadUri is null)
            throw new LibrariannException("release-grab-token-invalid-or-expired");
        return await GrabTrustedAsync(userId, request.DownloadClientId, release, cancellationToken: cancellationToken);
    }

    public async Task<GrabReleaseResponse> GrabTrustedAsync(int userId, int downloadClientId, ReleaseCandidate release,
        int? monitoringTargetId = null, int? wantedItemId = null,
        CancellationToken cancellationToken = default)
    {
        if (release.DownloadUri is null) throw new LibrariannException("release-download-uri-missing");
        if (monitoringTargetId.HasValue &&
            await unitOfWork.AcquisitionDownloadRepository.HasActiveForMonitoringAsync(monitoringTargetId.Value,
                wantedItemId, cancellationToken))
            throw new LibrariannException("automatic-grab-already-active");
        var configuration = await unitOfWork.IntegrationProviderRepository.GetAsync(downloadClientId, cancellationToken)
                            ?? throw new LibrariannException("download-client-does-not-exist");
        if (!configuration.IsEnabled || configuration.Category != IntegrationProviderCategory.DownloadClient ||
            configuration.DownloadClientKind is null)
            throw new LibrariannException("download-client-is-not-enabled");

        using var client = downloadClientFactory.Create(configuration);
        if (client.Protocol != release.Protocol)
            throw new LibrariannException("download-client-protocol-mismatch");

        var queueItem = new AcquisitionDownload
        {
            RequestedByUserId = userId,
            IntegrationProviderConfigurationId = configuration.Id,
            MonitoringTargetId = monitoringTargetId,
            WantedItemId = wantedItemId,
            DownloadClientName = configuration.Name,
            ExternalId = $"pending:{Guid.NewGuid():N}",
            ReleaseTitle = release.Title,
            Format = release.Format,
            Protocol = release.Protocol,
            Status = AcquisitionDownloadStatus.Queued,
        };
        unitOfWork.AcquisitionDownloadRepository.Add(queueItem);
        await unitOfWork.CommitAsync(cancellationToken);

        try
        {
            queueItem.ExternalId = await client.AddDownloadAsync(new DownloadGrabRequest(release.DownloadUri, release.Title,
                configuration.DownloadCategory, configuration.Tags), cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            return new GrabReleaseResponse(queueItem.ExternalId, configuration.Name, release.Title);
        }
        catch
        {
            queueItem.Status = AcquisitionDownloadStatus.Failed;
            queueItem.ErrorMessage = "The download client rejected the release.";
            await unitOfWork.CommitAsync(CancellationToken.None);
            throw;
        }
    }
}

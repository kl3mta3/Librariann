using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs.Acquisition;

namespace Librariann.API.Services.Acquisition;

public interface IReleaseGrabService
{
    Task<IReadOnlyCollection<DownloadClientOption>> GetAvailableClientsAsync(CancellationToken cancellationToken = default);
    Task<GrabReleaseResponse> GrabAsync(int userId, GrabReleaseRequest request,
        CancellationToken cancellationToken = default);
    Task<GrabReleaseResponse> GrabTrustedAsync(int userId, int downloadClientId, ReleaseCandidate release,
        int? monitoringTargetId = null, int? wantedItemId = null, CancellationToken cancellationToken = default);
}

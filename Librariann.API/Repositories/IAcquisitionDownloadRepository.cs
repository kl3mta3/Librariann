using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.Entities.Acquisition;

namespace Librariann.API.Repositories;

public interface IAcquisitionDownloadRepository
{
    Task<IReadOnlyList<AcquisitionDownload>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AcquisitionDownload>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AcquisitionDownload>> GetPendingAutomaticImportsAsync(
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AcquisitionDownload>> GetPendingSeriesReconciliationAsync(
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AcquisitionDownload>> GetPendingMetadataRefreshAsync(
        CancellationToken cancellationToken = default);
    Task<AcquisitionDownload?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> HasActiveForMonitoringAsync(int monitoringTargetId, int? wantedItemId,
        CancellationToken cancellationToken = default);
    void Add(AcquisitionDownload download);
}

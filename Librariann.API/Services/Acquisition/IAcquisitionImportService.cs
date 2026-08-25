using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs.Acquisition;

namespace Librariann.API.Services.Acquisition;

public interface IAcquisitionImportService
{
    Task<IReadOnlyCollection<ImportDestinationOption>> GetDestinationsAsync(CancellationToken cancellationToken = default);
    Task<ImportAnalysisResult> AnalyzeAsync(int downloadId, CancellationToken cancellationToken = default);
    Task<CommitImportResult> CommitAsync(CommitImportRequest request, CancellationToken cancellationToken = default);
    Task ProcessPendingAutomaticImportsAsync(CancellationToken cancellationToken = default);
    Task ReconcileImportedSeriesAsync(CancellationToken cancellationToken = default);
    Task QueueImportedSeriesMetadataRefreshAsync(int downloadId,
        CancellationToken cancellationToken = default);
}

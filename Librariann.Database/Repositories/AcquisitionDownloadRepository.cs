using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Repositories;
using Librariann.Models.Entities.Acquisition;
using Microsoft.EntityFrameworkCore;

namespace Librariann.Database.Repositories;

public sealed class AcquisitionDownloadRepository(DataContext context) : IAcquisitionDownloadRepository
{
    public async Task<IReadOnlyList<AcquisitionDownload>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.AcquisitionDownloads.AsNoTracking().OrderByDescending(item => item.CreatedAtUtc).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AcquisitionDownload>> GetActiveAsync(CancellationToken cancellationToken = default) =>
        await context.AcquisitionDownloads
            .Where(item => item.Status == AcquisitionDownloadStatus.Queued || item.Status == AcquisitionDownloadStatus.Downloading)
            .OrderBy(item => item.CreatedAtUtc).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AcquisitionDownload>> GetPendingAutomaticImportsAsync(
        CancellationToken cancellationToken = default) =>
        await context.AcquisitionDownloads
            .Where(item => item.Status == AcquisitionDownloadStatus.ImportPending &&
                           item.MonitoringTargetId != null)
            .OrderBy(item => item.CompletedAtUtc)
            .ThenBy(item => item.Id)
            .Take(100)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AcquisitionDownload>> GetPendingSeriesReconciliationAsync(
        CancellationToken cancellationToken = default) =>
        await context.AcquisitionDownloads
            .Where(item => item.Status == AcquisitionDownloadStatus.Imported &&
                           item.ImportedSeriesId == null && item.ImportedPath != string.Empty)
            .OrderBy(item => item.ImportedAtUtc)
            .ThenBy(item => item.Id)
            .Take(100)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AcquisitionDownload>> GetPendingMetadataRefreshAsync(
        CancellationToken cancellationToken = default) =>
        await context.AcquisitionDownloads
            .Where(item => item.Status == AcquisitionDownloadStatus.Imported &&
                           item.ImportedSeriesId != null && item.MetadataRefreshQueuedAtUtc == null)
            .OrderBy(item => item.ImportedAtUtc)
            .ThenBy(item => item.Id)
            .Take(100)
            .ToListAsync(cancellationToken);

    public Task<AcquisitionDownload?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        context.AcquisitionDownloads.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<bool> HasActiveForMonitoringAsync(int monitoringTargetId, int? wantedItemId,
        CancellationToken cancellationToken = default) => context.AcquisitionDownloads.AsNoTracking().AnyAsync(item =>
        item.MonitoringTargetId == monitoringTargetId && item.WantedItemId == wantedItemId &&
        item.Status != AcquisitionDownloadStatus.Failed && item.Status != AcquisitionDownloadStatus.Removed &&
        item.Status != AcquisitionDownloadStatus.Imported, cancellationToken);

    public void Add(AcquisitionDownload download) => context.AcquisitionDownloads.Add(download);
}

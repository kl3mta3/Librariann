using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs.Acquisition;

namespace Librariann.API.Services.Acquisition;

public interface IMonitoringService
{
    Task<IReadOnlyCollection<MonitoringTargetDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<MonitoringSearchRunDto>> GetHistoryAsync(int? targetId, int take,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<WantedItemDto>> GetWantedAsync(int? targetId,
        CancellationToken cancellationToken = default);
    Task<MonitoringTargetDto> UpsertAsync(int userId, UpsertMonitoringTargetRequest request,
        CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task SearchNowAsync(int id, CancellationToken cancellationToken = default);
    Task SyncCatalogNowAsync(int id, CancellationToken cancellationToken = default);
}

public interface IMonitoringCatalogService
{
    Task SyncCatalogAsync(int targetId, CancellationToken cancellationToken = default);
    Task SyncAllAsync(CancellationToken cancellationToken = default);
}

public interface IMonitoringJobService
{
    Task SearchDueAsync(CancellationToken cancellationToken = default);
    Task SearchTargetAsync(int targetId, CancellationToken cancellationToken = default);
}

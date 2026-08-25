using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.Entities.Acquisition;

namespace Librariann.API.Repositories;

public sealed record OwnedLibraryTitle(int SeriesId, string Title);

public interface IMonitoringRepository
{
    Task<IReadOnlyList<MonitoringTarget>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MonitoringTarget>> GetDueAsync(CancellationToken cancellationToken = default);
    Task<MonitoringTarget?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MonitoringSearchRun>> GetHistoryAsync(int? targetId, int take,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WantedItem>> GetWantedAsync(int? targetId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WantedItem>> GetDueWantedAsync(int targetId, int take,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OwnedLibraryTitle>> GetOwnedTitlesAsync(CancellationToken cancellationToken = default);
    void Add(MonitoringTarget target);
    void AddRun(MonitoringSearchRun run);
    void AddWanted(WantedItem item);
    void Remove(MonitoringTarget target);
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Repositories;
using Librariann.Models.Entities.Acquisition;
using Microsoft.EntityFrameworkCore;

namespace Librariann.Database.Repositories;

public sealed class MonitoringRepository(DataContext context) : IMonitoringRepository
{
    public async Task<IReadOnlyList<MonitoringTarget>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.MonitoringTargets.AsNoTracking()
            .OrderBy(target => target.Title)
            .ThenBy(target => target.Kind)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<MonitoringTarget>> GetDueAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await context.MonitoringTargets
            .Where(target => target.IsEnabled && target.NextSearchAtUtc <= now)
            .OrderBy(target => target.NextSearchAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    public Task<MonitoringTarget?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        context.MonitoringTargets.FirstOrDefaultAsync(target => target.Id == id, cancellationToken);

    public async Task<IReadOnlyList<MonitoringSearchRun>> GetHistoryAsync(int? targetId, int take,
        CancellationToken cancellationToken = default)
    {
        var query = context.MonitoringSearchRuns.AsNoTracking().AsQueryable();
        if (targetId.HasValue) query = query.Where(run => run.MonitoringTargetId == targetId.Value);
        return await query.OrderByDescending(run => run.StartedAtUtc).Take(take).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WantedItem>> GetWantedAsync(int? targetId,
        CancellationToken cancellationToken = default)
    {
        var query = context.WantedItems.AsQueryable();
        if (targetId.HasValue) query = query.Where(item => item.MonitoringTargetId == targetId.Value);
        return await query.OrderBy(item => item.Series).ThenBy(item => item.Sequence).ThenBy(item => item.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WantedItem>> GetDueWantedAsync(int targetId, int take,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await context.WantedItems
            .Where(item => item.MonitoringTargetId == targetId && item.Status == WantedItemStatus.Missing &&
                           item.NextSearchAtUtc <= now)
            .OrderBy(item => item.NextSearchAtUtc)
            .ThenBy(item => item.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OwnedLibraryTitle>> GetOwnedTitlesAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await context.Series.AsNoTracking().Select(series => new {series.Id, series.Name})
            .ToListAsync(cancellationToken);
        return rows.Select(row => new OwnedLibraryTitle(row.Id, row.Name)).ToArray();
    }

    public void Add(MonitoringTarget target) => context.MonitoringTargets.Add(target);
    public void AddRun(MonitoringSearchRun run) => context.MonitoringSearchRuns.Add(run);
    public void AddWanted(WantedItem item) => context.WantedItems.Add(item);
    public void Remove(MonitoringTarget target) => context.MonitoringTargets.Remove(target);
}

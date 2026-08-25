using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.Mapping;
using Librariann.API.Repositories;
using Librariann.Common.Helpers;
using Librariann.Database.Extensions;
using Librariann.Models.DTOs.Dashboard;
using Librariann.Models.Entities.User;
using Microsoft.EntityFrameworkCore;

namespace Librariann.Database.Repositories;

public class AppUserSmartFilterRepository(DataContext context) : IAppUserSmartFilterRepository
{
    public void Update(AppUserSmartFilter filter)
    {
        context.Entry(filter).State = EntityState.Modified;
    }

    public void Attach(AppUserSmartFilter filter)
    {
        context.AppUserSmartFilter.Attach(filter);
    }

    public void Delete(AppUserSmartFilter filter)
    {
        context.AppUserSmartFilter.Remove(filter);
    }

    public async Task<IList<SmartFilterDto>> GetAllDtosByUserId(int userId, CancellationToken ct = default)
    {
        return await context.AppUserSmartFilter
            .Where(f => f.AppUserId == userId)
            .Select(SmartFilterMapping.ToSmartFilterDtoExpression)
            .ToListAsync(ct);
    }

    public Task<PagedList<SmartFilterDto>> GetPagedDtosByUserIdAsync(int userId, UserParams userParams,
        CancellationToken ct = default)
    {
        var filters = context.AppUserSmartFilter
            .Where(f => f.AppUserId == userId)
            .Select(SmartFilterMapping.ToSmartFilterDtoExpression);

        return PagedList<SmartFilterDto>.CreateAsync(filters, userParams, ct);
    }

    public async Task<AppUserSmartFilter?> GetById(int smartFilterId, CancellationToken ct = default)
    {
        return await context.AppUserSmartFilter
            .FirstOrDefaultAsync(d => d.Id == smartFilterId, ct);
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Repositories;
using Librariann.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Librariann.Database.Repositories;

public class InviteRequestRepository(DataContext context) : IInviteRequestRepository
{
    public void Add(AppUserInviteRequest request) => context.AppUserInviteRequest.Add(request);
    public void Delete(AppUserInviteRequest request) => context.AppUserInviteRequest.Remove(request);

    public async Task<AppUserInviteRequest?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await context.AppUserInviteRequest.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<AppUserInviteRequest?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await context.AppUserInviteRequest.FirstOrDefaultAsync(r => r.Email.Equals(email), ct);

    public async Task<IList<AppUserInviteRequest>> GetAllAsync(CancellationToken ct = default) =>
        await context.AppUserInviteRequest.OrderBy(r => r.CreatedUtc).ToListAsync(ct);

    public async Task<int> GetCountAsync(CancellationToken ct = default) =>
        await context.AppUserInviteRequest.CountAsync(ct);
}

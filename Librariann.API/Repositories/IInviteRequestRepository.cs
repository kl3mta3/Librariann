using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.Entities;

namespace Librariann.API.Repositories;

public interface IInviteRequestRepository
{
    void Add(AppUserInviteRequest request);
    void Delete(AppUserInviteRequest request);
    Task<AppUserInviteRequest?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<AppUserInviteRequest?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<IList<AppUserInviteRequest>> GetAllAsync(CancellationToken ct = default);
    Task<int> GetCountAsync(CancellationToken ct = default);
}

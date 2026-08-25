using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs.SideNav;
using Librariann.Models.Entities.User;

namespace Librariann.API.Repositories;

public interface IAppUserExternalSourceRepository
{
    void Update(AppUserExternalSource source);
    void Delete(AppUserExternalSource source);
    Task<AppUserExternalSource?> GetById(int externalSourceId, CancellationToken ct = default);
    Task<IList<AppUserExternalSource>> GetAll(CancellationToken ct = default);
    Task<IList<ExternalSourceDto>> GetExternalSources(int userId, CancellationToken ct = default);
    Task<bool> ExternalSourceExists(int userId, string name, string host, string apiKey, CancellationToken ct = default);
}

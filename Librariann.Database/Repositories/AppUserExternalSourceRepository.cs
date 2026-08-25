using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.Mapping;
using Librariann.API.Repositories;
using Librariann.API.Services;
using Librariann.Common.Helpers;
using Librariann.Models.DTOs.SideNav;
using Librariann.Models.Entities.User;
using Microsoft.EntityFrameworkCore;

namespace Librariann.Database.Repositories;


public class AppUserExternalSourceRepository(DataContext context,
    ICredentialProtectionService credentialProtectionService) : IAppUserExternalSourceRepository
{

    public void Update(AppUserExternalSource source)
    {
        context.AppUserExternalSource.Update(source);
    }

    public void Delete(AppUserExternalSource source)
    {
        context.AppUserExternalSource.Remove(source);
    }

    public async Task<AppUserExternalSource?> GetById(int externalSourceId, CancellationToken ct = default)
    {
        return await context.AppUserExternalSource
            .Where(s => s.Id == externalSourceId)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IList<AppUserExternalSource>> GetAll(CancellationToken ct = default)
    {
        return await context.AppUserExternalSource.ToListAsync(ct);
    }

    public async Task<IList<ExternalSourceDto>> GetExternalSources(int userId, CancellationToken ct = default)
    {
        var sources = await context.AppUserExternalSource.Where(s => s.AppUserId == userId)
            .Select(ExternalSourceMapping.ToExternalSourceDtoExpression)
            .ToListAsync(ct);
        foreach (var source in sources)
        {
            if (credentialProtectionService.IsProtected(source.ApiKey))
            {
                source.ApiKey = credentialProtectionService.Unprotect(source.ApiKey,
                    ServerSettingCredentialScopes.ExternalSourceApiKey(userId));
            }
        }

        return sources;
    }

    /// <summary>
    /// Checks if all the properties match exactly. This will allow a user to setup 2 External Sources with different Users
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="name"></param>
    /// <param name="host"></param>
    /// <param name="apiKey"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<bool> ExternalSourceExists(int userId, string name, string host, string apiKey,
        CancellationToken ct = default)
    {
        host = host.Trim();
        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(apiKey)) return false;
        var hostWithEndingSlash = UrlHelper.EnsureEndsWithSlash(host)!;
        var candidateKeys = await context.AppUserExternalSource
            .Where(s => s.AppUserId == userId )
            .Where(s => s.Host.ToUpper().Equals(hostWithEndingSlash.ToUpper())
                        && s.Name.ToUpper().Equals(name.ToUpper()))
            .Select(s => s.ApiKey)
            .ToListAsync(ct);
        return candidateKeys.Any(candidate =>
        {
            var plaintext = credentialProtectionService.IsProtected(candidate)
                ? credentialProtectionService.Unprotect(candidate,
                    ServerSettingCredentialScopes.ExternalSourceApiKey(userId))
                : candidate;
            return string.Equals(plaintext, apiKey, System.StringComparison.Ordinal);
        });
    }
}

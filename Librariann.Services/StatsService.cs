using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Database;
using Librariann.API.Services;
using Librariann.Common.EnvironmentInfo;
using Librariann.Models.DTOs.Stats;

namespace Librariann.Services;

/// <summary>
/// Local, in-app server info only. This never leaves the instance - Librariann does not report any
/// usage data to an outside server.
/// </summary>
public class StatsService(IUnitOfWork unitOfWork) : IStatsService
{
    public async Task<ServerInfoSlimDto> GetServerInfoSlim(CancellationToken ct = default)
    {
        var serverSettings = await unitOfWork.SettingsRepository.GetSettingsDtoAsync(ct);
        return new ServerInfoSlimDto()
        {
            InstallId = serverSettings.InstallId,
            LibrariannVersion = serverSettings.InstallVersion,
            IsDocker = OsInfo.IsDocker,
            FirstInstallDate = serverSettings.FirstInstallDate,
            FirstInstallVersion = serverSettings.FirstInstallVersion
        };
    }
}

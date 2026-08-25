using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs.Stats;

namespace Librariann.API.Services;

public interface IStatsService
{
    Task<ServerInfoSlimDto> GetServerInfoSlim(CancellationToken ct = default);
}

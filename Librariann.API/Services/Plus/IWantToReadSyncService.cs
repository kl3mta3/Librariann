using System.Threading;
using System.Threading.Tasks;

namespace Librariann.API.Services.Plus;

public interface IWantToReadSyncService
{
    Task Sync(CancellationToken ct = default);
}

using System.Threading;
using System.Threading.Tasks;

namespace Librariann.API.Services;

public interface IActiveUserTrackerService
{
    void RecordActive(int userId);
    Task FlushAsync(CancellationToken ct = default);
}

using System.Threading;
using System.Threading.Tasks;

namespace Librariann.API.Services.Reading;

public interface IReadingHistoryService
{
    Task AggregateYesterdaysActivity(CancellationToken ct = default);
}

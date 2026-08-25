using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs.Acquisition;

namespace Librariann.API.Services.Acquisition;

public interface IAcquisitionQueueService
{
    Task<IReadOnlyCollection<AcquisitionDownloadDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task PollAsync(CancellationToken cancellationToken = default);
    Task RetryAsync(int downloadId, CancellationToken cancellationToken = default);
    Task RemoveAsync(int downloadId, bool deleteData, CancellationToken cancellationToken = default);
}

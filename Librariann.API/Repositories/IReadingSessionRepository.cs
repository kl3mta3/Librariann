using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs.Progress;

namespace Librariann.API.Repositories;

public interface IReadingSessionRepository
{
    Task<IList<ReadingSessionDto>> GetAllReadingSessionAsync(bool isActiveOnly = true, CancellationToken ct = default);
}

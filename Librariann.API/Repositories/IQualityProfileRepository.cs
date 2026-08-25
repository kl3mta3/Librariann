using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.Entities.Acquisition;

namespace Librariann.API.Repositories;

public interface IQualityProfileRepository
{
    Task<IReadOnlyList<QualityProfile>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<QualityProfile?> GetAsync(int id, CancellationToken cancellationToken = default);
    void Add(QualityProfile profile);
    void Remove(QualityProfile profile);
}

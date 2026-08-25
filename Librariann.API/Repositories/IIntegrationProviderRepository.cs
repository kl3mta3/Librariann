using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.Entities.Acquisition;

namespace Librariann.API.Repositories;

public interface IIntegrationProviderRepository
{
    Task<IReadOnlyList<IntegrationProviderConfiguration>> GetAllAsync(CancellationToken ct = default);
    Task<IntegrationProviderConfiguration?> GetAsync(int id, CancellationToken ct = default);
    void Add(IntegrationProviderConfiguration configuration);
    void Update(IntegrationProviderConfiguration configuration);
    void Remove(IntegrationProviderConfiguration configuration);
}


using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs.Acquisition;

namespace Librariann.API.Services.Acquisition;

public interface IIntegrationProviderService
{
    Task<IReadOnlyList<IntegrationProviderDto>> GetAllAsync(CancellationToken ct = default);
    Task<IntegrationProviderDto> CreateAsync(UpsertIntegrationProviderDto dto, CancellationToken ct = default);
    Task<IntegrationProviderDto> UpdateAsync(UpsertIntegrationProviderDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}


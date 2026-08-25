using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs.Acquisition;

namespace Librariann.API.Services.Acquisition;

public interface IQualityProfileService
{
    Task<IReadOnlyCollection<QualityProfileDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<QualityProfileDto> UpsertAsync(UpsertQualityProfileRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

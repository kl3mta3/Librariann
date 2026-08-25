using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs.Acquisition;

namespace Librariann.API.Services.Acquisition;

public interface IIntegrationProviderTestService
{
    Task<ProviderTestResult> TestAsync(int providerId, CancellationToken cancellationToken = default);
}

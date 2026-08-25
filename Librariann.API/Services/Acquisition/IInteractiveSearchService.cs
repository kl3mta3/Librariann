using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs.Acquisition;

namespace Librariann.API.Services.Acquisition;

public interface IInteractiveSearchService
{
    Task<InteractiveSearchResponse> SearchAsync(int userId, InteractiveSearchRequest request,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Runs the same provider search and evaluation without issuing browser grab tokens. Intended only for
    /// trusted background automation and audit generation.
    /// </summary>
    Task<InteractiveSearchResponse> SearchForAutomationAsync(InteractiveSearchRequest request,
        CancellationToken cancellationToken = default);
}

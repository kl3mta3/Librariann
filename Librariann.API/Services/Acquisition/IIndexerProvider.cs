using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs.Acquisition;

namespace Librariann.API.Services.Acquisition;

public interface IIndexerProvider
{
    string ProviderKey { get; }
    IndexerProtocol Protocol { get; }
    Task<ProviderTestResult> TestAsync(CancellationToken cancellationToken = default);
    Task<IndexerCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ReleaseCandidate>> SearchAsync(IndexerSearchRequest request,
        CancellationToken cancellationToken = default);
}


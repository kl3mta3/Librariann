using System;
using System.Threading;
using System.Threading.Tasks;

namespace Librariann.API.Services.Acquisition;

public interface IIntegrationEndpointValidator
{
    /// <summary>
    /// Resolves and validates a provider endpoint before it is stored or contacted.
    /// Private/loopback endpoints require explicit opt-in for self-hosted clients.
    /// Link-local and metadata-service ranges are never allowed.
    /// </summary>
    Task<Uri> ValidateAsync(string url, bool allowPrivateNetwork, CancellationToken cancellationToken = default);
}


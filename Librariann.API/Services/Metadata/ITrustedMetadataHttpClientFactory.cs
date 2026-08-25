using System.Net.Http;
using System.Threading.Tasks;

namespace Librariann.API.Services.Metadata;

/// <summary>
/// Creates clients for fixed, application-owned public metadata hosts. This must not be used for user-configurable
/// integration URLs, which require <c>IIntegrationHttpClientFactory</c> and its SSRF protections.
/// </summary>
public interface ITrustedMetadataHttpClientFactory
{
    Task<HttpClient> CreateOpenLibraryClient();
}

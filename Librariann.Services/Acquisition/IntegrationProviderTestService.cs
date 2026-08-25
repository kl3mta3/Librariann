using System;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Database;
using Librariann.API.Services;
using Librariann.API.Services.Acquisition;
using Librariann.API.Services.Metadata;
using Librariann.Common;
using Librariann.Models.DTOs.Acquisition;
using Librariann.Models.Entities.Acquisition;

namespace Librariann.Services.Acquisition;

public sealed class IntegrationProviderTestService(
    IUnitOfWork unitOfWork,
    IDownloadClientFactory downloadClientFactory,
    IMetadataProviderFactory metadataProviderFactory,
    IIntegrationHttpClientFactory httpClientFactory,
    ICredentialProtectionService credentialProtection) : IIntegrationProviderTestService
{
    public async Task<ProviderTestResult> TestAsync(int providerId, CancellationToken cancellationToken = default)
    {
        var configuration = await unitOfWork.IntegrationProviderRepository.GetAsync(providerId, cancellationToken)
                            ?? throw new LibrariannException("integration-provider-does-not-exist");
        if (configuration.Category == IntegrationProviderCategory.DownloadClient)
        {
            using var downloadClient = downloadClientFactory.Create(configuration);
            return await downloadClient.TestAsync(cancellationToken);
        }

        if (configuration.Category == IntegrationProviderCategory.Metadata)
        {
            using var metadataProvider = metadataProviderFactory.Create(configuration);
            return await metadataProvider.TestAsync(cancellationToken);
        }

        if (configuration.Category != IntegrationProviderCategory.Indexer || configuration.IndexerProtocol is null)
            throw new LibrariannException("integration-provider-test-not-supported");

        var httpClient = httpClientFactory.Create(configuration);
        var username = ReadSecret(configuration, configuration.ProtectedUsername, "username");
        var password = ReadSecret(configuration, configuration.ProtectedPassword, "password");
        if (!string.IsNullOrWhiteSpace(username))
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")));
        }
        using var provider = new NewznabIndexerProvider($"integration-{configuration.Id}",
            configuration.IndexerProtocol.Value, httpClient, ReadSecret(configuration, configuration.ProtectedApiKey, "api-key"));
        return await provider.TestAsync(cancellationToken);
    }

    private string ReadSecret(IntegrationProviderConfiguration configuration, string value, string field) =>
        string.IsNullOrEmpty(value) ? string.Empty : credentialProtection.Unprotect(value, IntegrationCredentialScope.For(configuration, field));
}

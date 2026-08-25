using System;
using System.Net.Http.Headers;
using System.Text;
using Librariann.API.Services;
using Librariann.API.Services.Acquisition;
using Librariann.Common;
using Librariann.Models.DTOs.Acquisition;
using Librariann.Models.Entities.Acquisition;

namespace Librariann.Services.Acquisition;

public sealed class DownloadClientFactory(
    IIntegrationHttpClientFactory httpClientFactory,
    ICredentialProtectionService credentialProtection) : IDownloadClientFactory
{
    public IDownloadClient Create(IntegrationProviderConfiguration configuration)
    {
        if (configuration.Category != IntegrationProviderCategory.DownloadClient || configuration.DownloadClientKind is null)
            throw new LibrariannException("integration-provider-is-not-download-client");

        var client = httpClientFactory.Create(configuration);
        var username = ReadSecret(configuration, configuration.ProtectedUsername, "username");
        var password = ReadSecret(configuration, configuration.ProtectedPassword, "password");
        var apiKey = ReadSecret(configuration, configuration.ProtectedApiKey, "api-key");
        var providerKey = $"integration-{configuration.Id}";

        return configuration.DownloadClientKind.Value switch
        {
            DownloadClientKind.QBittorrent => new QBittorrentDownloadClient(providerKey, client, username, password),
            DownloadClientKind.Sabnzbd => new SabnzbdDownloadClient(providerKey, client, apiKey),
            DownloadClientKind.UTorrent => new UTorrentDownloadClient(providerKey, AddBasicAuthentication(client, username, password)),
            _ => throw new LibrariannException("download-client-not-supported"),
        };
    }

    private string ReadSecret(IntegrationProviderConfiguration configuration, string protectedValue, string field) =>
        string.IsNullOrEmpty(protectedValue)
            ? string.Empty
            : credentialProtection.Unprotect(protectedValue, IntegrationCredentialScope.For(configuration, field));

    private static System.Net.Http.HttpClient AddBasicAuthentication(System.Net.Http.HttpClient client, string username, string password)
    {
        var value = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", value);
        return client;
    }
}

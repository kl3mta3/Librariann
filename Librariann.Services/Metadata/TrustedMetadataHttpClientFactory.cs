using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Librariann.API.Database;
using Librariann.API.Services.Metadata;
using Librariann.Models.Entities.Enums;
using Librariann.Services.Metadata.Providers;

namespace Librariann.Services.Metadata;

public sealed class TrustedMetadataHttpClientFactory(IUnitOfWork unitOfWork) : ITrustedMetadataHttpClientFactory
{
    public async Task<HttpClient> CreateOpenLibraryClient()
    {
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.All,
            ConnectTimeout = TimeSpan.FromSeconds(10),
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        };

        var client = new HttpClient(handler, disposeHandler: true)
        {
            BaseAddress = new Uri("https://openlibrary.org/"),
            Timeout = TimeSpan.FromSeconds(30),
        };

        // A contact email in the User-Agent is Open Library's documented convention for an "identified" client
        // (3 req/s instead of 1 req/s) - no account or API key involved. Falls back to a plain description.
        var contactEmail = (await unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.MetadataProviderContactEmail)).Value;
        var identified = !string.IsNullOrWhiteSpace(contactEmail);
        client.DefaultRequestHeaders.UserAgent.ParseAdd(identified
            ? $"Librariann/1.0 ({contactEmail.Trim()})"
            : "Librariann/1.0 (self-hosted library manager)");
        OpenLibraryMetadataProvider.ConfigureThrottle(identified);

        return client;
    }
}

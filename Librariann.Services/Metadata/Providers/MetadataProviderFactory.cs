using Librariann.API.Database;
using Librariann.API.Services;
using Librariann.API.Services.Acquisition;
using Librariann.API.Services.Metadata;
using Librariann.Common;
using Librariann.Models.Entities.Acquisition;
using Librariann.Models.Entities.Enums;
using Librariann.Services.Acquisition;

namespace Librariann.Services.Metadata.Providers;

public sealed class MetadataProviderFactory(
    IIntegrationHttpClientFactory httpClientFactory,
    ICredentialProtectionService credentialProtection,
    IUnitOfWork unitOfWork) : IMetadataProviderFactory
{
    public IMetadataProvider Create(IntegrationProviderConfiguration configuration)
    {
        if (configuration.Category != IntegrationProviderCategory.Metadata)
            throw new LibrariannException("integration-provider-is-not-metadata-provider");
        var type = configuration.ProviderType.Trim().ToLowerInvariant().Replace("_", "-").Replace(" ", "-");
        return type switch
        {
            "openlibrary" or "open-library" => CreateOpenLibraryProvider(configuration),
            "googlebooks" or "google-books" => new GoogleBooksMetadataProvider(httpClientFactory.Create(configuration),
                ReadApiKey(configuration)),
            "anilist" or "ani-list" => new AniListMetadataProvider(httpClientFactory.Create(configuration)),
            "mangadex" or "manga-dex" => new MangaDexMetadataProvider(httpClientFactory.Create(configuration)),
            "comicvine" or "comic-vine" => new ComicVineMetadataProvider(httpClientFactory.Create(configuration),
                ReadApiKey(configuration)),
            _ => throw new LibrariannException("unsupported-metadata-provider"),
        };
    }

    /// <summary>
    /// A contact email in the User-Agent is Open Library's documented convention for an "identified" client
    /// (3 req/s instead of 1 req/s) - no account or API key involved. Read here (rather than in
    /// IIntegrationHttpClientFactory, which is shared by every indexer/download-client/provider) so this stays
    /// scoped to Open Library specifically.
    /// </summary>
    private OpenLibraryMetadataProvider CreateOpenLibraryProvider(IntegrationProviderConfiguration configuration)
    {
        var client = httpClientFactory.Create(configuration);
        var contactEmail = unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.MetadataProviderContactEmail)
            .GetAwaiter().GetResult().Value;
        var identified = !string.IsNullOrWhiteSpace(contactEmail);

        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(identified
            ? $"Librariann/1.0 ({contactEmail.Trim()})"
            : "Librariann/1.0 (self-hosted library manager)");
        OpenLibraryMetadataProvider.ConfigureThrottle(identified);

        return new OpenLibraryMetadataProvider(client);
    }

    private string ReadApiKey(IntegrationProviderConfiguration configuration) =>
        string.IsNullOrWhiteSpace(configuration.ProtectedApiKey)
            ? string.Empty
            : credentialProtection.Unprotect(configuration.ProtectedApiKey,
                IntegrationCredentialScope.For(configuration, "api-key"));
}

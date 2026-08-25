using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Services.Metadata;
using Librariann.Models.DTOs.Acquisition;
using Librariann.Models.DTOs.Metadata;

namespace Librariann.Services.Metadata.Providers;

/// <summary>Public MangaDex title-metadata provider. Librariann does not use it to fetch chapters.</summary>
public sealed class MangaDexMetadataProvider(HttpClient httpClient) : IMetadataProvider
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    public string Key => "mangadex";
    public string Name => "MangaDex";
    public IReadOnlySet<LibrariannMediaType> SupportedMediaTypes { get; } =
        new HashSet<LibrariannMediaType> {LibrariannMediaType.Manga};

    public async Task<ProviderTestResult> TestAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await SearchCoreAsync("Librariann", 1, false, cancellationToken);
            return new ProviderTestResult(true, "Connected to MangaDex.", stopwatch.Elapsed);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new ProviderTestResult(false, "MangaDex could not be reached.", stopwatch.Elapsed);
        }
    }

    public async Task<IReadOnlyCollection<NormalizedMetadataCandidate>> SearchAsync(MetadataLookupRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.MediaType != LibrariannMediaType.Manga) return [];
        var search = !string.IsNullOrWhiteSpace(request.Title) ? request.Title.Trim() : request.Series.Trim();
        if (search.Length == 0) throw new IOException("MangaDex manga search requires a title or series.");
        return (await SearchCoreAsync(search, 20, request.IncludeAdult, cancellationToken))
            .Select(item => ToCandidate(item, request.Language))
            .Where(candidate => candidate.Title.Length > 0)
            .ToArray();
    }

    public void Dispose() => httpClient.Dispose();

    private async Task<IReadOnlyCollection<MangaDexTitle>> SearchCoreAsync(string title, int limit, bool includeAdult,
        CancellationToken cancellationToken)
    {
        var path = "manga?title=" + Uri.EscapeDataString(title) +
                   $"&limit={limit}&includes%5B%5D=author&includes%5B%5D=artist&includes%5B%5D=cover_art" +
                   "&contentRating%5B%5D=safe&contentRating%5B%5D=suggestive";
        if (includeAdult)
            path += "&contentRating%5B%5D=erotica&contentRating%5B%5D=pornographic";
        using var response = await httpClient.GetAsync(path, HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = JsonSerializer.Deserialize<MangaDexResponse>(
            await MetadataProviderResponseReader.ReadAsync(response, cancellationToken), SerializerOptions);
        if (!string.Equals(payload?.Result, "ok", StringComparison.OrdinalIgnoreCase))
            throw new IOException("MangaDex returned an invalid response.");
        return payload!.Data?.Where(item => Guid.TryParse(item.Id, out _) && item.Attributes is not null)
            .Take(20).ToArray() ?? [];
    }

    private static NormalizedMetadataCandidate ToCandidate(MangaDexTitle item, string requestedLanguage)
    {
        var attributes = item.Attributes!;
        var title = Localized(attributes.Title, requestedLanguage);
        var alternates = (attributes.AlternateTitles ?? [])
            .SelectMany(value => value.Values)
            .Where(value => !string.IsNullOrWhiteSpace(value) && !value.Equals(title, StringComparison.OrdinalIgnoreCase))
            .Select(value => value.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Take(30).ToArray();
        var people = (item.Relationships ?? [])
            .Where(relation => relation.Type is "author" or "artist")
            .Select(relation => relation.Attributes?.Name)
            .Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase).Take(20).ToArray();
        var coverFile = item.Relationships?.FirstOrDefault(relation => relation.Type == "cover_art")
            ?.Attributes?.FileName;
        var identifiers = new Dictionary<string, string> {{"mangadex", item.Id}};
        CopyIdentifier(attributes.Links, identifiers, "al", "anilist");
        CopyIdentifier(attributes.Links, identifiers, "mal", "myanimelist");
        CopyIdentifier(attributes.Links, identifiers, "mu", "mangaupdates");

        return new NormalizedMetadataCandidate
        {
            ProviderKey = "mangadex", ProviderName = "MangaDex", ExternalId = item.Id,
            MediaType = LibrariannMediaType.Manga,
            IsAdult = attributes.ContentRating is "erotica" or "pornographic",
            Title = title, AlternateTitles = alternates, Authors = people,
            PublicationYear = attributes.Year,
            Languages = string.IsNullOrWhiteSpace(attributes.OriginalLanguage) ? [] : [attributes.OriginalLanguage],
            Genres = attributes.Tags?.Select(tag => Localized(tag.Attributes?.Name, "en"))
                .Where(value => value.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).Take(30).ToArray() ?? [],
            Description = Localized(attributes.Description, requestedLanguage),
            CoverUri = string.IsNullOrWhiteSpace(coverFile) ? null :
                new Uri($"https://uploads.mangadex.org/covers/{item.Id}/{Uri.EscapeDataString(coverFile)}.512.jpg"),
            DetailsUri = new Uri($"https://mangadex.org/title/{item.Id}"), Identifiers = identifiers,
        };
    }

    private static string Localized(IReadOnlyDictionary<string, string>? values, string requestedLanguage)
    {
        if (values is null || values.Count == 0) return string.Empty;
        var language = NormalizeLanguage(requestedLanguage);
        if (language.Length > 0 && values.TryGetValue(language, out var requested) && !string.IsNullOrWhiteSpace(requested))
            return requested.Trim();
        if (values.TryGetValue("en", out var english) && !string.IsNullOrWhiteSpace(english)) return english.Trim();
        return values.Values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;
    }

    private static string NormalizeLanguage(string value)
    {
        var language = value.Trim().ToLowerInvariant();
        if (language.Length == 0) return string.Empty;
        try { return CultureInfo.GetCultureInfo(language).TwoLetterISOLanguageName; }
        catch (CultureNotFoundException) { return language.Length >= 2 ? language[..2] : language; }
    }

    private static void CopyIdentifier(IReadOnlyDictionary<string, string>? source,
        IDictionary<string, string> destination, string sourceKey, string destinationKey)
    {
        if (source?.TryGetValue(sourceKey, out var value) == true && !string.IsNullOrWhiteSpace(value))
            destination[destinationKey] = value.Trim();
    }

    private sealed record MangaDexResponse
    {
        [JsonPropertyName("result")] public string Result { get; init; } = string.Empty;
        [JsonPropertyName("data")] public IReadOnlyCollection<MangaDexTitle>? Data { get; init; }
    }
    private sealed record MangaDexTitle
    {
        [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
        [JsonPropertyName("attributes")] public MangaDexAttributes? Attributes { get; init; }
        [JsonPropertyName("relationships")] public IReadOnlyCollection<MangaDexRelationship>? Relationships { get; init; }
    }
    private sealed record MangaDexAttributes
    {
        [JsonPropertyName("title")] public IReadOnlyDictionary<string, string>? Title { get; init; }
        [JsonPropertyName("altTitles")] public IReadOnlyCollection<IReadOnlyDictionary<string, string>>? AlternateTitles { get; init; }
        [JsonPropertyName("description")] public IReadOnlyDictionary<string, string>? Description { get; init; }
        [JsonPropertyName("originalLanguage")] public string OriginalLanguage { get; init; } = string.Empty;
        [JsonPropertyName("year")] public int? Year { get; init; }
        [JsonPropertyName("contentRating")] public string ContentRating { get; init; } = string.Empty;
        [JsonPropertyName("tags")] public IReadOnlyCollection<MangaDexTag>? Tags { get; init; }
        [JsonPropertyName("links")] public IReadOnlyDictionary<string, string>? Links { get; init; }
    }
    private sealed record MangaDexTag([property: JsonPropertyName("attributes")] MangaDexTagAttributes? Attributes);
    private sealed record MangaDexTagAttributes([property: JsonPropertyName("name")] IReadOnlyDictionary<string, string>? Name);
    private sealed record MangaDexRelationship
    {
        [JsonPropertyName("type")] public string Type { get; init; } = string.Empty;
        [JsonPropertyName("attributes")] public MangaDexRelationshipAttributes? Attributes { get; init; }
    }
    private sealed record MangaDexRelationshipAttributes
    {
        [JsonPropertyName("name")] public string? Name { get; init; }
        [JsonPropertyName("fileName")] public string? FileName { get; init; }
    }
}

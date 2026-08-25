using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Services.Metadata;
using Librariann.Common;
using Librariann.Models.DTOs.Acquisition;
using Librariann.Models.DTOs.Metadata;

namespace Librariann.Services.Metadata.Providers;

public sealed class GoogleBooksMetadataProvider(HttpClient httpClient, string apiKey) : IMetadataProvider
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public string Key => "google-books";
    public string Name => "Google Books";
    public IReadOnlySet<LibrariannMediaType> SupportedMediaTypes { get; } =
        new HashSet<LibrariannMediaType> {LibrariannMediaType.Book};

    public async Task<ProviderTestResult> TestAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var response = await httpClient.GetAsync(BuildPath("librariann", null, 1),
                HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            await MetadataProviderResponseReader.ReadAsync(response, cancellationToken);
            return new ProviderTestResult(true, "Connected to Google Books.", stopwatch.Elapsed);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new ProviderTestResult(false, "Google Books could not be reached.", stopwatch.Elapsed);
        }
    }

    public async Task<IReadOnlyCollection<NormalizedMetadataCandidate>> SearchAsync(MetadataLookupRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.MediaType != LibrariannMediaType.Book) return [];
        var query = BuildQuery(request);
        using var response = await httpClient.GetAsync(BuildPath(query, request.Language, 20),
            HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = JsonSerializer.Deserialize<GoogleBooksResponse>(
            await MetadataProviderResponseReader.ReadAsync(response, cancellationToken), SerializerOptions);
        if (payload?.Items is null) return [];
        return payload.Items.Where(item => !string.IsNullOrWhiteSpace(item.Id) && !string.IsNullOrWhiteSpace(item.VolumeInfo?.Title))
            .Take(20)
            .Select(item => ToCandidate(item))
            .ToArray();
    }

    public void Dispose() => httpClient.Dispose();

    private string BuildPath(string query, string? language, int maximumResults)
    {
        var path = $"volumes?q={Uri.EscapeDataString(query)}&maxResults={maximumResults}&projection=full&printType=books";
        if (!string.IsNullOrWhiteSpace(language)) path += "&langRestrict=" + Uri.EscapeDataString(language.Trim());
        if (!string.IsNullOrWhiteSpace(apiKey)) path += "&key=" + Uri.EscapeDataString(apiKey);
        return path;
    }

    private static string BuildQuery(MetadataLookupRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Isbn))
            return "isbn:" + new string(request.Isbn.Where(char.IsLetterOrDigit).ToArray());
        var terms = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Title)) terms.Add($"intitle:\"{request.Title.Trim()}\"");
        if (!string.IsNullOrWhiteSpace(request.Author)) terms.Add($"inauthor:\"{request.Author.Trim()}\"");
        if (terms.Count == 0) throw new LibrariannException("metadata-search-requires-title-author-or-isbn");
        return string.Join(' ', terms);
    }

    private NormalizedMetadataCandidate ToCandidate(GoogleBook item)
    {
        var info = item.VolumeInfo!;
        var isbns = info.IndustryIdentifiers?
            .Where(identifier => identifier.Type is "ISBN_10" or "ISBN_13")
            .Select(identifier => identifier.Identifier)
            .Where(identifier => !string.IsNullOrWhiteSpace(identifier))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
        var cover = SecureUri(info.ImageLinks?.Large ?? info.ImageLinks?.Medium ?? info.ImageLinks?.Thumbnail);
        return new NormalizedMetadataCandidate
        {
            ProviderKey = Key,
            ProviderName = Name,
            ExternalId = item.Id,
            MediaType = LibrariannMediaType.Book,
            IsAdult = string.Equals(info.MaturityRating, "MATURE", StringComparison.OrdinalIgnoreCase),
            Title = info.Title!.Trim(),
            AlternateTitles = string.IsNullOrWhiteSpace(info.Subtitle) ? [] : [$"{info.Title}: {info.Subtitle}"],
            Authors = Limit(info.Authors),
            PublicationYear = ParseYear(info.PublishedDate),
            Languages = string.IsNullOrWhiteSpace(info.Language) ? [] : [info.Language],
            Isbns = isbns,
            Publishers = string.IsNullOrWhiteSpace(info.Publisher) ? [] : [info.Publisher],
            Genres = Limit(info.Categories),
            Description = info.Description?.Trim() ?? string.Empty,
            CoverUri = cover,
            DetailsUri = SecureUri(info.InfoLink),
            Identifiers = new Dictionary<string, string> {{"google-books", item.Id}},
        };
    }

    private static IReadOnlyCollection<string> Limit(IEnumerable<string>? values) => values?
        .Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase).Take(30).ToArray() ?? [];

    private static int? ParseYear(string? value) => value?.Length >= 4 &&
        int.TryParse(value[..4], NumberStyles.None, CultureInfo.InvariantCulture, out var year) ? year : null;

    private static Uri? SecureUri(string? value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)) return null;
        if (uri.Scheme == Uri.UriSchemeHttp)
            return new UriBuilder(uri) {Scheme = Uri.UriSchemeHttps, Port = -1}.Uri;
        return uri.Scheme == Uri.UriSchemeHttps ? uri : null;
    }

    private sealed record GoogleBooksResponse
    {
        [JsonPropertyName("items")] public IReadOnlyCollection<GoogleBook>? Items { get; init; }
    }

    private sealed record GoogleBook
    {
        [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
        [JsonPropertyName("volumeInfo")] public GoogleVolumeInfo? VolumeInfo { get; init; }
    }

    private sealed record GoogleVolumeInfo
    {
        [JsonPropertyName("title")] public string? Title { get; init; }
        [JsonPropertyName("subtitle")] public string? Subtitle { get; init; }
        [JsonPropertyName("authors")] public IReadOnlyCollection<string>? Authors { get; init; }
        [JsonPropertyName("publisher")] public string? Publisher { get; init; }
        [JsonPropertyName("publishedDate")] public string? PublishedDate { get; init; }
        [JsonPropertyName("description")] public string? Description { get; init; }
        [JsonPropertyName("industryIdentifiers")] public IReadOnlyCollection<GoogleIdentifier>? IndustryIdentifiers { get; init; }
        [JsonPropertyName("categories")] public IReadOnlyCollection<string>? Categories { get; init; }
        [JsonPropertyName("imageLinks")] public GoogleImageLinks? ImageLinks { get; init; }
        [JsonPropertyName("infoLink")] public string? InfoLink { get; init; }
        [JsonPropertyName("language")] public string? Language { get; init; }
        [JsonPropertyName("maturityRating")] public string? MaturityRating { get; init; }
    }

    private sealed record GoogleIdentifier
    {
        [JsonPropertyName("type")] public string Type { get; init; } = string.Empty;
        [JsonPropertyName("identifier")] public string Identifier { get; init; } = string.Empty;
    }

    private sealed record GoogleImageLinks
    {
        [JsonPropertyName("thumbnail")] public string? Thumbnail { get; init; }
        [JsonPropertyName("medium")] public string? Medium { get; init; }
        [JsonPropertyName("large")] public string? Large { get; init; }
    }
}

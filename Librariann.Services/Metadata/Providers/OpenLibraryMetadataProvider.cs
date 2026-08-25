using System;
using System.Collections.Generic;
using System.Diagnostics;
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

public sealed partial class OpenLibraryMetadataProvider(HttpClient httpClient) : IMetadataProvider, IMetadataCatalogProvider
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    // Open Library's published API guidelines (openlibrary.org/developers/api): 1 request/second for
    // unidentified clients, and explicitly "Please Do Not... make hundreds of single-book requests". This
    // throttle is shared across every instance/request so a bulk caller (matching a whole library) can't
    // accidentally hammer them - single lookups from the UI pay at most a ~1s worst-case wait too.
    private static readonly SemaphoreSlim ThrottleLock = new(1, 1);
    private static DateTime _lastRequestUtc = DateTime.MinValue;
    private static readonly TimeSpan UnidentifiedInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan IdentifiedInterval = TimeSpan.FromSeconds(1.0 / 3);
    private static TimeSpan MinRequestInterval = UnidentifiedInterval;

    /// <summary>
    /// Called by whatever builds this provider's HttpClient once it knows whether a contact email was set in
    /// the User-Agent (see ServerSettingKey.MetadataProviderContactEmail) - Open Library grants "identified"
    /// clients 3 req/s instead of 1 req/s for exactly that, no account or API key involved.
    /// </summary>
    public static void ConfigureThrottle(bool identified) =>
        MinRequestInterval = identified ? IdentifiedInterval : UnidentifiedInterval;

    private static async Task ThrottleAsync(CancellationToken ct)
    {
        await ThrottleLock.WaitAsync(ct);
        try
        {
            var wait = MinRequestInterval - (DateTime.UtcNow - _lastRequestUtc);
            if (wait > TimeSpan.Zero) await Task.Delay(wait, ct);
            _lastRequestUtc = DateTime.UtcNow;
        }
        finally
        {
            ThrottleLock.Release();
        }
    }

    public string Key => "open-library";
    public string Name => "Open Library";
    public IReadOnlySet<LibrariannMediaType> SupportedMediaTypes { get; } =
        new HashSet<LibrariannMediaType> {LibrariannMediaType.Book};

    public async Task<ProviderTestResult> TestAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await ThrottleAsync(cancellationToken);
            using var response = await httpClient.GetAsync("search.json?q=librariann&fields=key&limit=1",
                HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            await MetadataProviderResponseReader.ReadAsync(response, cancellationToken);
            return new ProviderTestResult(true, "Connected to Open Library.", stopwatch.Elapsed);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new ProviderTestResult(false, "Open Library could not be reached.", stopwatch.Elapsed);
        }
    }

    public async Task<IReadOnlyCollection<NormalizedMetadataCandidate>> SearchAsync(MetadataLookupRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.MediaType != LibrariannMediaType.Book) return [];
        var parameters = new List<string>
        {
            "fields=" + Uri.EscapeDataString("key,title,author_name,first_publish_year,isbn,language,cover_i,publisher,subject,series"),
            "limit=20",
        };
        if (!string.IsNullOrWhiteSpace(request.Isbn))
        {
            parameters.Add("isbn=" + Uri.EscapeDataString(NormalizeIsbn(request.Isbn)));
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(request.Title)) parameters.Add("title=" + Uri.EscapeDataString(request.Title.Trim()));
            if (!string.IsNullOrWhiteSpace(request.Author)) parameters.Add("author=" + Uri.EscapeDataString(request.Author.Trim()));
        }
        if (parameters.Count == 2) throw new LibrariannException("metadata-search-requires-title-author-or-isbn");

        await ThrottleAsync(cancellationToken);
        using var response = await httpClient.GetAsync("search.json?" + string.Join('&', parameters),
            HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        var bytes = await MetadataProviderResponseReader.ReadAsync(response, cancellationToken);
        var payload = JsonSerializer.Deserialize<OpenLibrarySearchResponse>(bytes, SerializerOptions);
        if (payload?.Docs is null) return [];

        return payload.Docs.Where(document => !string.IsNullOrWhiteSpace(document.Key) && !string.IsNullOrWhiteSpace(document.Title))
            .Take(20)
            .Select(document => new NormalizedMetadataCandidate
            {
                ProviderKey = Key,
                ProviderName = Name,
                ExternalId = document.Key.Trim('/'),
                MediaType = LibrariannMediaType.Book,
                Title = document.Title.Trim(),
                Authors = Limit(document.AuthorNames),
                Series = document.Series?.FirstOrDefault()?.Trim() ?? string.Empty,
                PublicationYear = document.FirstPublishYear,
                Languages = Limit(document.Languages),
                Isbns = Limit(document.Isbns?.Select(NormalizeIsbn)),
                Publishers = Limit(document.Publishers),
                Genres = Limit(document.Subjects),
                CoverUri = document.CoverId is > 0
                    ? new Uri($"https://covers.openlibrary.org/b/id/{document.CoverId}-L.jpg")
                    : null,
                DetailsUri = new Uri(httpClient.BaseAddress!, document.Key.TrimStart('/')),
                Identifiers = new Dictionary<string, string> {{"open-library", document.Key.Trim('/')}},
            })
            .ToArray();
    }

    public void Dispose() => httpClient.Dispose();

    /// <summary>
    /// Looks up many books by ISBN in a single request via Open Library's Books API (bibkeys), rather than one
    /// search.json call per book - this is the batch mechanism their API guidelines ask clients to use instead
    /// of "hundreds of single-book requests". Used by bulk library-wide metadata matching; the interactive
    /// single-series search (<see cref="SearchAsync"/>) doesn't need it.
    /// </summary>
    /// <param name="isbns">Up to 50 ISBNs per call (a practical batch size, not an Open Library hard limit).</param>
    /// <returns>A map of the input ISBN (as given) to its matched candidate, for ISBNs that resolved.</returns>
    public async Task<IReadOnlyDictionary<string, NormalizedMetadataCandidate>> SearchByIsbnBatchAsync(
        IReadOnlyCollection<string> isbns, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, NormalizedMetadataCandidate>();
        var normalized = isbns
            .Select(isbn => (Original: isbn, Normalized: NormalizeIsbn(isbn)))
            .Where(pair => pair.Normalized.Length > 0)
            .DistinctBy(pair => pair.Normalized)
            .Take(50)
            .ToArray();
        if (normalized.Length == 0) return result;

        var bibkeys = string.Join(',', normalized.Select(pair => $"ISBN:{pair.Normalized}"));

        await ThrottleAsync(cancellationToken);
        using var response = await httpClient.GetAsync(
            $"api/books?bibkeys={Uri.EscapeDataString(bibkeys)}&format=json&jscmd=data",
            HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        var bytes = await MetadataProviderResponseReader.ReadAsync(response, cancellationToken);
        var payload = JsonSerializer.Deserialize<Dictionary<string, OpenLibraryBookDetail>>(bytes, SerializerOptions);
        if (payload == null) return result;

        foreach (var (original, key) in normalized.Select(pair => (pair.Original, Key: $"ISBN:{pair.Normalized}")))
        {
            if (!payload.TryGetValue(key, out var detail) || string.IsNullOrWhiteSpace(detail.Title)) continue;

            var externalId = detail.Key?.Trim('/') ?? key;
            result[original] = new NormalizedMetadataCandidate
            {
                ProviderKey = Key,
                ProviderName = Name,
                ExternalId = externalId,
                MediaType = LibrariannMediaType.Book,
                Title = detail.Title.Trim(),
                Authors = Limit(detail.Authors?.Select(a => a.Name)),
                PublicationYear = ParseYear(detail.PublishDate ?? string.Empty),
                Isbns = Limit((detail.Identifiers?.Isbn13 ?? []).Concat(detail.Identifiers?.Isbn10 ?? [])),
                Publishers = Limit(detail.Publishers?.Select(p => p.Name)),
                Genres = Limit(detail.Subjects?.Select(s => s.Name)),
                CoverUri = !string.IsNullOrWhiteSpace(detail.Cover?.Large) ? new Uri(detail.Cover.Large) : null,
                DetailsUri = !string.IsNullOrWhiteSpace(externalId) ? new Uri(httpClient.BaseAddress!, externalId.TrimStart('/')) : null,
                Identifiers = new Dictionary<string, string> {{"open-library", externalId}},
            };
        }

        return result;
    }

    public async Task<IReadOnlyCollection<MetadataCatalogItem>> GetCatalogAsync(MetadataCatalogRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.MediaType != LibrariannMediaType.Book || request.Kind != Librariann.Models.Entities.Acquisition.MonitoringTargetKind.Author)
            return [];
        var authorId = request.ExternalItemId.Trim().Trim('/');
        if (authorId.StartsWith("authors/", StringComparison.OrdinalIgnoreCase)) authorId = authorId[8..];
        if (!AuthorIdRegex().IsMatch(authorId)) throw new LibrariannException("invalid-open-library-author-id");

        // Open Library documents this endpoint and supports a limit up to 1000. Keep each sync bounded to 500
        // so provider responses stay below Librariann's response-size limit.
        await ThrottleAsync(cancellationToken);
        using var response = await httpClient.GetAsync($"authors/{Uri.EscapeDataString(authorId)}/works.json?limit=500",
            HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        var bytes = await MetadataProviderResponseReader.ReadAsync(response, cancellationToken);
        var payload = JsonSerializer.Deserialize<OpenLibraryWorksResponse>(bytes, SerializerOptions);
        return payload?.Entries?
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Key) && !string.IsNullOrWhiteSpace(entry.Title))
            .Select(entry => new MetadataCatalogItem(Key, entry.Key.Trim('/'), entry.Title.Trim(), request.Title.Trim(),
                string.Empty, string.Empty, ParseYear(entry.FirstPublishDate)))
            .DistinctBy(item => item.ExternalItemId, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    private static IReadOnlyCollection<string> Limit(IEnumerable<string>? values) => values?
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Select(value => value.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Take(30)
        .ToArray() ?? [];

    private static string NormalizeIsbn(string value) => new string(value.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

    private static int? ParseYear(string value) => value.Length >= 4 && int.TryParse(value[..4], out var year)
        ? year
        : null;

    private sealed record OpenLibrarySearchResponse
    {
        [JsonPropertyName("docs")] public IReadOnlyCollection<OpenLibraryDocument>? Docs { get; init; }
    }

    private sealed record OpenLibraryDocument
    {
        [JsonPropertyName("key")] public string Key { get; init; } = string.Empty;
        [JsonPropertyName("title")] public string Title { get; init; } = string.Empty;
        [JsonPropertyName("author_name")] public IReadOnlyCollection<string>? AuthorNames { get; init; }
        [JsonPropertyName("first_publish_year")] public int? FirstPublishYear { get; init; }
        [JsonPropertyName("isbn")] public IReadOnlyCollection<string>? Isbns { get; init; }
        [JsonPropertyName("language")] public IReadOnlyCollection<string>? Languages { get; init; }
        [JsonPropertyName("cover_i")] public int? CoverId { get; init; }
        [JsonPropertyName("publisher")] public IReadOnlyCollection<string>? Publishers { get; init; }
        [JsonPropertyName("subject")] public IReadOnlyCollection<string>? Subjects { get; init; }
        [JsonPropertyName("series")] public IReadOnlyCollection<string>? Series { get; init; }
    }

    private sealed record OpenLibraryWorksResponse
    {
        [JsonPropertyName("entries")] public IReadOnlyCollection<OpenLibraryWork>? Entries { get; init; }
    }

    private sealed record OpenLibraryWork
    {
        [JsonPropertyName("key")] public string Key { get; init; } = string.Empty;
        [JsonPropertyName("title")] public string Title { get; init; } = string.Empty;
        [JsonPropertyName("first_publish_date")] public string FirstPublishDate { get; init; } = string.Empty;
    }

    // Shape of the "Books API" (api/books?bibkeys=...&jscmd=data) response - notably different from search.json's
    // OpenLibraryDocument shape above.
    private sealed record OpenLibraryBookDetail
    {
        [JsonPropertyName("title")] public string Title { get; init; } = string.Empty;
        [JsonPropertyName("key")] public string? Key { get; init; }
        [JsonPropertyName("authors")] public IReadOnlyCollection<OpenLibraryNamedRef>? Authors { get; init; }
        [JsonPropertyName("publish_date")] public string? PublishDate { get; init; }
        [JsonPropertyName("publishers")] public IReadOnlyCollection<OpenLibraryNamedRef>? Publishers { get; init; }
        [JsonPropertyName("subjects")] public IReadOnlyCollection<OpenLibraryNamedRef>? Subjects { get; init; }
        [JsonPropertyName("cover")] public OpenLibraryCoverDetail? Cover { get; init; }
        [JsonPropertyName("identifiers")] public OpenLibraryIdentifiers? Identifiers { get; init; }
    }

    private sealed record OpenLibraryNamedRef
    {
        [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    }

    private sealed record OpenLibraryCoverDetail
    {
        [JsonPropertyName("large")] public string? Large { get; init; }
    }

    private sealed record OpenLibraryIdentifiers
    {
        [JsonPropertyName("isbn_13")] public IReadOnlyCollection<string>? Isbn13 { get; init; }
        [JsonPropertyName("isbn_10")] public IReadOnlyCollection<string>? Isbn10 { get; init; }
    }

    [System.Text.RegularExpressions.GeneratedRegex("^OL[0-9]+A$",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase |
        System.Text.RegularExpressions.RegexOptions.CultureInvariant)]
    private static partial System.Text.RegularExpressions.Regex AuthorIdRegex();
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Services.Metadata;
using Librariann.Models.DTOs.Acquisition;
using Librariann.Models.DTOs.Metadata;

namespace Librariann.Services.Metadata.Providers;

/// <summary>
/// Anonymous AniList GraphQL metadata provider for manga. It deliberately performs bounded searches and leaves
/// candidate selection to Librariann's scored, explicit apply flow.
/// </summary>
public sealed partial class AniListMetadataProvider(HttpClient httpClient) : IMetadataProvider
{
    private const string SearchQuery = """
        query ($search: String!, $perPage: Int!, $isAdult: Boolean) {
          Page(page: 1, perPage: $perPage) {
            media(search: $search, type: MANGA, isAdult: $isAdult, sort: SEARCH_MATCH) {
              id
              idMal
              isAdult
              title { romaji english native }
              synonyms
              description(asHtml: false)
              startDate { year }
              countryOfOrigin
              genres
              coverImage { extraLarge large }
              siteUrl
              staff(perPage: 25) {
                edges { role node { name { full } } }
              }
            }
          }
        }
        """;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public string Key => "anilist";
    public string Name => "AniList";
    public IReadOnlySet<LibrariannMediaType> SupportedMediaTypes { get; } =
        new HashSet<LibrariannMediaType> {LibrariannMediaType.Manga};

    public async Task<ProviderTestResult> TestAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await SearchCoreAsync("Librariann", 1, false, cancellationToken);
            return new ProviderTestResult(true, "Connected to AniList.", stopwatch.Elapsed);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new ProviderTestResult(false, "AniList could not be reached.", stopwatch.Elapsed);
        }
    }

    public async Task<IReadOnlyCollection<NormalizedMetadataCandidate>> SearchAsync(MetadataLookupRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.MediaType != LibrariannMediaType.Manga) return [];
        var search = !string.IsNullOrWhiteSpace(request.Title) ? request.Title.Trim() : request.Series.Trim();
        if (search.Length == 0) throw new IOException("AniList manga search requires a title or series.");
        return (await SearchCoreAsync(search, 20, request.IncludeAdult, cancellationToken)).Select(ToCandidate).ToArray();
    }

    public void Dispose() => httpClient.Dispose();

    private async Task<IReadOnlyCollection<AniListMedia>> SearchCoreAsync(string search, int perPage, bool includeAdult,
        CancellationToken cancellationToken)
    {
        bool? isAdult = includeAdult ? null : false;
        using var response = await httpClient.PostAsJsonAsync(string.Empty,
            new {query = SearchQuery, variables = new {search, perPage, isAdult}}, SerializerOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = JsonSerializer.Deserialize<AniListResponse>(
            await MetadataProviderResponseReader.ReadAsync(response, cancellationToken), SerializerOptions);
        if (payload?.Errors?.Count > 0) throw new IOException("AniList returned a GraphQL error.");
        return payload?.Data?.Page?.Media?
            .Where(item => item.Id > 0 && item.Title is not null && PreferredTitle(item.Title).Length > 0)
            .Take(20)
            .ToArray() ?? [];
    }

    private NormalizedMetadataCandidate ToCandidate(AniListMedia item)
    {
        var title = PreferredTitle(item.Title!);
        var alternateTitles = new[] {item.Title!.English, item.Title.Romaji, item.Title.Native}
            .Concat(item.Synonyms ?? [])
            .Where(value => !string.IsNullOrWhiteSpace(value) &&
                            !value.Equals(title, StringComparison.OrdinalIgnoreCase))
            .Select(value => value!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(30)
            .ToArray();
        var identifiers = new Dictionary<string, string> {["anilist"] = item.Id.ToString()};
        if (item.IdMal is > 0) identifiers["myanimelist"] = item.IdMal.Value.ToString();

        return new NormalizedMetadataCandidate
        {
            ProviderKey = Key,
            ProviderName = Name,
            ExternalId = item.Id.ToString(),
            MediaType = LibrariannMediaType.Manga,
            IsAdult = item.IsAdult,
            Title = title,
            AlternateTitles = alternateTitles,
            Authors = Authors(item.Staff?.Edges),
            PublicationYear = item.StartDate?.Year,
            Languages = OriginLanguages(item.CountryOfOrigin),
            Genres = Limit(item.Genres),
            Description = PlainText(item.Description),
            CoverUri = SecureUri(item.CoverImage?.ExtraLarge ?? item.CoverImage?.Large),
            DetailsUri = SecureUri(item.SiteUrl),
            Identifiers = identifiers,
        };
    }

    private static string PreferredTitle(AniListTitle title) =>
        (title.English ?? title.Romaji ?? title.Native ?? string.Empty).Trim();

    private static IReadOnlyCollection<string> Authors(IEnumerable<AniListStaffEdge>? edges) => edges?
        .Where(edge => edge.Role.Contains("story", StringComparison.OrdinalIgnoreCase) ||
                       edge.Role.Contains("art", StringComparison.OrdinalIgnoreCase) ||
                       edge.Role.Contains("creator", StringComparison.OrdinalIgnoreCase))
        .Select(edge => edge.Node?.Name?.Full)
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Select(value => value!.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Take(20)
        .ToArray() ?? [];

    private static IReadOnlyCollection<string> OriginLanguages(string? country) => country?.ToUpperInvariant() switch
    {
        "JP" => ["ja"],
        "KR" => ["ko"],
        "CN" or "TW" => ["zh"],
        _ => [],
    };

    private static IReadOnlyCollection<string> Limit(IEnumerable<string>? values) => values?
        .Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase).Take(30).ToArray() ?? [];

    private static string PlainText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var withoutMarkup = HtmlTagRegex().Replace(value, " ");
        return WhitespaceRegex().Replace(WebUtility.HtmlDecode(withoutMarkup), " ").Trim();
    }

    private static Uri? SecureUri(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps ? uri : null;

    private sealed record AniListResponse
    {
        [JsonPropertyName("data")] public AniListData? Data { get; init; }
        [JsonPropertyName("errors")] public IReadOnlyCollection<JsonElement>? Errors { get; init; }
    }

    private sealed record AniListData([property: JsonPropertyName("Page")] AniListPage? Page);
    private sealed record AniListPage([property: JsonPropertyName("media")] IReadOnlyCollection<AniListMedia>? Media);
    private sealed record AniListMedia
    {
        [JsonPropertyName("id")] public int Id { get; init; }
        [JsonPropertyName("idMal")] public int? IdMal { get; init; }
        [JsonPropertyName("isAdult")] public bool IsAdult { get; init; }
        [JsonPropertyName("title")] public AniListTitle? Title { get; init; }
        [JsonPropertyName("synonyms")] public IReadOnlyCollection<string>? Synonyms { get; init; }
        [JsonPropertyName("description")] public string? Description { get; init; }
        [JsonPropertyName("startDate")] public AniListDate? StartDate { get; init; }
        [JsonPropertyName("countryOfOrigin")] public string? CountryOfOrigin { get; init; }
        [JsonPropertyName("genres")] public IReadOnlyCollection<string>? Genres { get; init; }
        [JsonPropertyName("coverImage")] public AniListCover? CoverImage { get; init; }
        [JsonPropertyName("siteUrl")] public string? SiteUrl { get; init; }
        [JsonPropertyName("staff")] public AniListStaff? Staff { get; init; }
    }

    private sealed record AniListTitle
    {
        [JsonPropertyName("romaji")] public string? Romaji { get; init; }
        [JsonPropertyName("english")] public string? English { get; init; }
        [JsonPropertyName("native")] public string? Native { get; init; }
    }

    private sealed record AniListDate([property: JsonPropertyName("year")] int? Year);
    private sealed record AniListCover
    {
        [JsonPropertyName("extraLarge")] public string? ExtraLarge { get; init; }
        [JsonPropertyName("large")] public string? Large { get; init; }
    }

    private sealed record AniListStaff([property: JsonPropertyName("edges")] IReadOnlyCollection<AniListStaffEdge>? Edges);
    private sealed record AniListStaffEdge
    {
        [JsonPropertyName("role")] public string Role { get; init; } = string.Empty;
        [JsonPropertyName("node")] public AniListStaffNode? Node { get; init; }
    }
    private sealed record AniListStaffNode([property: JsonPropertyName("name")] AniListStaffName? Name);
    private sealed record AniListStaffName([property: JsonPropertyName("full")] string? Full);

    [GeneratedRegex("<[^>]+>", RegexOptions.CultureInvariant)]
    private static partial Regex HtmlTagRegex();
    [GeneratedRegex("\\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();
}

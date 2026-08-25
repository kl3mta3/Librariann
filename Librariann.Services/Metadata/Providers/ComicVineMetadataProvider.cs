using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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
/// Comic Vine metadata-only provider. Comic Vine requires a user-owned API key and visible source credit;
/// every normalized candidate therefore retains its Comic Vine name and details link.
/// </summary>
public sealed partial class ComicVineMetadataProvider(HttpClient httpClient, string apiKey) : IMetadataProvider
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    public string Key => "comic-vine";
    public string Name => "Comic Vine";
    public IReadOnlySet<LibrariannMediaType> SupportedMediaTypes { get; } =
        new HashSet<LibrariannMediaType> {LibrariannMediaType.Comic};

    public async Task<ProviderTestResult> TestAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        if (string.IsNullOrWhiteSpace(apiKey))
            return new ProviderTestResult(false, "Comic Vine requires an API key.", stopwatch.Elapsed);
        try
        {
            await SearchCoreAsync("Librariann", false, 1, cancellationToken);
            return new ProviderTestResult(true, "Connected to Comic Vine.", stopwatch.Elapsed);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new ProviderTestResult(false, "Comic Vine could not be reached or rejected the API key.",
                stopwatch.Elapsed);
        }
    }

    public async Task<IReadOnlyCollection<NormalizedMetadataCandidate>> SearchAsync(MetadataLookupRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.MediaType != LibrariannMediaType.Comic) return [];
        if (string.IsNullOrWhiteSpace(apiKey)) throw new IOException("Comic Vine requires an API key.");
        var search = !string.IsNullOrWhiteSpace(request.Series) ? request.Series.Trim() : request.Title.Trim();
        if (search.Length == 0) throw new IOException("Comic Vine search requires a title or series.");
        var searchIssues = request.Issue.HasValue;
        return (await SearchCoreAsync(search, searchIssues, 20, cancellationToken))
            .Select(item => ToCandidate(item, searchIssues))
            .Where(candidate => candidate.Title.Length > 0)
            .ToArray();
    }

    public void Dispose() => httpClient.Dispose();

    private async Task<IReadOnlyCollection<ComicVineItem>> SearchCoreAsync(string query, bool issues, int limit,
        CancellationToken cancellationToken)
    {
        var resource = issues ? "issue" : "volume";
        var fields = issues
            ? "id,name,deck,description,issue_number,cover_date,volume,image,site_detail_url,person_credits"
            : "id,name,deck,description,start_year,publisher,image,site_detail_url,count_of_issues";
        var path = "search/?api_key=" + Uri.EscapeDataString(apiKey.Trim()) +
                   "&format=json&limit=" + Math.Clamp(limit, 1, 20) +
                   "&resources=" + resource + "&query=" + Uri.EscapeDataString(query) +
                   "&field_list=" + Uri.EscapeDataString(fields);
        using var response = await httpClient.GetAsync(path, HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = JsonSerializer.Deserialize<ComicVineResponse>(
            await MetadataProviderResponseReader.ReadAsync(response, cancellationToken), SerializerOptions);
        if (payload is null || payload.StatusCode != 1 ||
            !string.Equals(payload.Error, "OK", StringComparison.OrdinalIgnoreCase))
            throw new IOException("Comic Vine returned an API error.");
        return payload.Results?.Where(item => item.Id > 0).Take(20).ToArray() ?? [];
    }

    private NormalizedMetadataCandidate ToCandidate(ComicVineItem item, bool issue)
    {
        var series = issue ? item.Volume?.Name?.Trim() ?? string.Empty : item.Name?.Trim() ?? string.Empty;
        var issueNumber = ParseNumber(item.IssueNumber);
        var issueTitle = item.Name?.Trim() ?? string.Empty;
        var title = issue && issueTitle.Length > 0 ? issueTitle : series;
        var identifierType = issue ? "issue" : "volume";
        return new NormalizedMetadataCandidate
        {
            ProviderKey = Key,
            ProviderName = Name,
            ExternalId = $"{identifierType}:{item.Id}",
            MediaType = LibrariannMediaType.Comic,
            Title = title,
            AlternateTitles = issue && issueTitle.Length > 0 && series.Length > 0 &&
                              !issueTitle.Equals(series, StringComparison.OrdinalIgnoreCase)
                ? [series]
                : [],
            Authors = item.PersonCredits?.Where(person => IsCreatorRole(person.Role))
                .Select(person => person.Name).Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Take(20).ToArray() ?? [],
            Series = series,
            Issue = issueNumber,
            PublicationYear = issue ? ParseYear(item.CoverDate) : item.StartYear,
            Publishers = string.IsNullOrWhiteSpace(item.Publisher?.Name) ? [] : [item.Publisher.Name.Trim()],
            Description = PlainText(item.Description ?? item.Deck),
            CoverUri = SecureUri(item.Image?.OriginalUrl ?? item.Image?.SuperUrl ?? item.Image?.MediumUrl),
            DetailsUri = SecureUri(item.SiteDetailUrl),
            Identifiers = new Dictionary<string, string> {{"comicvine", item.Id.ToString(CultureInfo.InvariantCulture)}},
        };
    }

    private static bool IsCreatorRole(string? role) => role?.Contains("writer", StringComparison.OrdinalIgnoreCase) == true ||
                                                        role?.Contains("artist", StringComparison.OrdinalIgnoreCase) == true ||
                                                        role?.Contains("creator", StringComparison.OrdinalIgnoreCase) == true;

    private static int? ParseNumber(string? value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) ? number : null;

    private static int? ParseYear(string? value) =>
        DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date.Year : null;

    private static string PlainText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var text = WhitespaceRegex().Replace(WebUtility.HtmlDecode(HtmlTagRegex().Replace(value, " ")), " ").Trim();
        return SpaceBeforePunctuationRegex().Replace(text, "$1");
    }

    private static Uri? SecureUri(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps ? uri : null;

    private sealed record ComicVineResponse
    {
        [JsonPropertyName("status_code")] public int StatusCode { get; init; }
        [JsonPropertyName("error")] public string Error { get; init; } = string.Empty;
        [JsonPropertyName("results")] public IReadOnlyCollection<ComicVineItem>? Results { get; init; }
    }

    private sealed record ComicVineItem
    {
        [JsonPropertyName("id")] public int Id { get; init; }
        [JsonPropertyName("name")] public string? Name { get; init; }
        [JsonPropertyName("deck")] public string? Deck { get; init; }
        [JsonPropertyName("description")] public string? Description { get; init; }
        [JsonPropertyName("issue_number")] public string? IssueNumber { get; init; }
        [JsonPropertyName("cover_date")] public string? CoverDate { get; init; }
        [JsonPropertyName("start_year")] public int? StartYear { get; init; }
        [JsonPropertyName("volume")] public ComicVineReference? Volume { get; init; }
        [JsonPropertyName("publisher")] public ComicVineReference? Publisher { get; init; }
        [JsonPropertyName("image")] public ComicVineImage? Image { get; init; }
        [JsonPropertyName("site_detail_url")] public string? SiteDetailUrl { get; init; }
        [JsonPropertyName("person_credits")] public IReadOnlyCollection<ComicVinePerson>? PersonCredits { get; init; }
    }

    private sealed record ComicVineReference([property: JsonPropertyName("name")] string? Name);
    private sealed record ComicVinePerson
    {
        [JsonPropertyName("name")] public string? Name { get; init; }
        [JsonPropertyName("role")] public string? Role { get; init; }
    }
    private sealed record ComicVineImage
    {
        [JsonPropertyName("original_url")] public string? OriginalUrl { get; init; }
        [JsonPropertyName("super_url")] public string? SuperUrl { get; init; }
        [JsonPropertyName("medium_url")] public string? MediumUrl { get; init; }
    }

    [GeneratedRegex("<[^>]+>", RegexOptions.CultureInvariant)]
    private static partial Regex HtmlTagRegex();
    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();
    [GeneratedRegex(@"\s+([.,;:!?])", RegexOptions.CultureInvariant)]
    private static partial Regex SpaceBeforePunctuationRegex();
}

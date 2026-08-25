using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Services.Metadata;
using Librariann.Common;
using Librariann.Common.Extensions;
using Librariann.Models.DTOs.Person;

namespace Librariann.Services.Metadata;

/// <summary>
/// Low-volume, user-initiated author matching against Open Library.
/// </summary>
public sealed partial class OpenLibraryAuthorMetadataService(ITrustedMetadataHttpClientFactory httpClientFactory)
    : IAuthorMetadataService
{
    private const string ProviderKey = "open-library";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyCollection<AuthorMetadataCandidateDto>> SearchAsync(string query,
        CancellationToken cancellationToken = default)
    {
        query = query.Trim();
        if (query.Length is < 2 or > 200) throw new LibrariannException("author-search-query-invalid");

        using var client = await CreateClient();
        using var response = await client.GetAsync(
            $"search/authors.json?q={Uri.EscapeDataString(query)}&limit=10",
            HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        var bytes = await Providers.MetadataProviderResponseReader.ReadAsync(response, cancellationToken);
        var payload = JsonSerializer.Deserialize<AuthorSearchResponse>(bytes, SerializerOptions);
        if (payload?.Docs is null) return [];

        var normalizedQuery = query.ToNormalized();
        return payload.Docs
            .Where(author => AuthorIdRegex().IsMatch(author.Key) && !string.IsNullOrWhiteSpace(author.Name))
            .Select(author => ToCandidate(author, normalizedQuery))
            .OrderByDescending(candidate => candidate.MatchScore)
            .ThenByDescending(candidate => candidate.PortraitUri is not null)
            .ThenByDescending(candidate => candidate.WorkCount)
            .Take(10)
            .ToArray();
    }

    public async Task<AuthorMetadataDetails?> GetDetailsAsync(string providerKey, string externalId,
        CancellationToken cancellationToken = default)
    {
        if (!providerKey.Equals(ProviderKey, StringComparison.OrdinalIgnoreCase))
            throw new LibrariannException("unsupported-author-metadata-provider");

        externalId = NormalizeAuthorId(externalId);
        using var client = await CreateClient();
        using var response = await client.GetAsync($"authors/{Uri.EscapeDataString(externalId)}.json",
            HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var bytes = await Providers.MetadataProviderResponseReader.ReadAsync(response, cancellationToken);
        var author = JsonSerializer.Deserialize<AuthorDetailsResponse>(bytes, SerializerOptions);
        if (author is null || string.IsNullOrWhiteSpace(author.Name)) return null;

        return new AuthorMetadataDetails
        {
            ProviderKey = ProviderKey,
            ExternalId = externalId,
            Name = author.Name.Trim(),
            Aliases = Clean(author.AlternateNames),
            Description = ReadText(author.Bio),
            PortraitUrl = $"https://covers.openlibrary.org/a/olid/{externalId}-L.jpg?default=false",
        };
    }

    private Task<HttpClient> CreateClient()
    {
        return httpClientFactory.CreateOpenLibraryClient();
    }

    private static AuthorMetadataCandidateDto ToCandidate(AuthorSearchDocument author, string normalizedQuery)
    {
        var normalizedName = author.Name.ToNormalized();
        var aliases = Clean(author.AlternateNames);
        var exactName = normalizedName == normalizedQuery;
        var exactAlias = aliases.Any(alias => alias.ToNormalized() == normalizedQuery);
        var startsWith = normalizedName.StartsWith(normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
                         normalizedQuery.StartsWith(normalizedName, StringComparison.OrdinalIgnoreCase);
        var reasons = new List<string>();
        var score = 20;
        if (exactName)
        {
            score = 100;
            reasons.Add("Exact name match");
        }
        else if (exactAlias)
        {
            score = 92;
            reasons.Add("Exact alias match");
        }
        else if (startsWith)
        {
            score = 70;
            reasons.Add("Close name match");
        }
        else
        {
            reasons.Add("Name search result");
        }

        if (!string.IsNullOrWhiteSpace(author.TopWork)) reasons.Add($"Known for {author.TopWork.Trim()}");
        return new AuthorMetadataCandidateDto
        {
            ProviderKey = ProviderKey,
            ProviderName = "Open Library",
            ExternalId = author.Key,
            Name = author.Name.Trim(),
            Aliases = aliases,
            BirthDate = author.BirthDate?.Trim() ?? string.Empty,
            DeathDate = author.DeathDate?.Trim() ?? string.Empty,
            TopWork = author.TopWork?.Trim() ?? string.Empty,
            WorkCount = Math.Max(0, author.WorkCount),
            PortraitUri = author.Photos is {Count: > 0}
                ? new Uri($"https://covers.openlibrary.org/a/olid/{author.Key}-M.jpg?default=false")
                : null,
            DetailsUri = new Uri($"https://openlibrary.org/authors/{author.Key}"),
            MatchScore = score,
            MatchReasons = reasons,
        };
    }

    private static string NormalizeAuthorId(string value)
    {
        var id = value.Trim().Trim('/');
        if (id.StartsWith("authors/", StringComparison.OrdinalIgnoreCase)) id = id[8..];
        if (!AuthorIdRegex().IsMatch(id)) throw new LibrariannException("invalid-open-library-author-id");
        return id.ToUpperInvariant();
    }

    private static IReadOnlyCollection<string> Clean(IEnumerable<string>? values) => values?
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Select(value => value.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Take(30)
        .ToArray() ?? [];

    private static string ReadText(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String) return value.GetString()?.Trim() ?? string.Empty;
        if (value.ValueKind == JsonValueKind.Object && value.TryGetProperty("value", out var text) &&
            text.ValueKind == JsonValueKind.String) return text.GetString()?.Trim() ?? string.Empty;
        return string.Empty;
    }

    private sealed record AuthorSearchResponse
    {
        [JsonPropertyName("docs")] public IReadOnlyCollection<AuthorSearchDocument>? Docs { get; init; }
    }

    private sealed record AuthorSearchDocument
    {
        [JsonPropertyName("key")] public string Key { get; init; } = string.Empty;
        [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
        [JsonPropertyName("alternate_names")] public IReadOnlyCollection<string>? AlternateNames { get; init; }
        [JsonPropertyName("birth_date")] public string? BirthDate { get; init; }
        [JsonPropertyName("death_date")] public string? DeathDate { get; init; }
        [JsonPropertyName("top_work")] public string? TopWork { get; init; }
        [JsonPropertyName("work_count")] public int WorkCount { get; init; }
        [JsonPropertyName("photos")] public IReadOnlyCollection<int>? Photos { get; init; }
    }

    private sealed record AuthorDetailsResponse
    {
        [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
        [JsonPropertyName("alternate_names")] public IReadOnlyCollection<string>? AlternateNames { get; init; }
        [JsonPropertyName("bio")] public JsonElement Bio { get; init; }
    }

    [GeneratedRegex("^OL[0-9]+A$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AuthorIdRegex();
}

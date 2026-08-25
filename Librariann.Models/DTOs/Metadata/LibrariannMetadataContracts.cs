using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Librariann.Models.Entities.Metadata;

namespace Librariann.Models.DTOs.Metadata;

public enum LibrariannMediaType
{
    Book = 1,
    Comic = 2,
    Manga = 3,
}

public sealed record MetadataLookupRequest
{
    [EnumDataType(typeof(LibrariannMediaType))]
    public LibrariannMediaType MediaType { get; init; } = LibrariannMediaType.Book;

    [StringLength(512)] public string Title { get; init; } = string.Empty;
    [StringLength(256)] public string Author { get; init; } = string.Empty;
    [StringLength(256)] public string Series { get; init; } = string.Empty;
    [StringLength(32)] public string Isbn { get; init; } = string.Empty;
    [StringLength(32)] public string Language { get; init; } = string.Empty;
    [Range(0, 10000)] public int? Volume { get; init; }
    [Range(0, 100000)] public int? Issue { get; init; }
    [Range(0, 9999)] public int? PublicationYear { get; init; }
    public bool IncludeAdult { get; init; }
    public IReadOnlyDictionary<string, string> Identifiers { get; init; } = new Dictionary<string, string>();
}

public sealed record NormalizedMetadataCandidate
{
    public string ProviderKey { get; init; } = string.Empty;
    public string ProviderName { get; init; } = string.Empty;
    public string ExternalId { get; init; } = string.Empty;
    public LibrariannMediaType MediaType { get; init; }
    public bool IsAdult { get; init; }
    public string Title { get; init; } = string.Empty;
    public IReadOnlyCollection<string> AlternateTitles { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> Authors { get; init; } = Array.Empty<string>();
    public string Series { get; init; } = string.Empty;
    public int? Volume { get; init; }
    public int? Issue { get; init; }
    public int? PublicationYear { get; init; }
    public IReadOnlyCollection<string> Languages { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> Isbns { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> Publishers { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> Genres { get; init; } = Array.Empty<string>();
    public string Description { get; init; } = string.Empty;
    public Uri? CoverUri { get; init; }
    public Uri? DetailsUri { get; init; }
    public IReadOnlyDictionary<string, string> Identifiers { get; init; } = new Dictionary<string, string>();
}

public sealed record MetadataMatchDecision(
    NormalizedMetadataCandidate Candidate,
    int Score,
    IReadOnlyCollection<string> MatchReasons)
{
    /// <summary>
    /// Short-lived, single-use, user-bound reference to the server-side candidate. Provider data is never
    /// accepted back from the browser when applying metadata.
    /// </summary>
    public string ApplyToken { get; init; } = string.Empty;
}

public sealed record MetadataProviderFailure(string ProviderKey, string ProviderName, string Message);

public sealed record MetadataLookupResponse(
    IReadOnlyCollection<MetadataMatchDecision> Results,
    IReadOnlyCollection<MetadataProviderFailure> ProviderFailures);

public sealed record ApplyMetadataRequest
{
    [Range(1, int.MaxValue)] public int SeriesId { get; init; }
    [Required, StringLength(128)] public string ApplyToken { get; init; } = string.Empty;
    [MinLength(1)] public IReadOnlyCollection<MetadataFieldKey> Fields { get; init; } = Array.Empty<MetadataFieldKey>();
}

public sealed record MetadataFieldApplyResult(MetadataFieldKey Field, bool Applied, string Reason);

public sealed record ApplyMetadataResponse(
    int SeriesId,
    string ProviderKey,
    string ProviderItemId,
    IReadOnlyCollection<MetadataFieldApplyResult> Fields);

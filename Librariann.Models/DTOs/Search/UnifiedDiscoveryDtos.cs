using System.ComponentModel.DataAnnotations;
using Librariann.Models.DTOs.Acquisition;
using Librariann.Models.DTOs.Metadata;

namespace Librariann.Models.DTOs.Search;

/// <summary>
/// One library-item-centered query spanning owned media, external metadata, and available releases.
/// Sections are returned only when the caller has the corresponding capability.
/// </summary>
public sealed record UnifiedDiscoveryRequest
{
    [Required, StringLength(512, MinimumLength = 2)]
    public string Query { get; init; } = string.Empty;

    [StringLength(256)] public string Author { get; init; } = string.Empty;
    [StringLength(32)] public string Isbn { get; init; } = string.Empty;
    [StringLength(32)] public string Language { get; init; } = string.Empty;
    [EnumDataType(typeof(LibrariannMediaType))]
    public LibrariannMediaType MediaType { get; init; } = LibrariannMediaType.Book;
    [Range(1, int.MaxValue)] public int? QualityProfileId { get; init; }
    public AcquisitionMediaFormat? OwnedFormat { get; init; }
    public bool IncludeAdult { get; init; }
}

public sealed record UnifiedDiscoveryAccess(
    bool CanSearchLibrary,
    bool CanSearchExternalMetadata,
    bool CanSearchReleases,
    bool RestrictedByContentPolicy);

public sealed record UnifiedDiscoveryResponse(
    SearchResultGroupDto Library,
    MetadataLookupResponse? ExternalMetadata,
    InteractiveSearchResponse? Releases,
    UnifiedDiscoveryAccess Access,
    int? QualityProfileId);

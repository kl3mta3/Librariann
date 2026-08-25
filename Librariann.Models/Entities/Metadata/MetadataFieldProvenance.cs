using System;

namespace Librariann.Models.Entities.Metadata;

public enum MetadataEntityType
{
    Series = 1,
    Volume = 2,
    Chapter = 3,
}

public enum MetadataFieldKey
{
    Title = 1,
    SortTitle = 2,
    Description = 3,
    Cover = 4,
    Authors = 5,
    Series = 6,
    Volume = 7,
    Issue = 8,
    Isbn = 9,
    Publisher = 10,
    PublicationDate = 11,
    Language = 12,
    Genres = 13,
    Tags = 14,
    AgeRating = 15,
    WebLinks = 16,
}

/// <summary>
/// Records the owner of one resolved metadata field. The value itself remains in the mature
/// library model; its hash lets refresh logic detect changes without duplicating private content.
/// </summary>
public sealed class MetadataFieldProvenance
{
    public int Id { get; set; }
    public MetadataEntityType EntityType { get; set; }
    public int EntityId { get; set; }
    public MetadataFieldKey Field { get; set; }
    public string ProviderKey { get; set; } = string.Empty;
    public string ProviderItemId { get; set; } = string.Empty;
    public string ValueHash { get; set; } = string.Empty;
    public bool IsUserOverride { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

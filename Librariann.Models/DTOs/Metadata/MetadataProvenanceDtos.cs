using System;
using System.ComponentModel.DataAnnotations;
using Librariann.Models.Entities.Metadata;

namespace Librariann.Models.DTOs.Metadata;

public sealed record MetadataProvenanceDto(
    int Id,
    MetadataEntityType EntityType,
    int EntityId,
    MetadataFieldKey Field,
    string ProviderKey,
    string ProviderItemId,
    bool IsUserOverride,
    DateTime UpdatedAtUtc);

public sealed record RecordMetadataProvenanceRequest
{
    [EnumDataType(typeof(MetadataEntityType))]
    public MetadataEntityType EntityType { get; init; }

    [Range(1, int.MaxValue)] public int EntityId { get; init; }

    [EnumDataType(typeof(MetadataFieldKey))]
    public MetadataFieldKey Field { get; init; }

    [Required, StringLength(100)] public string ProviderKey { get; init; } = string.Empty;
    [StringLength(512)] public string ProviderItemId { get; init; } = string.Empty;
    [Required, StringLength(16384)] public string CanonicalValue { get; init; } = string.Empty;
    public bool IsUserOverride { get; init; }
}

public sealed record MetadataRefreshPermission(bool CanRefresh, string Reason);

using Librariann.Models.DTOs.LibrariannPlus.Metadata;
using Librariann.Models.Entities;

namespace Librariann.Models.Mapping;

/// <summary>Explicit replacement for <c>CreateMap&lt;MetadataFieldMapping, MetadataFieldMappingDto&gt;()</c>.</summary>
public static class MetadataFieldMappingMapping
{
    public static MetadataFieldMappingDto ToMetadataFieldMappingDto(this MetadataFieldMapping m) => new()
    {
        Id = m.Id,
        SourceType = m.SourceType,
        DestinationType = m.DestinationType,
        SourceValue = m.SourceValue,
        DestinationValue = m.DestinationValue,
        ExcludeFromSource = m.ExcludeFromSource,
    };
}

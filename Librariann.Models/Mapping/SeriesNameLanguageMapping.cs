using Librariann.Models.DTOs.LibrariannPlus.Metadata;
using Librariann.Models.Entities.MetadataMatching;

namespace Librariann.Models.Mapping;

/// <summary>Explicit replacement for <c>CreateMap&lt;SeriesNameLanguage, SeriesNameLanguageDto&gt;()</c>.</summary>
public static class SeriesNameLanguageMapping
{
    public static SeriesNameLanguageDto ToSeriesNameLanguageDto(this SeriesNameLanguage l) => new()
    {
        Name = l.Name,
        LocalizedName = l.LocalizedName,
    };
}

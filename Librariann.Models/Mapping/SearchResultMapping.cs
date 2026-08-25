using System;
using System.Linq;
using System.Linq.Expressions;
using Librariann.Models.DTOs.Search;
using Librariann.Models.Entities;

namespace Librariann.Models.Mapping;

/// <summary>Explicit replacement for <c>CreateMap&lt;Series, SearchResultDto&gt;()</c>.</summary>
public static class SearchResultMapping
{
    public static readonly Expression<Func<Series, SearchResultDto>> ToSearchResultDtoExpression = s => new SearchResultDto
    {
        SeriesId = s.Id,
        Name = s.Name,
        OriginalName = s.OriginalName,
        SortName = s.SortName,
        LocalizedName = s.LocalizedName,
        Format = s.Format,
        LibraryName = s.Library.Name,
        LibraryId = s.LibraryId,
        ReleaseYear = s.Metadata.ReleaseYear,
        VolumeCount = s.Volumes.Count,
        ChapterCount = s.Volumes.SelectMany(v => v.Chapters).Count(),
    };

    private static readonly Func<Series, SearchResultDto> CompiledToSearchResultDto = ToSearchResultDtoExpression.Compile();

    public static SearchResultDto ToSearchResultDto(this Series s) => CompiledToSearchResultDto(s);
}

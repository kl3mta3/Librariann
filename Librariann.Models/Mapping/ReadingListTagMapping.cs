using System;
using System.Linq.Expressions;
using Librariann.Models.DTOs.ReadingLists;
using Librariann.Models.Entities.ReadingLists;

namespace Librariann.Models.Mapping;

/// <summary>Explicit replacement for <c>CreateMap&lt;ReadingListTag, ReadingListTagDto&gt;()</c>.</summary>
public static class ReadingListTagMapping
{
    public static readonly Expression<Func<ReadingListTag, ReadingListTagDto>> ToReadingListTagDtoExpression = t => new ReadingListTagDto
    {
        Id = t.Id,
        Title = t.Title,
        NormalizedTitle = t.NormalizedTitle,
    };

    private static readonly Func<ReadingListTag, ReadingListTagDto> CompiledToReadingListTagDto = ToReadingListTagDtoExpression.Compile();

    public static ReadingListTagDto ToReadingListTagDto(this ReadingListTag t) => CompiledToReadingListTagDto(t);
}

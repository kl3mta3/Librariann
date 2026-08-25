using System;
using System.Linq.Expressions;
using Librariann.Models.DTOs.Scrobbling;
using Librariann.Models.Entities.Scrobble;

namespace Librariann.Models.Mapping;

/// <summary>Explicit replacement for <c>CreateMap&lt;ScrobbleError, ScrobbleErrorDto&gt;()</c>.</summary>
public static class ScrobbleErrorMapping
{
    public static readonly Expression<Func<ScrobbleError, ScrobbleErrorDto>> ToScrobbleErrorDtoExpression = e => new ScrobbleErrorDto
    {
        Comment = e.Comment,
        Details = e.Details,
        SeriesId = e.SeriesId,
        ChapterId = e.ChapterId,
        LibraryId = e.LibraryId,
        Created = e.Created,
    };

    private static readonly Func<ScrobbleError, ScrobbleErrorDto> CompiledToScrobbleErrorDto = ToScrobbleErrorDtoExpression.Compile();

    public static ScrobbleErrorDto ToScrobbleErrorDto(this ScrobbleError e) => CompiledToScrobbleErrorDto(e);
}

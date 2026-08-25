using System;
using System.Linq.Expressions;
using Librariann.Models.DTOs.Scrobbling;
using Librariann.Models.Entities.Scrobble;

namespace Librariann.Models.Mapping;

/// <summary>Explicit replacement for <c>CreateMap&lt;ScrobbleHold, ScrobbleHoldDto&gt;()</c>.</summary>
public static class ScrobbleHoldMapping
{
    public static readonly Expression<Func<ScrobbleHold, ScrobbleHoldDto>> ToScrobbleHoldDtoExpression = h => new ScrobbleHoldDto
    {
        SeriesName = h.Series.Name,
        SeriesId = h.SeriesId,
        LibraryId = h.Series.LibraryId,
        Created = h.Created,
        CreatedUtc = h.CreatedUtc,
    };

    private static readonly Func<ScrobbleHold, ScrobbleHoldDto> CompiledToScrobbleHoldDto = ToScrobbleHoldDtoExpression.Compile();

    public static ScrobbleHoldDto ToScrobbleHoldDto(this ScrobbleHold h) => CompiledToScrobbleHoldDto(h);
}

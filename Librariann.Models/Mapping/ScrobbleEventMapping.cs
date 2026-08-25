using System;
using System.Linq.Expressions;
using Librariann.Models.DTOs.Scrobbling;
using Librariann.Models.Entities.Scrobble;

namespace Librariann.Models.Mapping;

/// <summary>Explicit replacement for <c>CreateMap&lt;ScrobbleEvent, ScrobbleEventDto&gt;()</c>.</summary>
public static class ScrobbleEventMapping
{
    public static readonly Expression<Func<ScrobbleEvent, ScrobbleEventDto>> ToScrobbleEventDtoExpression = e => new ScrobbleEventDto
    {
        Id = e.Id,
        SeriesName = e.Series.Name,
        SeriesId = e.SeriesId,
        LibraryId = e.LibraryId,
        IsProcessed = e.IsProcessed,
        VolumeNumber = e.VolumeNumber,
        ChapterNumber = e.ChapterNumber,
        LastModifiedUtc = e.LastModifiedUtc,
        CreatedUtc = e.CreatedUtc,
        Rating = e.Rating,
        ReadStatus = e.ReadStatus,
        ScrobbleEventType = e.ScrobbleEventType,
        ScrobbleProvider = e.ScrobbleProvider,
        IsErrored = e.IsErrored,
        ErrorDetails = e.ErrorDetails,
    };

    private static readonly Func<ScrobbleEvent, ScrobbleEventDto> CompiledToScrobbleEventDto = ToScrobbleEventDtoExpression.Compile();

    public static ScrobbleEventDto ToScrobbleEventDto(this ScrobbleEvent e) => CompiledToScrobbleEventDto(e);
}

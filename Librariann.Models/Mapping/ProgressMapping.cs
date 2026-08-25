using System;
using System.Linq.Expressions;
using Librariann.Models.DTOs.Progress;
using Librariann.Models.Entities.Progress;

namespace Librariann.Models.Mapping;

/// <summary>Explicit replacement for <c>CreateMap&lt;AppUserProgress, ProgressDto&gt;()</c>.</summary>
public static class ProgressMapping
{
    public static readonly Expression<Func<AppUserProgress, ProgressDto>> ToProgressDtoExpression = p => new ProgressDto
    {
        VolumeId = p.VolumeId,
        ChapterId = p.ChapterId,
        PageNum = p.PagesRead,
        SeriesId = p.SeriesId,
        LibraryId = p.LibraryId,
        BookScrollId = p.BookScrollId,
        PlaybackPositionSeconds = p.PlaybackPositionSeconds,
        LastModifiedUtc = p.LastModifiedUtc,
    };

    private static readonly Func<AppUserProgress, ProgressDto> CompiledToProgressDto = ToProgressDtoExpression.Compile();

    public static ProgressDto ToProgressDto(this AppUserProgress p) => CompiledToProgressDto(p);
}

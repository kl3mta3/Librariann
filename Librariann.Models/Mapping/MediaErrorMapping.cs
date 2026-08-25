using System;
using System.Linq.Expressions;
using Librariann.Models.DTOs.MediaErrors;
using Librariann.Models.Entities;

namespace Librariann.Models.Mapping;

/// <summary>Explicit replacement for <c>CreateMap&lt;MediaError, MediaErrorDto&gt;()</c>.</summary>
public static class MediaErrorMapping
{
    public static readonly Expression<Func<MediaError, MediaErrorDto>> ToMediaErrorDtoExpression = e => new MediaErrorDto
    {
        Extension = e.Extension,
        FilePath = e.FilePath,
        Comment = e.Comment,
        Details = e.Details,
        CreatedUtc = e.CreatedUtc,
    };

    private static readonly Func<MediaError, MediaErrorDto> CompiledToMediaErrorDto = ToMediaErrorDtoExpression.Compile();

    public static MediaErrorDto ToMediaErrorDto(this MediaError e) => CompiledToMediaErrorDto(e);
}

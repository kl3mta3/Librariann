using System;
using System.Linq.Expressions;
using Librariann.Models.DTOs.Metadata;
using Librariann.Models.Entities;

namespace Librariann.Models.Mapping;

/// <summary>
/// Explicit replacement for <c>CreateMap&lt;Genre, GenreTagDto&gt;()</c> and
/// <c>CreateMap&lt;Tag, TagDto&gt;()</c> — both are the same flat shape (Id, Title).
/// </summary>
public static class GenreTagMapping
{
    public static readonly Expression<Func<Genre, GenreTagDto>> ToGenreTagDtoExpression = g => new GenreTagDto
    {
        Id = g.Id,
        Title = g.Title,
    };

    public static readonly Expression<Func<Tag, TagDto>> ToTagDtoExpression = t => new TagDto
    {
        Id = t.Id,
        Title = t.Title,
    };

    private static readonly Func<Genre, GenreTagDto> CompiledToGenreTagDto = ToGenreTagDtoExpression.Compile();
    private static readonly Func<Tag, TagDto> CompiledToTagDto = ToTagDtoExpression.Compile();

    public static GenreTagDto ToGenreTagDto(this Genre g) => CompiledToGenreTagDto(g);
    public static TagDto ToTagDto(this Tag t) => CompiledToTagDto(t);
}

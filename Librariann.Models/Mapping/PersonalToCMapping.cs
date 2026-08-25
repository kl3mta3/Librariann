using System;
using System.Linq.Expressions;
using Librariann.Models.DTOs.Reader;
using Librariann.Models.Entities.User;

namespace Librariann.Models.Mapping;

/// <summary>Explicit replacement for <c>CreateMap&lt;AppUserTableOfContent, PersonalToCDto&gt;()</c>.</summary>
public static class PersonalToCMapping
{
    public static readonly Expression<Func<AppUserTableOfContent, PersonalToCDto>> ToPersonalToCDtoExpression = t => new PersonalToCDto
    {
        Id = t.Id,
        ChapterId = t.ChapterId,
        PageNumber = t.PageNumber,
        Title = t.Title,
        BookScrollId = t.BookScrollId,
        SelectedText = t.SelectedText,
        ChapterTitle = t.ChapterTitle,
    };

    private static readonly Func<AppUserTableOfContent, PersonalToCDto> CompiledToPersonalToCDto = ToPersonalToCDtoExpression.Compile();

    public static PersonalToCDto ToPersonalToCDto(this AppUserTableOfContent t) => CompiledToPersonalToCDto(t);
}

using System;
using System.Linq;
using System.Linq.Expressions;
using Librariann.Models.DTOs.Person;
using Librariann.Models.Entities.Person;

namespace Librariann.Models.Mapping;

/// <summary>Explicit replacement for <c>CreateMap&lt;Person, PersonDto&gt;()</c>.</summary>
public static class PersonMapping
{
    public static readonly Expression<Func<Person, PersonDto>> ToPersonDtoExpression = p => new PersonDto
    {
        Id = p.Id,
        Name = p.Name,
        CoverImageLocked = p.CoverImageLocked,
        PrimaryColor = p.PrimaryColor,
        SecondaryColor = p.SecondaryColor,
        CoverImage = p.CoverImage,
        Aliases = p.Aliases.Select(a => a.Alias).ToList(),
        Description = p.Description,
        Asin = p.Asin,
        AniListId = p.AniListId,
        MalId = p.MalId,
        HardcoverId = p.HardcoverId,
        OpenLibraryId = p.OpenLibraryId,
    };

    private static readonly Func<Person, PersonDto> CompiledToPersonDto = ToPersonDtoExpression.Compile();

    public static PersonDto ToPersonDto(this Person p) => CompiledToPersonDto(p);
}

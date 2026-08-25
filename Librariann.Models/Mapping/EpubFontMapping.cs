using System;
using System.Linq.Expressions;
using Librariann.Models.DTOs.Font;
using Librariann.Models.Entities;

namespace Librariann.Models.Mapping;

/// <summary>Explicit replacement for <c>CreateMap&lt;EpubFont, EpubFontDto&gt;()</c>.</summary>
public static class EpubFontMapping
{
    public static readonly Expression<Func<EpubFont, EpubFontDto>> ToEpubFontDtoExpression = f => new EpubFontDto
    {
        Id = f.Id,
        Family = f.Family,
        Name = f.Name,
        Provider = f.Provider,
        FileName = f.FileName,
        Style = f.Style,
        Weight = f.Weight,
    };

    private static readonly Func<EpubFont, EpubFontDto> CompiledToEpubFontDto = ToEpubFontDtoExpression.Compile();

    public static EpubFontDto ToEpubFontDto(this EpubFont f) => CompiledToEpubFontDto(f);
}

using System;
using System.Linq.Expressions;
using Librariann.Models.DTOs.Dashboard;
using Librariann.Models.Entities.User;

namespace Librariann.Models.Mapping;

/// <summary>Explicit replacement for <c>CreateMap&lt;AppUserSmartFilter, SmartFilterDto&gt;()</c>.</summary>
public static class SmartFilterMapping
{
    public static readonly Expression<Func<AppUserSmartFilter, SmartFilterDto>> ToSmartFilterDtoExpression = f => new SmartFilterDto
    {
        Id = f.Id,
        Name = f.Name,
        Filter = f.Filter,
        EntityType = f.EntityType,
    };

    private static readonly Func<AppUserSmartFilter, SmartFilterDto> CompiledToSmartFilterDto = ToSmartFilterDtoExpression.Compile();

    public static SmartFilterDto ToSmartFilterDto(this AppUserSmartFilter f) => CompiledToSmartFilterDto(f);
}

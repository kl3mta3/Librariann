using System;
using System.Linq.Expressions;
using Librariann.Models.DTOs.SideNav;
using Librariann.Models.Entities.User;

namespace Librariann.Models.Mapping;

/// <summary>Explicit replacement for <c>CreateMap&lt;AppUserExternalSource, ExternalSourceDto&gt;()</c>.</summary>
public static class ExternalSourceMapping
{
    public static readonly Expression<Func<AppUserExternalSource, ExternalSourceDto>> ToExternalSourceDtoExpression = s => new ExternalSourceDto
    {
        Id = s.Id,
        Name = s.Name,
        Host = s.Host,
        ApiKey = s.ApiKey,
    };

    private static readonly Func<AppUserExternalSource, ExternalSourceDto> CompiledToExternalSourceDto = ToExternalSourceDtoExpression.Compile();

    public static ExternalSourceDto ToExternalSourceDto(this AppUserExternalSource s) => CompiledToExternalSourceDto(s);
}

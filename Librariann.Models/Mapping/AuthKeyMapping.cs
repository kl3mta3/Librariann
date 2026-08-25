using System;
using System.Linq.Expressions;
using Librariann.Models.DTOs.Account;
using Librariann.Models.Entities.User;

namespace Librariann.Models.Mapping;

/// <summary>Explicit replacement for <c>CreateMap&lt;AppUserAuthKey, AuthKeyDto&gt;()</c>.</summary>
public static class AuthKeyMapping
{
    public static readonly Expression<Func<AppUserAuthKey, AuthKeyDto>> ToAuthKeyDtoExpression = k => new AuthKeyDto
    {
        Id = k.Id,
        Key = k.Key,
        Name = k.Name,
        CreatedAtUtc = k.CreatedAtUtc,
        ExpiresAtUtc = k.ExpiresAtUtc,
        LastAccessedAtUtc = k.LastAccessedAtUtc,
        Provider = k.Provider,
    };

    private static readonly Func<AppUserAuthKey, AuthKeyDto> CompiledToAuthKeyDto = ToAuthKeyDtoExpression.Compile();

    public static AuthKeyDto ToAuthKeyDto(this AppUserAuthKey k) => CompiledToAuthKeyDto(k);
}

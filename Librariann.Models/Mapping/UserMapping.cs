using System;
using System.Linq;
using System.Linq.Expressions;
using Librariann.Models.DTOs;
using Librariann.Models.DTOs.Account;
using Librariann.Models.Entities.User;

namespace Librariann.Models.Mapping;

/// <summary>
/// Explicit replacement for <c>CreateMap&lt;AppUser, UserDto&gt;()</c>. Faithfully leaves
/// <see cref="UserDto.Preferences"/>, <see cref="UserDto.Token"/>, <see cref="UserDto.RefreshToken"/>,
/// <see cref="UserDto.ApiKey"/>, and <see cref="UserDto.LibrariannVersion"/> unset — none of these ever
/// flat-matched an <see cref="AppUser"/> property (the entity's preferences property is named
/// <c>UserPreferences</c>, not <c>Preferences</c>, and the others don't exist on the entity at all), so callers
/// already set them explicitly after mapping (see e.g. <c>AccountController.ConstructUserDto</c>).
/// </summary>
public static class UserMapping
{
    public static readonly Expression<Func<AppUser, UserDto>> ToUserDtoExpression = u => new UserDto
    {
        Id = u.Id,
        OidcId = u.OidcId,
        Username = u.UserName!,
        Email = u.Email!,
        Roles = u.UserRoles.Select(r => r.Role.Name!).ToList(),
        AgeRestriction = new AgeRestrictionDto
        {
            AgeRating = u.AgeRestriction,
            IncludeUnknowns = u.AgeRestrictionIncludeUnknowns,
        },
        IdentityProvider = u.IdentityProvider,
        Created = u.Created,
        CreatedUtc = u.CreatedUtc,
        AuthKeys = u.AuthKeys.Select(k => new AuthKeyDto
        {
            Id = k.Id,
            Key = k.Key,
            Name = k.Name,
            CreatedAtUtc = k.CreatedAtUtc,
            ExpiresAtUtc = k.ExpiresAtUtc,
            LastAccessedAtUtc = k.LastAccessedAtUtc,
            Provider = k.Provider,
        }).ToList(),
        CoverImage = u.CoverImage,
        PrimaryColor = u.PrimaryColor,
        SecondaryColor = u.SecondaryColor,
    };

    private static readonly Func<AppUser, UserDto> CompiledToUserDto = ToUserDtoExpression.Compile();

    public static UserDto ToUserDto(this AppUser u) => CompiledToUserDto(u);
}

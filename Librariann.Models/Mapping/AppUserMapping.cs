using System.Collections.Generic;
using System.Linq;
using Librariann.Models.DTOs.Account;
using Librariann.Models.Entities;
using Librariann.Models.Entities.User;

namespace Librariann.Models.Mapping;

/// <summary>
/// Explicit replacement for <c>CreateMap&lt;AppUser, MemberDto&gt;()</c> (<c>AutoMapperProfiles.cs</c>). There is
/// no <c>ProjectTo</c> call site for this pairing anywhere — both call sites use plain <c>mapper.Map&lt;MemberDto&gt;()</c>
/// — so this is a plain method, not an <c>Expression&lt;Func&lt;&gt;&gt;</c>. Faithfully leaves
/// <see cref="MemberDto.IsPending"/> and <see cref="MemberDto.Roles"/> at their DTO defaults (false/null) exactly
/// as the original did — <see cref="AppUser"/> has no matching properties for AutoMapper's convention to have
/// found either of them from (both are populated by the caller separately, e.g. via
/// <c>UserManager.GetRolesAsync</c>).
/// </summary>
public static class AppUserMapping
{
    public static MemberDto ToMemberDto(this AppUser u) => new()
    {
        Id = u.Id,
        Username = u.UserName,
        Email = u.Email,
        AgeRestriction = new AgeRestrictionDto
        {
            AgeRating = u.AgeRestriction,
            IncludeUnknowns = u.AgeRestrictionIncludeUnknowns,
        },
        Created = u.Created,
        CreatedUtc = u.CreatedUtc,
        LastActive = u.LastActive,
        LastActiveUtc = u.LastActiveUtc,
        IdentityProvider = u.IdentityProvider,
        // Null-guarded: AppUser.Libraries has no default initializer (= null!) so it's genuinely null unless
        // .Include()d or populated via EF relationship fixup - this is a plain in-memory conversion (no query
        // context to translate), so it's safe/necessary to guard here, unlike the shared Expression trees.
        Libraries = (u.Libraries ?? Enumerable.Empty<Library>()).Select(l => l.ToLibraryDto()).ToList(),
    };
}

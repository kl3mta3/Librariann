using Librariann.Models.DTOs.Account;
using Librariann.Models.Entities.User;

namespace Librariann.Models.Mapping;

/// <summary>Explicit replacement for <c>CreateMap&lt;AppUser, MemberInfoDto&gt;()</c>.</summary>
public static class MemberInfoMapping
{
    public static MemberInfoDto ToMemberInfoDto(this AppUser u) => new()
    {
        Id = u.Id,
        Username = u.UserName ?? string.Empty,
        Created = u.Created,
        CreatedUtc = u.CreatedUtc,
        CoverImage = u.CoverImage,
    };
}

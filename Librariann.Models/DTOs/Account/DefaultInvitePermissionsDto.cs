using System.Collections.Generic;
using Librariann.Models.Entities.Enums;

namespace Librariann.Models.DTOs.Account;

/// <summary>
/// Server-wide default roles/libraries/age-restriction applied to pre-fill the invite modal, and used as-is
/// when approving a pending invite request or auto-accepting one - neither of those flows has a UI to pick
/// permissions per-request.
/// </summary>
public sealed record DefaultInvitePermissionsDto
{
    public ICollection<string> Roles { get; init; } = [];
    public IList<int> Libraries { get; init; } = [];
    public AgeRestrictionDto AgeRestriction { get; init; } = new() { AgeRating = AgeRating.NotApplicable, IncludeUnknowns = false };
}

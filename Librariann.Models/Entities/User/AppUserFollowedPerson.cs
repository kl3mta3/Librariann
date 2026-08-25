using System;

namespace Librariann.Models.Entities.User;

/// <summary>
/// A person explicitly followed by a user. The relationship is intentionally local so Home recommendations do not
/// depend on an external metadata account.
/// </summary>
public sealed class AppUserFollowedPerson
{
    public int AppUserId { get; set; }
    public int PersonId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

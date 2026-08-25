using System;
using Librariann.Models.Entities.Interfaces;

namespace Librariann.Models.Entities;

/// <summary>
/// A pending self-service invite request, created when a visitor uses the "Request an Invite" link on the
/// login screen. Not an AppUser - just a mailbox to review/approve/reject before a real account is created via
/// the existing invite flow.
/// </summary>
public class AppUserInviteRequest : IEntityDate
{
    public int Id { get; set; }
    public string Email { get; set; } = default!;
    /// <summary>
    /// Display-only. The invite flow itself has no username field - the invitee picks one during setup.
    /// </summary>
    public string? Name { get; set; }

    public DateTime Created { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime LastModified { get; set; }
    public DateTime LastModifiedUtc { get; set; }
}

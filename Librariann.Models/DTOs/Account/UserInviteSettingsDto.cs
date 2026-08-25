namespace Librariann.Models.DTOs.Account;

/// <summary>
/// The Users-tab invite settings: the login-screen request link, auto-accept, and the default permission set
/// applied to new invites (manual, approved, or auto-accepted).
/// </summary>
public sealed record UserInviteSettingsDto
{
    public bool ShowRequestInviteLink { get; init; }
    public bool AutoAcceptInviteRequests { get; init; }
    public DefaultInvitePermissionsDto DefaultInvitePermissions { get; init; } = new();
}

using System.ComponentModel.DataAnnotations;

namespace Librariann.Models.DTOs.Account;

/// <summary>
/// Submitted anonymously from the login screen's "Request an Invite" link.
/// </summary>
public sealed record RequestInviteDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = default!;
    /// <summary>
    /// Display-only, shown to admins reviewing the request. Not used to create the account.
    /// </summary>
    public string? Name { get; set; }
}

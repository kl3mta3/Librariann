namespace Librariann.Models.DTOs.Account;

/// <summary>
/// What an anonymous requester sees back from request-invite. Never exposes an invite link or setup details -
/// those only ever go to the requester's own inbox (auto-accept path) or to an admin (pending-approval path).
/// </summary>
public sealed record RequestInviteResponseDto
{
    /// <summary>True if auto-accept processed it immediately and mailed an invite. False if it's pending admin review.</summary>
    public bool AutoAccepted { get; init; }
}

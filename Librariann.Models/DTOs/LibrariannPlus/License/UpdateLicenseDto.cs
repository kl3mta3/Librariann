namespace Librariann.Models.DTOs.LibrariannPlus.License;
#nullable enable

public sealed record UpdateLicenseDto
{
    /// <summary>
    /// License Key received from Librariann+
    /// </summary>
    public required string License { get; set; }
    /// <summary>
    /// Email registered with Stripe
    /// </summary>
    public required string Email { get; set; }
    /// <summary>
    /// Optional DiscordId
    /// </summary>
    public string? DiscordId { get; set; }
}

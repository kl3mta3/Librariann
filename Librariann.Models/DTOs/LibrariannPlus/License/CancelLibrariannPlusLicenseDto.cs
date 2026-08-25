namespace Librariann.Models.DTOs.LibrariannPlus.License;
#nullable enable

public sealed record CancelLibrariannPlusLicenseDto
{
    public required string Email { get; set; }
    /// <summary>
    /// Optional comment to tell why you cancelled
    /// </summary>
    public string? Comment  { get; set; }
}

namespace Librariann.Models.DTOs.LibrariannPlus.License;

public sealed record ResetLicenseDto
{
    public required string License { get; set; }
    public required string InstallId { get; set; }
    public required string EmailId { get; set; }
}

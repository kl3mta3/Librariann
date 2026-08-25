using System.ComponentModel.DataAnnotations;

namespace Librariann.Models.DTOs.LibrariannPlus.License;
#nullable enable

public sealed record RenewLibrariannPlusLicenseDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The billing cadence to renew on. Only <see cref="LibrariannPlusBillingInterval.Month"/> and
    /// <see cref="LibrariannPlusBillingInterval.Year"/> are supported.
    /// </summary>
    [EnumDataType(typeof(LibrariannPlusBillingInterval))]
    [Required]
    public LibrariannPlusBillingInterval BillingInterval { get; set; }
}

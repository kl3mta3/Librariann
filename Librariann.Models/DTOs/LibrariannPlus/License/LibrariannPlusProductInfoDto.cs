using System.ComponentModel.DataAnnotations;
namespace Librariann.Models.DTOs.LibrariannPlus.License;
#nullable enable

public sealed record LibrariannPlusProductInfoDto
{
    /// <summary>
    /// Stripe product name (e.g. "Librariann+")
    /// </summary>
    public string? ProductName { get; set; }

    /// <summary>
    /// List price in cents (0 = free)
    /// </summary>
    public long PriceAmount { get; set; }

    /// <summary>
    /// ISO currency code (e.g. "usd")
    /// </summary>
    public string PriceCurrency { get; set; }

    /// <summary>
    /// Billing cycle interval the renewal request should send to select this product
    /// </summary>
    [EnumDataType(typeof(LibrariannPlusBillingInterval))]
    public LibrariannPlusBillingInterval BillingInterval { get; set; }
}
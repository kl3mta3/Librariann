using System.ComponentModel.DataAnnotations;
using Librariann.Models.Entities.Enums.Device;

namespace Librariann.Models.DTOs.Device.EmailDevice;

public sealed record CreateEmailDeviceDto
{
    [Required]
    public string Name { get; set; } = default!;
    /// <summary>
    /// Platform of the device. If not know, defaults to "Custom"
    /// </summary>
    [EnumDataType(typeof(EmailDevicePlatform))]
    [Required]
    public EmailDevicePlatform Platform { get; set; }
    [Required]
    public string EmailAddress { get; set; } = default!;


}

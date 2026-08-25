using System.ComponentModel.DataAnnotations;
namespace Librariann.Models.DTOs.LibrariannPlus.License;

public sealed record LibrariannPlusRegisterResultDto
{
    public bool Success { get; set; }
    public bool IsSubscriptionActive { get; set; }
    [EnumDataType(typeof(LibrariannPlusRegistrationErrorCode))]
    public LibrariannPlusRegistrationErrorCode ErrorCode { get; set; }
}
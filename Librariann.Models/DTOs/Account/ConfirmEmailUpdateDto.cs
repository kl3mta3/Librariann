using System.ComponentModel.DataAnnotations;

namespace Librariann.Models.DTOs.Account;

public sealed record ConfirmEmailUpdateDto
{
    [Required]
    public string Email { get; set; } = default!;
    [Required]
    public string Token { get; set; } = default!;
}

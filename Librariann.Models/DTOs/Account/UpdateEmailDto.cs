using System.ComponentModel.DataAnnotations;

namespace Librariann.Models.DTOs.Account;

public sealed record UpdateEmailDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
}

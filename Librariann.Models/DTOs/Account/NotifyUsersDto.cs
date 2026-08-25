using System.ComponentModel.DataAnnotations;

namespace Librariann.Models.DTOs.Account;

public sealed record NotifyUsersDto
{
    /// <summary>When true, only users with the Admin role receive the notice. When false, everyone does.</summary>
    public bool AdminsOnly { get; init; }
    [Required]
    public string Message { get; set; } = default!;
}

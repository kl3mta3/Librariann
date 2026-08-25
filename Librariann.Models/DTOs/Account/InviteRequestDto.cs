using System;

namespace Librariann.Models.DTOs.Account;

public sealed record InviteRequestDto
{
    public int Id { get; init; }
    public string Email { get; init; } = default!;
    public string? Name { get; init; }
    public DateTime RequestedUtc { get; init; }
}

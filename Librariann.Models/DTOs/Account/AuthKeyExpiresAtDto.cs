using System;

namespace Librariann.Models.DTOs.Account;

public sealed record AuthKeyExpiresAtDto
{
    public required DateTime? ExpiresAt { get; set; }
}

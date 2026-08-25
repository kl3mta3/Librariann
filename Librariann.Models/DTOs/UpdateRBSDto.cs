using System.Collections.Generic;

namespace Librariann.Models.DTOs;
#nullable enable

public sealed record UpdateRbsDto
{
    public required string Username { get; init; }
    public IList<string>? Roles { get; init; }
}

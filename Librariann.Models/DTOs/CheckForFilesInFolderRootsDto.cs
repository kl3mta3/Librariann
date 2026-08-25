using System.Collections.Generic;

namespace Librariann.Models.DTOs;

public sealed record CheckForFilesInFolderRootsDto
{
    public ICollection<string> Roots { get; init; }
}

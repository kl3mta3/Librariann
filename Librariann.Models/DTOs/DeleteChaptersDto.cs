using System.Collections.Generic;

namespace Librariann.Models.DTOs;

public sealed record DeleteChaptersDto
{
    public IList<int> ChapterIds { get; set; } = default!;
}

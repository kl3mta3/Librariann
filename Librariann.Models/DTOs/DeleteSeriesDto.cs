using System.Collections.Generic;

namespace Librariann.Models.DTOs;

public sealed record DeleteSeriesDto
{
    public IList<int> SeriesIds { get; set; } = default!;
}

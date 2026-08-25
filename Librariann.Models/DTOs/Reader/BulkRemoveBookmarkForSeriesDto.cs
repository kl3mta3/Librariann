using System.Collections.Generic;

namespace Librariann.Models.DTOs.Reader;

public sealed record BulkRemoveBookmarkForSeriesDto
{
    public ICollection<int> SeriesIds { get; init; } = default!;
}

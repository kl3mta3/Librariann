using System.Collections.Generic;
using Librariann.Models.DTOs.Collection;

namespace Librariann.Models.DTOs.CollectionTags;

public sealed record UpdateSeriesForTagDto
{
    public AppUserCollectionDto Tag { get; init; } = default!;
    public IEnumerable<int> SeriesIdsToRemove { get; init; } = default!;
}

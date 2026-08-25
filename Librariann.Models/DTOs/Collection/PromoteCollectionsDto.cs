using System.Collections.Generic;

namespace Librariann.Models.DTOs.Collection;

public class PromoteCollectionsDto
{
    public IList<int> CollectionIds { get; init; }
    public bool Promoted { get; init; }
}

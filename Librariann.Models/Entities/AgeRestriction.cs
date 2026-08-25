using Librariann.Models.Entities.Enums;

namespace Librariann.Models.Entities;

public class AgeRestriction
{
    public AgeRating AgeRating { get; set; }
    public bool IncludeUnknowns { get; set; }
}

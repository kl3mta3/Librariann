using Librariann.Models.DTOs.LibrariannPlus.Metadata;

namespace Librariann.Models.DTOs.Metadata.Matching;

public sealed record ExternalSeriesMatchDto
{
    public ExternalSeriesDetailDto Series { get; set; }
    public float MatchRating { get; set; }
}

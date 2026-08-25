using System.Collections.Generic;
using Librariann.Models.DTOs.LibrariannPlus.Metadata;
using Librariann.Models.DTOs.Recommendation;

namespace Librariann.Models.DTOs.SeriesDetail;
#nullable enable

/// <summary>
/// All the data from Librariann+ for Series Detail
/// </summary>
/// <remarks>This is what the UI sees, not what the API sends back</remarks>
public sealed record SeriesDetailPlusDto
{
    public RecommendationDto? Recommendations { get; set; }
    public IEnumerable<UserReviewDto> Reviews { get; set; }
    public IEnumerable<RatingDto>? Ratings { get; set; }
    public ExternalSeriesDetailDto? Series { get; set; }
}

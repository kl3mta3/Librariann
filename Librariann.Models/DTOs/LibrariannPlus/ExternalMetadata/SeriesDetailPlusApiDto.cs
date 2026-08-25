using System.Collections.Generic;
using Librariann.Models.DTOs.LibrariannPlus.Metadata;
using Librariann.Models.DTOs.Scrobbling;
using Librariann.Models.DTOs.SeriesDetail;

namespace Librariann.Models.DTOs.LibrariannPlus.ExternalMetadata;
#nullable enable

public sealed record SeriesDetailPlusApiDto
{
    public IEnumerable<MediaRecommendationDto> Recommendations { get; set; }
    /// <summary>
    /// MangaBaka tag-vector similar series (v3). Populated only on the v3 path.
    /// </summary>
    public IEnumerable<MediaRecommendationDto> SimilarSeries { get; set; } = [];
    /// <summary>
    /// MangaBaka collaborative-filtering "readers also like" series (v3). Populated only on the v3 path.
    /// </summary>
    public IEnumerable<MediaRecommendationDto> ReadersAlsoLike { get; set; } = [];
    public IEnumerable<UserReviewDto> Reviews { get; set; }
    public IEnumerable<RatingDto> Ratings { get; set; }
    public ExternalSeriesDetailDto? Series { get; set; }
    public int? AniListId { get; set; }
    public long? MalId { get; set; }
    public int? MangabakaId { get; set; }
    public int? HardCoverId { get; set; }
    public int? CbrId { get; set; }
}

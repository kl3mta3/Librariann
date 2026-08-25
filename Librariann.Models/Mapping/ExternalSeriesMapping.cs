using Librariann.Models.DTOs.Recommendation;
using Librariann.Models.Entities.Metadata;

namespace Librariann.Models.Mapping;

/// <summary>Explicit replacement for <c>CreateMap&lt;ExternalRecommendation, ExternalSeriesDto&gt;()</c>.</summary>
public static class ExternalSeriesMapping
{
    public static ExternalSeriesDto ToExternalSeriesDto(this ExternalRecommendation r) => new()
    {
        Name = r.Name,
        CoverUrl = r.CoverUrl,
        Url = r.Url,
        Summary = r.Summary,
        AniListId = r.AniListId,
        MangaBakaId = r.MangaBakaId,
        MalId = r.MalId,
        Provider = r.Provider,
        MetadataProvider = r.MetadataProvider,
        RecommendationSource = r.RecommendationSource,
        AgeRating = r.AgeRating,
    };
}

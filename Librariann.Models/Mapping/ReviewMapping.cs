using Librariann.Common.Helpers;
using Librariann.Models.DTOs.SeriesDetail;
using Librariann.Models.Entities.Metadata;

namespace Librariann.Models.Mapping;

/// <summary>
/// Explicit replacement for the old bidirectional AutoMapper profiles between <see cref="ExternalReview"/> and
/// <see cref="UserReviewDto"/>. Both were flat/convention maps with one <c>ForMember</c> override each; behavior is
/// preserved exactly, including that <see cref="ExternalReview"/> has no <c>LibraryId</c>/<c>UserId</c> properties,
/// so those stay at their <see cref="UserReviewDto"/> defaults (0) here too, same as AutoMapper's original mapping.
/// </summary>
public static class ReviewMapping
{
    public static UserReviewDto ToUserReviewDto(this ExternalReview r) => new()
    {
        Tagline = r.Tagline,
        Body = r.Body,
        BodyJustText = r.BodyJustText,
        SeriesId = r.SeriesId,
        ChapterId = r.ChapterId,
        Username = r.Username,
        TotalVotes = r.TotalVotes,
        Rating = r.Rating,
        RawBody = r.RawBody,
        Score = r.Score,
        SiteUrl = r.SiteUrl,
        IsExternal = true,
        Provider = r.Provider,
        Authority = r.Authority,
    };

    public static ExternalReview ToExternalReview(this UserReviewDto dto) => new()
    {
        Tagline = dto.Tagline,
        Body = dto.Body,
        BodyJustText = HtmlHelper.GetCharacters(dto.Body)!,
        RawBody = dto.RawBody,
        Provider = dto.Provider,
        Authority = dto.Authority,
        SiteUrl = dto.SiteUrl,
        Username = dto.Username,
        Rating = (int) dto.Rating,
        Score = dto.Score,
        TotalVotes = dto.TotalVotes,
        SeriesId = dto.SeriesId,
        ChapterId = dto.ChapterId,
    };
}

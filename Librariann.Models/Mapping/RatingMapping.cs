using System;
using System.Linq.Expressions;
using Librariann.Models.DTOs;
using Librariann.Models.Entities.Metadata;

namespace Librariann.Models.Mapping;

/// <summary>
/// Explicit replacement for the old bidirectional AutoMapper profiles between <see cref="ExternalRating"/> and
/// <see cref="RatingDto"/>. Both were flat/convention maps with no <c>ForMember</c> overrides, so only the
/// properties that exist on both sides are copied here, matching AutoMapper's original behavior exactly (e.g. an
/// <see cref="ExternalRating"/> built from a <see cref="RatingDto"/> leaves <c>Id</c>/<c>SeriesId</c>/<c>ChapterId</c>
/// at their defaults, same as AutoMapper did).
/// </summary>
public static class RatingMapping
{
    /// <summary>
    /// Expression form, reusable directly in EF Core queries (e.g. <c>.Select(RatingMapping.ToRatingDtoExpression)</c>)
    /// so this translates to SQL exactly like the old <c>ProjectTo&lt;RatingDto&gt;</c> did.
    /// </summary>
    public static readonly Expression<Func<ExternalRating, RatingDto>> ToRatingDtoExpression = r => new RatingDto
    {
        AverageScore = r.AverageScore,
        FavoriteCount = r.FavoriteCount,
        Provider = r.Provider,
        Authority = r.Authority,
        ProviderUrl = r.ProviderUrl,
    };

    private static readonly Func<ExternalRating, RatingDto> CompiledToRatingDto = ToRatingDtoExpression.Compile();

    public static RatingDto ToRatingDto(this ExternalRating r) => CompiledToRatingDto(r);

    public static ExternalRating ToExternalRating(this RatingDto dto) => new()
    {
        AverageScore = dto.AverageScore,
        FavoriteCount = dto.FavoriteCount,
        Provider = dto.Provider,
        Authority = dto.Authority,
        ProviderUrl = dto.ProviderUrl,
    };
}

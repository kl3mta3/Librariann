using System;
using System.Linq;
using System.Linq.Expressions;
using Librariann.Models.DTOs.SeriesDetail;
using Librariann.Models.Entities;
using Librariann.Models.Entities.Enums;
using Librariann.Models.Entities.User;

namespace Librariann.Models.Mapping;

/// <summary>
/// Explicit replacement for <c>CreateMap&lt;AppUserRating, UserReviewDto&gt;()</c> and
/// <c>CreateMap&lt;AppUserRating, UserReviewExtendedDto&gt;()</c> (<c>AutoMapperProfiles.cs</c>).
/// </summary>
public static class AppUserRatingMapping
{
    /// <summary>EF-translatable form, used via <c>ProjectTo</c>/<c>.Select()</c>.</summary>
    public static readonly Expression<Func<AppUserRating, UserReviewDto>> ToUserReviewDtoExpression = r => new UserReviewDto
    {
        SeriesId = r.SeriesId,
        Rating = r.Rating,
        LibraryId = r.Series.LibraryId,
        Body = r.Review!,
        UserId = r.AppUser.Id,
        Username = r.AppUser.UserName!,
    };

    /// <summary>
    /// Plain-object form for the one call site (<c>ReviewController</c>) that calls this directly on a freshly
    /// built/fetched <see cref="AppUserRating"/> whose <see cref="AppUserRating.Series"/>/
    /// <see cref="AppUserRating.AppUser"/> navigation properties are never loaded there (no <c>.Include()</c> on
    /// that read path, old or new). Null-guarded to faithfully replicate AutoMapper's automatic null-propagation
    /// for property-chain <c>MapFrom</c> lambdas — the original code silently left <c>LibraryId</c>/
    /// <c>Username</c>/<c>UserId</c> at their defaults there rather than throwing, and still does here.
    /// </summary>
    public static UserReviewDto ToUserReviewDto(this AppUserRating r) => new()
    {
        SeriesId = r.SeriesId,
        Rating = r.Rating,
        LibraryId = r.Series?.LibraryId ?? 0,
        Body = r.Review!,
        UserId = r.AppUser?.Id ?? 0,
        Username = r.AppUser?.UserName!,
    };

    /// <summary>
    /// EF-translatable form for the sole (<c>ProjectTo</c>-based) call site. <c>Series</c> is a scalar reference
    /// navigation, so it can't be composed via <c>IQueryable.Select(Expression)</c> the way collection navigations
    /// can (see <see cref="VolumeMapping"/>); instead this correlates a fresh subquery against the caller's
    /// <c>context.Series</c> by id, which EF Core translates natively and needs no <c>.Include()</c> at all.
    /// </summary>
    public static Expression<Func<AppUserRating, UserReviewExtendedDto>> ToUserReviewExtendedDtoExpression(IQueryable<Series> seriesSet) => r => new UserReviewExtendedDto
    {
        Id = r.Id,
        Body = r.Review!,
        SeriesId = r.SeriesId,
        ChapterId = null,
        LibraryId = r.Series.LibraryId,
        Username = r.AppUser.UserName!,
        Rating = r.Rating,
        CreatedUtc = r.CreatedUtc,
        Series = seriesSet.Where(s => s.Id == r.SeriesId).Select(SeriesMapping.ToSeriesDtoExpression(0)).First(),
        Writers = r.Series.Metadata.People
            .Where(p => p.Role == PersonRole.Writer)
            .OrderBy(p => p.OrderWeight)
            .Select(p => p.Person)
            .AsQueryable()
            .Select(PersonMapping.ToPersonDtoExpression)
            .ToList(),
        Chapter = null,
    };
}

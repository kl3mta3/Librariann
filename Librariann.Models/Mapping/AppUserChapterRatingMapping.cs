using System;
using System.Linq;
using System.Linq.Expressions;
using Librariann.Models.DTOs.SeriesDetail;
using Librariann.Models.Entities;
using Librariann.Models.Entities.Enums;
using Librariann.Models.Entities.User;

namespace Librariann.Models.Mapping;

/// <summary>
/// Explicit replacement for <c>CreateMap&lt;AppUserChapterRating, UserReviewDto&gt;()</c> and
/// <c>CreateMap&lt;AppUserChapterRating, UserReviewExtendedDto&gt;()</c> (<c>AutoMapperProfiles.cs</c>).
/// </summary>
public static class AppUserChapterRatingMapping
{
    /// <summary>
    /// EF-translatable form, used via <c>ProjectTo</c>/<c>.Select()</c>. Faithfully leaves
    /// <see cref="UserReviewDto.UserId"/> at its default (0) — the original profile never had a ForMember for it
    /// here (unlike the <see cref="AppUserRatingMapping"/> equivalent, which explicitly sets it from
    /// <c>src.AppUser.Id</c>), and <see cref="AppUserChapterRating"/>'s own <c>AppUserId</c> property doesn't
    /// convention-match <c>UserReviewDto.UserId</c> by name, so it was never populated.
    /// </summary>
    public static readonly Expression<Func<AppUserChapterRating, UserReviewDto>> ToUserReviewDtoExpression = r => new UserReviewDto
    {
        SeriesId = r.SeriesId,
        ChapterId = r.ChapterId,
        Rating = r.Rating,
        LibraryId = r.Series.LibraryId,
        Body = r.Review!,
        Username = r.AppUser.UserName!,
    };

    /// <summary>
    /// Plain-object form, null-guarded — see the equivalent note on
    /// <see cref="AppUserRatingMapping.ToUserReviewDto"/>. No current call site actually needs this (the only
    /// plain <c>Map&lt;UserReviewDto&gt;</c> call sites in <c>ReviewController</c> use
    /// <see cref="AppUserRatingMapping"/>/this type respectively) but is provided for parity/future-proofing.
    /// </summary>
    public static UserReviewDto ToUserReviewDto(this AppUserChapterRating r) => new()
    {
        SeriesId = r.SeriesId,
        ChapterId = r.ChapterId,
        Rating = r.Rating,
        LibraryId = r.Series?.LibraryId ?? 0,
        Body = r.Review!,
        Username = r.AppUser?.UserName!,
    };

    /// <summary>
    /// EF-translatable form for the sole (<c>ProjectTo</c>-based) call site. Both <c>Series</c> and <c>Chapter</c>
    /// are scalar reference navigations, so — like <see cref="AppUserRatingMapping.ToUserReviewExtendedDtoExpression"/>
    /// — each is filled via a correlated subquery against the caller's own queryables rather than composed
    /// in-place, since EF Core only supports splicing a shared Expression via
    /// <c>IQueryable.Select(Expression)</c> for collection navigations (see <see cref="VolumeMapping"/>).
    /// </summary>
    public static Expression<Func<AppUserChapterRating, UserReviewExtendedDto>> ToUserReviewExtendedDtoExpression(
        IQueryable<Series> seriesSet, IQueryable<Chapter> chapterSet) => r => new UserReviewExtendedDto
    {
        Id = r.Id,
        Body = r.Review!,
        SeriesId = r.SeriesId,
        ChapterId = r.ChapterId,
        LibraryId = r.Series.LibraryId,
        Username = r.AppUser.UserName!,
        Rating = r.Rating,
        CreatedUtc = r.CreatedUtc,
        Series = seriesSet.Where(s => s.Id == r.SeriesId).Select(SeriesMapping.ToSeriesDtoExpression(0)).First(),
        Chapter = chapterSet.Where(c => c.Id == r.ChapterId).Select(ChapterMapping.ToChapterDtoExpression(0)).First(),
        Writers = r.Chapter.People
            .Where(p => p.Role == PersonRole.Writer)
            .OrderBy(p => p.OrderWeight)
            .Select(p => p.Person)
            .AsQueryable()
            .Select(PersonMapping.ToPersonDtoExpression)
            .ToList(),
    };
}

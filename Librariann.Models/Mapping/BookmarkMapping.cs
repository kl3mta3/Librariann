using System;
using System.Linq;
using System.Linq.Expressions;
using Librariann.Models.DTOs.Reader;
using Librariann.Models.Entities;
using Librariann.Models.Entities.User;

namespace Librariann.Models.Mapping;

/// <summary>
/// Explicit replacement for the two <c>CreateMap&lt;..., BookmarkDto&gt;()</c> registrations in
/// <c>AutoMapperProfiles.cs</c>. Both mapped nested <see cref="Series"/> to <see cref="Librariann.Models.DTOs.SeriesDto"/>
/// using the captured <c>userId = 0</c> placeholder, exactly like every other plain (non-
/// <c>ProjectToWithProgress</c>) call site — none of the original <c>ProjectTo&lt;BookmarkDto&gt;()</c> call
/// sites passed a userId parameter, so the nested Series's per-user progress/rating fields were always computed
/// as if for user 0.
///
/// The nested <c>Series</c> is a SCALAR reference navigation, not a collection, so it can't be composed the way
/// <see cref="VolumeMapping"/> composes <see cref="ChapterMapping"/> for its <c>Chapters</c> collection (EF Core
/// supports splicing a shared <c>Expression&lt;Func&lt;&gt;&gt;</c> via <c>IQueryable.Select(Expression)</c> for
/// collection navigations, not scalar ones). Instead, each expression here takes the caller's <c>context.Series</c>
/// queryable and correlates a fresh subquery against it by id — this is a standard, EF-Core-translatable
/// correlated-subquery pattern and, notably, needs no <c>.Include()</c> at all (unlike materializing the entity
/// graph and mapping client-side, which would silently produce incomplete data for any nav-collection Include
/// this forgot — <c>Series.Ratings</c>/<c>Series.Progress</c>/<c>Series.Library</c> all being required here).
/// </summary>
public static class BookmarkMapping
{
    /// <summary>Explicit replacement for the bare (convention-only) <c>CreateMap&lt;AppUserBookmark, BookmarkDto&gt;()</c>.</summary>
    public static Expression<Func<AppUserBookmark, BookmarkDto>> ToBookmarkDtoExpression(IQueryable<Series> seriesSet) => b => new BookmarkDto
    {
        Id = b.Id,
        Page = b.Page,
        VolumeId = b.VolumeId,
        SeriesId = b.SeriesId,
        ChapterId = b.ChapterId,
        ImageOffset = b.ImageOffset,
        XPath = b.XPath,
        ChapterTitle = b.ChapterTitle,
        Series = seriesSet.Where(s => s.Id == b.SeriesId).Select(SeriesMapping.ToSeriesDtoExpression(0)).FirstOrDefault(),
    };

    /// <summary>
    /// Explicit replacement for <c>CreateMap&lt;BookmarkSeriesPair, BookmarkDto&gt;()</c>. Faithfully leaves
    /// <see cref="BookmarkDto.ImageOffset"/>/<see cref="BookmarkDto.XPath"/>/<see cref="BookmarkDto.ChapterTitle"/>
    /// at their DTO defaults (0/null/null) exactly as the original did — the original profile had no ForMember
    /// for any of the three, and <see cref="Librariann.Models.Entities.User.BookmarkSeriesPair"/> has no flat
    /// properties by those names for AutoMapper's convention matching to have found, so they were always left
    /// unmapped.
    /// </summary>
    public static Expression<Func<Librariann.Models.Entities.User.BookmarkSeriesPair, BookmarkDto>> ToBookmarkDtoFromPairExpression(IQueryable<Series> seriesSet) => pair => new BookmarkDto
    {
        Id = pair.Bookmark.Id,
        Page = pair.Bookmark.Page,
        VolumeId = pair.Bookmark.VolumeId,
        SeriesId = pair.Bookmark.SeriesId,
        ChapterId = pair.Bookmark.ChapterId,
        Series = seriesSet.Where(s => s.Id == pair.Bookmark.SeriesId).Select(SeriesMapping.ToSeriesDtoExpression(0)).FirstOrDefault(),
    };
}

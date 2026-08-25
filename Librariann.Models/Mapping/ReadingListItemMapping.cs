using System;
using System.Linq;
using System.Linq.Expressions;
using Librariann.Models.DTOs.ReadingLists;
using Librariann.Models.Entities.Enums;
using Librariann.Models.Entities.ReadingLists;

namespace Librariann.Models.Mapping;

/// <summary>
/// Explicit replacement for <c>CreateMap&lt;ReadingListItem, ReadingListItemDto&gt;()</c>
/// (<c>AutoMapperReadingListProfile.cs</c>). Uses the same runtime-<c>userId</c>-factory pattern as
/// <see cref="SeriesMapping"/>/<see cref="ChapterMapping"/> for the per-user <c>PagesRead</c>/
/// <c>LastReadingProgressUtc</c> fields (the sole call site uses
/// <c>ProjectToWithProgress&lt;ReadingListItem, ReadingListItemDto&gt;</c>). <c>Title</c> is intentionally left
/// unset (DTO default), exactly as the original profile's <c>.ForMember(dest => dest.Title, opt =>
/// opt.Ignore())</c> — it's computed elsewhere after the DTO is materialized.
///
/// IMPORTANT: <c>src.Chapter.UserProgress</c> is intentionally NOT null-guarded — see the note on
/// <see cref="ChapterMapping"/> for why wrapping a nav-collection access in <c>?? Enumerable.Empty&lt;T&gt;()</c>
/// breaks EF Core's SQL translation of this Expression when used via <c>ProjectTo</c>/<c>.Select()</c>.
/// </summary>
public static class ReadingListItemMapping
{
    public static Expression<Func<ReadingListItem, ReadingListItemDto>> ToReadingListItemDtoExpression(int userId) => src => new ReadingListItemDto
    {
        Id = src.Id,
        Order = src.Order,
        ChapterId = src.ChapterId,
        SeriesId = src.SeriesId,
        SeriesName = src.Series.Name,
        SeriesSortName = src.Series.SortName,
        SeriesFormat = src.Series.Format,
        LibraryId = src.Series.LibraryId,
        LibraryName = src.Series.Library.Name,
        LibraryType = src.Series.Library.Type,
        VolumeId = src.VolumeId,
        VolumeNumber = src.Volume.Name,
        ChapterNumber = src.Chapter.Range,
        ChapterTitleName = src.Chapter.TitleName,
        PagesTotal = src.Chapter.Pages,
        ReleaseDate = src.Chapter.ReleaseDate,
        Summary = src.Chapter.Summary,
        IsSpecial = src.Chapter.IsSpecial,
        FileSize = src.Chapter.Files.Sum(f => f.Bytes),
        ReadingListId = src.ReadingListId,

        Chapter = new ReadingListItemChapterDto
        {
            Id = src.Chapter.Id,
            Range = src.Chapter.Range,
            TitleName = src.Chapter.TitleName,
            MinNumber = src.Chapter.MinNumber,
            MaxNumber = src.Chapter.MaxNumber,
            SortOrder = src.Chapter.SortOrder,
            Pages = src.Chapter.Pages,
            IsSpecial = src.Chapter.IsSpecial,
            ReleaseDate = src.Chapter.ReleaseDate,
            Summary = src.Chapter.Summary,
            WriterName = src.Chapter.People
                .Where(p => p.Role == PersonRole.Writer)
                .OrderBy(p => p.OrderWeight)
                .Select(p => p.Person.Name)
                .FirstOrDefault(),
            WriterId = src.Chapter.People
                .Where(p => p.Role == PersonRole.Writer)
                .OrderBy(p => p.OrderWeight)
                .Select(p => (int?) p.PersonId)
                .FirstOrDefault(),
            PencillerName = src.Chapter.People
                .Where(p => p.Role == PersonRole.Penciller)
                .OrderBy(p => p.OrderWeight)
                .Select(p => p.Person.Name)
                .FirstOrDefault(),
            PencillerId = src.Chapter.People
                .Where(p => p.Role == PersonRole.Penciller)
                .OrderBy(p => p.OrderWeight)
                .Select(p => (int?) p.PersonId)
                .FirstOrDefault(),
        },

        Volume = new ReadingListItemVolumeDto
        {
            Id = src.Volume.Id,
            Name = src.Volume.Name,
            MinNumber = src.Volume.MinNumber,
            MaxNumber = src.Volume.MaxNumber,
            SeriesId = src.Volume.SeriesId,
        },

        // Per-user progress fields (AutoMapperReadingListProfile.cs)
        PagesRead = src.Chapter.UserProgress
            .Where(p => p.AppUserId == userId)
            .Select(p => (int?) p.PagesRead)
            .FirstOrDefault() ?? 0,
        LastReadingProgressUtc = src.Chapter.UserProgress
            .Where(p => p.AppUserId == userId)
            .Select(p => (DateTime?) p.LastModifiedUtc)
            .FirstOrDefault(),
    };

    public static ReadingListItemDto ToReadingListItemDto(this ReadingListItem item, int userId) =>
        ToReadingListItemDtoExpression(userId).Compile()(item);
}

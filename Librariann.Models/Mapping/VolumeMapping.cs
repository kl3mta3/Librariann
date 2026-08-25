using System;
using System.Linq;
using System.Linq.Expressions;
using Librariann.Models.DTOs;
using Librariann.Models.Entities;

namespace Librariann.Models.Mapping;

/// <summary>
/// Explicit replacement for <c>CreateMap&lt;Volume, VolumeDto&gt;()</c> (<c>AutoMapperVolumeProfile.cs</c>).
/// Same runtime-<c>userId</c>-factory pattern as <see cref="SeriesMapping"/>/<see cref="ChapterMapping"/> for the
/// per-user <see cref="VolumeDto.PagesRead"/> field. Faithfully reproduces two original quirks: the obsolete
/// <see cref="VolumeDto.Number"/> is set from <c>(int) MinNumber</c>, not from the entity's own (differently
/// sourced) obsolete <c>Number</c> field; and <see cref="VolumeDto.CoverImageLocked"/> is a private property on
/// the DTO that AutoMapper (and this expression) never actually populates, so it stays at its default.
///
/// IMPORTANT: nav-collection accesses (<c>v.Chapters</c>, <c>c.UserProgress</c>) are intentionally NOT
/// null-guarded — see the equivalent note on <see cref="ChapterMapping"/>. EF Core cannot translate
/// <c>?? Enumerable.Empty&lt;T&gt;()</c> wrapped around a collection navigation used in a query; it only works
/// unwrapped, where EF instead builds a correlated SQL subquery. Only a direct <c>.Compile()</c>-and-invoke
/// against an under-included in-memory entity can NRE here — fix that at the call site.
/// </summary>
public static class VolumeMapping
{
    public static Expression<Func<Volume, VolumeDto>> ToVolumeDtoExpression(int userId) => v => new VolumeDto
    {
        Id = v.Id,
        MinNumber = v.MinNumber,
        MaxNumber = v.MaxNumber,
        Name = v.Name,
        Number = (int) v.MinNumber,
        Pages = v.Pages,
        PagesRead = v.Chapters.SelectMany(c => c.UserProgress).Where(p => p.AppUserId == userId).Sum(p => (int?) p.PagesRead) ?? 0,
        LastModifiedUtc = v.LastModifiedUtc,
        CreatedUtc = v.CreatedUtc,
        Created = v.Created,
        LastModified = v.LastModified,
        SeriesId = v.SeriesId,
        Chapters = v.Chapters.OrderBy(c => c.SortOrder).AsQueryable().Select(ChapterMapping.ToChapterDtoExpression(userId)).ToList(),
        MinHoursToRead = v.MinHoursToRead,
        MaxHoursToRead = v.MaxHoursToRead,
        AvgHoursToRead = v.AvgHoursToRead,
        WordCount = v.WordCount,
        CoverImage = v.CoverImage,
        PrimaryColor = v.PrimaryColor,
        SecondaryColor = v.SecondaryColor,
        AniListId = v.AniListId,
        MalId = v.MalId,
        HardcoverId = v.HardcoverId,
        MetronId = v.MetronId,
        ComicVineId = v.ComicVineId,
        MangaBakaId = v.MangaBakaId,
        CbrId = v.CbrId,
    };

    public static VolumeDto ToVolumeDto(this Volume v, int userId) => ToVolumeDtoExpression(userId).Compile()(v);
}

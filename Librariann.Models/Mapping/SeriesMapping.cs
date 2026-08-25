using System;
using System.Linq;
using System.Linq.Expressions;
using Librariann.Models.DTOs;
using Librariann.Models.DTOs.LibrariannPlus.Manage;
using Librariann.Models.Entities;

namespace Librariann.Models.Mapping;

/// <summary>
/// Explicit replacement for the merged <c>CreateMap&lt;Series, SeriesDto&gt;()</c> registrations in
/// <c>AutoMapperProfiles.cs</c> (flat fields) and <c>AutoMapperSeriesProfile.cs</c> (the five
/// per-user progress/rating fields). AutoMapper computed those five fields via a runtime-substituted
/// <c>userId</c> parameter passed to <c>ProjectTo</c> (see <c>ProjectToExtensions.ProjectToWithProgress</c>);
/// here that becomes an explicit factory parameter captured as a closure constant in the expression tree, which
/// EF Core translates into a SQL parameter exactly the same way.
/// </summary>
public static class SeriesMapping
{
    public static Expression<Func<Series, SeriesDto>> ToSeriesDtoExpression(int userId) => s => new SeriesDto
    {
        Id = s.Id,
        Name = s.Name,
        OriginalName = s.OriginalName,
        LocalizedName = s.LocalizedName,
        SortName = s.SortName,
        Pages = s.Pages,
        CoverImageLocked = s.CoverImageLocked,
        LastChapterAdded = s.LastChapterAdded,
        LastChapterAddedUtc = s.LastChapterAddedUtc,

        // Per-user progress/rating fields (from AutoMapperSeriesProfile.cs). NOT null-guarded on purpose: this
        // Expression is used directly in EF Core .Select()/ProjectTo queries, which translate navigation-
        // collection access into a correlated SQL subquery without ever reading the actual CLR property, so it's
        // safe there regardless of Include state. Wrapping it in `?? Enumerable.Empty<T>()` makes EF Core throw
        // "could not be translated" instead (confirmed the hard way on ChapterMapping/VolumeMapping — see their
        // XML docs). A plain in-memory .Map()-equivalent call against an under-included Series could still NRE
        // here; there is currently no such call site for SeriesDto (only ProjectTo/.Select() usage), so this is
        // theoretical unless a future caller adds one — fix it at that call site if it ever happens.
        UserRating = s.Ratings.Where(r => r.AppUserId == userId).Select(r => (float?) r.Rating).FirstOrDefault() ?? 0f,
        HasUserRated = s.Ratings.Any(r => r.AppUserId == userId && r.HasBeenRated),
        TotalReads = s.Progress.Where(p => p.AppUserId == userId).Min(p => (int?) p.TotalReads) ?? 0,
        PagesRead = s.Progress.Where(p => p.AppUserId == userId).Sum(p => (int?) p.PagesRead) ?? 0,
        LatestReadDate = s.Progress.Where(p => p.AppUserId == userId).Max(p => (DateTime?) p.LastModified) ?? DateTime.MinValue,

        Format = s.Format,
        Created = s.Created,
        SortNameLocked = s.SortNameLocked,
        LocalizedNameLocked = s.LocalizedNameLocked,
        NameLocked = s.NameLocked,
        WordCount = s.WordCount,
        LibraryId = s.LibraryId,
        LibraryName = s.Library.Name,
        MinHoursToRead = s.MinHoursToRead,
        MaxHoursToRead = s.MaxHoursToRead,
        AvgHoursToRead = s.AvgHoursToRead,
        FolderPath = s.FolderPath!,
        LowestFolderPath = s.LowestFolderPath,
        LastFolderScanned = s.LastFolderScanned,
        DontMatch = s.DontMatch,
        IsBlacklisted = s.IsBlacklisted,
        IsStandAlone = s.IsStandAlone,
        MetadataProviderOverride = s.MetadataProviderOverride,
        CoverImage = s.CoverImage,
        PrimaryColor = s.PrimaryColor,
        SecondaryColor = s.SecondaryColor,
        AniListId = s.AniListId,
        MalId = s.MalId,
        HardcoverId = s.HardcoverId,
        MetronId = s.MetronId,
        ComicVineId = s.ComicVineId,
        MangaBakaId = s.MangaBakaId,
        MangaBakaEditionId = s.MangaBakaEditionId,
        CbrId = s.CbrId,
    };

    public static SeriesDto ToSeriesDto(this Series s, int userId) => ToSeriesDtoExpression(userId).Compile()(s);

    /// <summary>
    /// Explicit replacement for <c>CreateMap&lt;Series, ManageMatchSeriesDto&gt;()</c> (<c>AutoMapperProfiles.cs</c>).
    /// The nested <see cref="ManageMatchSeriesDto.Series"/> is filled from the SAME row as the outer parameter, but
    /// EF Core still can't splice a shared parameterized <see cref="Expression{TDelegate}"/> in-place for a
    /// scalar result (only for collection navigations via <c>IQueryable.Select(Expression)</c> — see
    /// <see cref="VolumeMapping"/>), so this correlates a subquery against the caller's own <c>context.Series</c>
    /// by id, exactly like the other "scalar embed" mappings (<see cref="BookmarkMapping"/>,
    /// <see cref="AppUserRatingMapping"/>).
    /// </summary>
    public static Expression<Func<Series, ManageMatchSeriesDto>> ToManageMatchSeriesDtoExpression(IQueryable<Series> seriesSet) => s => new ManageMatchSeriesDto
    {
        Series = seriesSet.Where(s2 => s2.Id == s.Id).Select(ToSeriesDtoExpression(0)).First(),
        IsMatched = s.ExternalSeriesMetadata != null && s.ExternalSeriesMetadata.ValidUntilUtc > DateTime.MinValue,
        ValidUntilUtc = s.ExternalSeriesMetadata != null ? s.ExternalSeriesMetadata.ValidUntilUtc : DateTime.MinValue,
    };
}

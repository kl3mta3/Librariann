using System;
using System.Linq;
using System.Linq.Expressions;
using Librariann.Models.DTOs;
using Librariann.Models.Entities;
using Librariann.Models.Entities.Enums;

namespace Librariann.Models.Mapping;

/// <summary>
/// Explicit replacement for <c>CreateMap&lt;Chapter, ChapterDto&gt;()</c> and
/// <c>CreateMap&lt;Chapter, StandaloneChapterDto&gt;()</c> (<c>AutoMapperChapterProfile.cs</c>, including its
/// shared <c>MapChapterBase</c> extension covering the per-user progress fields and the 13 role-filtered People
/// collections). The per-user progress fields (<see cref="ChapterDto.PagesRead"/>,
/// <see cref="ChapterDto.LastReadingProgressUtc"/>, <see cref="ChapterDto.LastReadingProgress"/>,
/// <see cref="ChapterDto.TotalReads"/>) use the same runtime-<c>userId</c>-factory pattern as
/// <see cref="SeriesMapping"/>. Faithfully leaves <see cref="ChapterDto.VolumeTitle"/> and
/// <see cref="ChapterDto.PublicationStatus"/>/<c>PublicationStatusLocked</c> at their defaults on the base
/// <c>ChapterDto</c> map, exactly as the original AutoMapper profile did (only <c>StandaloneChapterDto</c> ever
/// set <c>VolumeTitle</c>; nothing ever set <c>PublicationStatus</c> from a <see cref="Chapter"/> at all).
///
/// IMPORTANT: <c>c.UserProgress</c> (and other nav-collection accesses below) are intentionally NOT null-guarded
/// (no <c>?? Enumerable.Empty&lt;T&gt;()</c>) — this Expression is also used directly in <c>.Select()</c>/
/// <c>ProjectTo</c> EF Core queries, which translate navigation-collection member access into a correlated SQL
/// subquery without ever dereferencing the actual CLR property (so it's safe there regardless of Include state).
/// Wrapping it in <c>??</c> makes EF Core throw "could not be translated" instead. The only place this can
/// actually NRE is when <see cref="ToChapterDto"/>/<see cref="ToStandaloneChapterDto"/> below compile-and-invoke
/// this expression directly against an in-memory <see cref="Chapter"/> that was fetched without
/// <c>.Include(c => c.UserProgress)</c> (that collection has no default initializer on the entity). Fix that at
/// the call site (add the missing Include, or default the property first) rather than here.
/// </summary>
public static class ChapterMapping
{
    public static Expression<Func<Chapter, ChapterDto>> ToChapterDtoExpression(int userId) => c => new ChapterDto
    {
        Id = c.Id,
        Range = c.Range,
#pragma warning disable CS0618 // Obsolete Number field, faithfully carried over from the entity's own obsolete field
        Number = c.Number,
#pragma warning restore CS0618
        MinNumber = c.MinNumber,
        MaxNumber = c.MaxNumber,
        SortOrder = c.SortOrder,
        Pages = c.Pages,
        IsSpecial = c.IsSpecial,
        Title = c.Title!,
        Files = c.Files.Select(f => new MangaFileDto
        {
            Id = f.Id,
            FilePath = f.FilePath,
            Pages = f.Pages,
            Bytes = f.Bytes,
            Format = f.Format,
            Created = f.Created,
            Extension = f.Extension,
            KoreaderHash = f.KoreaderHash,
        }).ToList(),

        // Per-user progress fields (AutoMapperChapterProfile.MapChapterBase)
        PagesRead = c.UserProgress.Where(p => p.AppUserId == userId).Select(p => (int?) p.PagesRead).FirstOrDefault() ?? 0,
        LastReadingProgressUtc = c.UserProgress.Where(p => p.AppUserId == userId).Select(p => (DateTime?) p.LastModifiedUtc).FirstOrDefault() ?? DateTime.MinValue,
        LastReadingProgress = c.UserProgress.Where(p => p.AppUserId == userId).Select(p => (DateTime?) p.LastModified).FirstOrDefault() ?? DateTime.MinValue,
        TotalReads = c.UserProgress.Where(p => p.AppUserId == userId).Select(p => (int?) p.TotalReads).FirstOrDefault() ?? 0,

        CoverImageLocked = c.CoverImageLocked,
        VolumeId = c.VolumeId,
        CreatedUtc = c.CreatedUtc,
        LastModifiedUtc = c.LastModifiedUtc,
        Created = c.Created,
        ReleaseDate = c.ReleaseDate,
        TitleName = c.TitleName,
        Summary = c.Summary!,
        AgeRating = c.AgeRating,
        WordCount = c.WordCount,
        MinHoursToRead = c.MinHoursToRead,
        MaxHoursToRead = c.MaxHoursToRead,
        AvgHoursToRead = c.AvgHoursToRead,
        WebLinks = c.WebLinks,
        ISBN = c.ISBN,

        // Role-filtered People collections (AutoMapperChapterProfile.MapChapterBase)
        Writers = c.People.Where(cp => cp.Role == PersonRole.Writer).Select(cp => cp.Person).OrderBy(p => p.NormalizedName)
            .Select(p => new Librariann.Models.DTOs.Person.PersonDto { Id = p.Id, Name = p.Name, CoverImageLocked = p.CoverImageLocked, PrimaryColor = p.PrimaryColor, SecondaryColor = p.SecondaryColor, CoverImage = p.CoverImage, Aliases = p.Aliases.Select(a => a.Alias).ToList(), Description = p.Description, Asin = p.Asin, AniListId = p.AniListId, MalId = p.MalId, HardcoverId = p.HardcoverId, OpenLibraryId = p.OpenLibraryId }).ToList(),
        CoverArtists = c.People.Where(cp => cp.Role == PersonRole.CoverArtist).Select(cp => cp.Person).OrderBy(p => p.NormalizedName)
            .Select(p => new Librariann.Models.DTOs.Person.PersonDto { Id = p.Id, Name = p.Name, CoverImageLocked = p.CoverImageLocked, PrimaryColor = p.PrimaryColor, SecondaryColor = p.SecondaryColor, CoverImage = p.CoverImage, Aliases = p.Aliases.Select(a => a.Alias).ToList(), Description = p.Description, Asin = p.Asin, AniListId = p.AniListId, MalId = p.MalId, HardcoverId = p.HardcoverId, OpenLibraryId = p.OpenLibraryId }).ToList(),
        Publishers = c.People.Where(cp => cp.Role == PersonRole.Publisher).Select(cp => cp.Person).OrderBy(p => p.NormalizedName)
            .Select(p => new Librariann.Models.DTOs.Person.PersonDto { Id = p.Id, Name = p.Name, CoverImageLocked = p.CoverImageLocked, PrimaryColor = p.PrimaryColor, SecondaryColor = p.SecondaryColor, CoverImage = p.CoverImage, Aliases = p.Aliases.Select(a => a.Alias).ToList(), Description = p.Description, Asin = p.Asin, AniListId = p.AniListId, MalId = p.MalId, HardcoverId = p.HardcoverId, OpenLibraryId = p.OpenLibraryId }).ToList(),
        Characters = c.People.Where(cp => cp.Role == PersonRole.Character).Select(cp => cp.Person).OrderBy(p => p.NormalizedName)
            .Select(p => new Librariann.Models.DTOs.Person.PersonDto { Id = p.Id, Name = p.Name, CoverImageLocked = p.CoverImageLocked, PrimaryColor = p.PrimaryColor, SecondaryColor = p.SecondaryColor, CoverImage = p.CoverImage, Aliases = p.Aliases.Select(a => a.Alias).ToList(), Description = p.Description, Asin = p.Asin, AniListId = p.AniListId, MalId = p.MalId, HardcoverId = p.HardcoverId, OpenLibraryId = p.OpenLibraryId }).ToList(),
        Pencillers = c.People.Where(cp => cp.Role == PersonRole.Penciller).Select(cp => cp.Person).OrderBy(p => p.NormalizedName)
            .Select(p => new Librariann.Models.DTOs.Person.PersonDto { Id = p.Id, Name = p.Name, CoverImageLocked = p.CoverImageLocked, PrimaryColor = p.PrimaryColor, SecondaryColor = p.SecondaryColor, CoverImage = p.CoverImage, Aliases = p.Aliases.Select(a => a.Alias).ToList(), Description = p.Description, Asin = p.Asin, AniListId = p.AniListId, MalId = p.MalId, HardcoverId = p.HardcoverId, OpenLibraryId = p.OpenLibraryId }).ToList(),
        Inkers = c.People.Where(cp => cp.Role == PersonRole.Inker).Select(cp => cp.Person).OrderBy(p => p.NormalizedName)
            .Select(p => new Librariann.Models.DTOs.Person.PersonDto { Id = p.Id, Name = p.Name, CoverImageLocked = p.CoverImageLocked, PrimaryColor = p.PrimaryColor, SecondaryColor = p.SecondaryColor, CoverImage = p.CoverImage, Aliases = p.Aliases.Select(a => a.Alias).ToList(), Description = p.Description, Asin = p.Asin, AniListId = p.AniListId, MalId = p.MalId, HardcoverId = p.HardcoverId, OpenLibraryId = p.OpenLibraryId }).ToList(),
        Imprints = c.People.Where(cp => cp.Role == PersonRole.Imprint).Select(cp => cp.Person).OrderBy(p => p.NormalizedName)
            .Select(p => new Librariann.Models.DTOs.Person.PersonDto { Id = p.Id, Name = p.Name, CoverImageLocked = p.CoverImageLocked, PrimaryColor = p.PrimaryColor, SecondaryColor = p.SecondaryColor, CoverImage = p.CoverImage, Aliases = p.Aliases.Select(a => a.Alias).ToList(), Description = p.Description, Asin = p.Asin, AniListId = p.AniListId, MalId = p.MalId, HardcoverId = p.HardcoverId, OpenLibraryId = p.OpenLibraryId }).ToList(),
        Colorists = c.People.Where(cp => cp.Role == PersonRole.Colorist).Select(cp => cp.Person).OrderBy(p => p.NormalizedName)
            .Select(p => new Librariann.Models.DTOs.Person.PersonDto { Id = p.Id, Name = p.Name, CoverImageLocked = p.CoverImageLocked, PrimaryColor = p.PrimaryColor, SecondaryColor = p.SecondaryColor, CoverImage = p.CoverImage, Aliases = p.Aliases.Select(a => a.Alias).ToList(), Description = p.Description, Asin = p.Asin, AniListId = p.AniListId, MalId = p.MalId, HardcoverId = p.HardcoverId, OpenLibraryId = p.OpenLibraryId }).ToList(),
        Letterers = c.People.Where(cp => cp.Role == PersonRole.Letterer).Select(cp => cp.Person).OrderBy(p => p.NormalizedName)
            .Select(p => new Librariann.Models.DTOs.Person.PersonDto { Id = p.Id, Name = p.Name, CoverImageLocked = p.CoverImageLocked, PrimaryColor = p.PrimaryColor, SecondaryColor = p.SecondaryColor, CoverImage = p.CoverImage, Aliases = p.Aliases.Select(a => a.Alias).ToList(), Description = p.Description, Asin = p.Asin, AniListId = p.AniListId, MalId = p.MalId, HardcoverId = p.HardcoverId, OpenLibraryId = p.OpenLibraryId }).ToList(),
        Editors = c.People.Where(cp => cp.Role == PersonRole.Editor).Select(cp => cp.Person).OrderBy(p => p.NormalizedName)
            .Select(p => new Librariann.Models.DTOs.Person.PersonDto { Id = p.Id, Name = p.Name, CoverImageLocked = p.CoverImageLocked, PrimaryColor = p.PrimaryColor, SecondaryColor = p.SecondaryColor, CoverImage = p.CoverImage, Aliases = p.Aliases.Select(a => a.Alias).ToList(), Description = p.Description, Asin = p.Asin, AniListId = p.AniListId, MalId = p.MalId, HardcoverId = p.HardcoverId, OpenLibraryId = p.OpenLibraryId }).ToList(),
        Translators = c.People.Where(cp => cp.Role == PersonRole.Translator).Select(cp => cp.Person).OrderBy(p => p.NormalizedName)
            .Select(p => new Librariann.Models.DTOs.Person.PersonDto { Id = p.Id, Name = p.Name, CoverImageLocked = p.CoverImageLocked, PrimaryColor = p.PrimaryColor, SecondaryColor = p.SecondaryColor, CoverImage = p.CoverImage, Aliases = p.Aliases.Select(a => a.Alias).ToList(), Description = p.Description, Asin = p.Asin, AniListId = p.AniListId, MalId = p.MalId, HardcoverId = p.HardcoverId, OpenLibraryId = p.OpenLibraryId }).ToList(),
        Teams = c.People.Where(cp => cp.Role == PersonRole.Team).Select(cp => cp.Person).OrderBy(p => p.NormalizedName)
            .Select(p => new Librariann.Models.DTOs.Person.PersonDto { Id = p.Id, Name = p.Name, CoverImageLocked = p.CoverImageLocked, PrimaryColor = p.PrimaryColor, SecondaryColor = p.SecondaryColor, CoverImage = p.CoverImage, Aliases = p.Aliases.Select(a => a.Alias).ToList(), Description = p.Description, Asin = p.Asin, AniListId = p.AniListId, MalId = p.MalId, HardcoverId = p.HardcoverId, OpenLibraryId = p.OpenLibraryId }).ToList(),
        Locations = c.People.Where(cp => cp.Role == PersonRole.Location).Select(cp => cp.Person).OrderBy(p => p.NormalizedName)
            .Select(p => new Librariann.Models.DTOs.Person.PersonDto { Id = p.Id, Name = p.Name, CoverImageLocked = p.CoverImageLocked, PrimaryColor = p.PrimaryColor, SecondaryColor = p.SecondaryColor, CoverImage = p.CoverImage, Aliases = p.Aliases.Select(a => a.Alias).ToList(), Description = p.Description, Asin = p.Asin, AniListId = p.AniListId, MalId = p.MalId, HardcoverId = p.HardcoverId, OpenLibraryId = p.OpenLibraryId }).ToList(),

        // Flat-convention nested collections (no ForMember in the original; matched by property name)
        Genres = c.Genres.Select(g => new Librariann.Models.DTOs.Metadata.GenreTagDto { Id = g.Id, Title = g.Title }).ToList(),
        Tags = c.Tags.Select(t => new Librariann.Models.DTOs.Metadata.TagDto { Id = t.Id, Title = t.Title }).ToList(),

        Language = c.Language,
        Count = c.Count,
        TotalCount = c.TotalCount,

        LanguageLocked = c.LanguageLocked,
        SummaryLocked = c.SummaryLocked,
        AgeRatingLocked = c.AgeRatingLocked,
        GenresLocked = c.GenresLocked,
        TagsLocked = c.TagsLocked,
        WriterLocked = c.WriterLocked,
        CharacterLocked = c.CharacterLocked,
        ColoristLocked = c.ColoristLocked,
        EditorLocked = c.EditorLocked,
        InkerLocked = c.InkerLocked,
        ImprintLocked = c.ImprintLocked,
        LettererLocked = c.LettererLocked,
        PencillerLocked = c.PencillerLocked,
        PublisherLocked = c.PublisherLocked,
        TranslatorLocked = c.TranslatorLocked,
        TeamLocked = c.TeamLocked,
        LocationLocked = c.LocationLocked,
        CoverArtistLocked = c.CoverArtistLocked,
        ReleaseDateLocked = c.ReleaseDateLocked,
        TitleNameLocked = c.TitleNameLocked,
        SortOrderLocked = c.SortOrderLocked,

        CoverImage = c.CoverImage,
        PrimaryColor = c.PrimaryColor,
        SecondaryColor = c.SecondaryColor,

        AniListId = c.AniListId,
        MalId = c.MalId,
        HardcoverId = c.HardcoverId,
        MetronId = c.MetronId,
        ComicVineId = c.ComicVineId,
        MangaBakaId = c.MangaBakaId,
        CbrId = c.CbrId,
    };

    public static Expression<Func<Chapter, StandaloneChapterDto>> ToStandaloneChapterDtoExpression(int userId) => c => new StandaloneChapterDto
    {
        Id = c.Id,
        Range = c.Range,
#pragma warning disable CS0618
        Number = c.Number,
#pragma warning restore CS0618
        MinNumber = c.MinNumber,
        MaxNumber = c.MaxNumber,
        SortOrder = c.SortOrder,
        Pages = c.Pages,
        IsSpecial = c.IsSpecial,
        Title = c.Title!,
        Files = c.Files.Select(f => new MangaFileDto
        {
            Id = f.Id,
            FilePath = f.FilePath,
            Pages = f.Pages,
            Bytes = f.Bytes,
            Format = f.Format,
            Created = f.Created,
            Extension = f.Extension,
            KoreaderHash = f.KoreaderHash,
        }).ToList(),

        PagesRead = c.UserProgress.Where(p => p.AppUserId == userId).Select(p => (int?) p.PagesRead).FirstOrDefault() ?? 0,
        LastReadingProgressUtc = c.UserProgress.Where(p => p.AppUserId == userId).Select(p => (DateTime?) p.LastModifiedUtc).FirstOrDefault() ?? DateTime.MinValue,
        LastReadingProgress = c.UserProgress.Where(p => p.AppUserId == userId).Select(p => (DateTime?) p.LastModified).FirstOrDefault() ?? DateTime.MinValue,
        TotalReads = c.UserProgress.Where(p => p.AppUserId == userId).Select(p => (int?) p.TotalReads).FirstOrDefault() ?? 0,

        CoverImageLocked = c.CoverImageLocked,
        VolumeId = c.VolumeId,
        CreatedUtc = c.CreatedUtc,
        LastModifiedUtc = c.LastModifiedUtc,
        Created = c.Created,
        ReleaseDate = c.ReleaseDate,
        TitleName = c.TitleName,
        Summary = c.Summary!,
        AgeRating = c.AgeRating,
        WordCount = c.WordCount,
        MinHoursToRead = c.MinHoursToRead,
        MaxHoursToRead = c.MaxHoursToRead,
        AvgHoursToRead = c.AvgHoursToRead,
        WebLinks = c.WebLinks,
        ISBN = c.ISBN,

        Writers = c.People.Where(cp => cp.Role == PersonRole.Writer).Select(cp => cp.Person).OrderBy(p => p.NormalizedName)
            .Select(p => new Librariann.Models.DTOs.Person.PersonDto { Id = p.Id, Name = p.Name, CoverImageLocked = p.CoverImageLocked, PrimaryColor = p.PrimaryColor, SecondaryColor = p.SecondaryColor, CoverImage = p.CoverImage, Aliases = p.Aliases.Select(a => a.Alias).ToList(), Description = p.Description, Asin = p.Asin, AniListId = p.AniListId, MalId = p.MalId, HardcoverId = p.HardcoverId, OpenLibraryId = p.OpenLibraryId }).ToList(),
        CoverArtists = c.People.Where(cp => cp.Role == PersonRole.CoverArtist).Select(cp => cp.Person).OrderBy(p => p.NormalizedName)
            .Select(p => new Librariann.Models.DTOs.Person.PersonDto { Id = p.Id, Name = p.Name, CoverImageLocked = p.CoverImageLocked, PrimaryColor = p.PrimaryColor, SecondaryColor = p.SecondaryColor, CoverImage = p.CoverImage, Aliases = p.Aliases.Select(a => a.Alias).ToList(), Description = p.Description, Asin = p.Asin, AniListId = p.AniListId, MalId = p.MalId, HardcoverId = p.HardcoverId, OpenLibraryId = p.OpenLibraryId }).ToList(),
        Publishers = c.People.Where(cp => cp.Role == PersonRole.Publisher).Select(cp => cp.Person).OrderBy(p => p.NormalizedName)
            .Select(p => new Librariann.Models.DTOs.Person.PersonDto { Id = p.Id, Name = p.Name, CoverImageLocked = p.CoverImageLocked, PrimaryColor = p.PrimaryColor, SecondaryColor = p.SecondaryColor, CoverImage = p.CoverImage, Aliases = p.Aliases.Select(a => a.Alias).ToList(), Description = p.Description, Asin = p.Asin, AniListId = p.AniListId, MalId = p.MalId, HardcoverId = p.HardcoverId, OpenLibraryId = p.OpenLibraryId }).ToList(),
        Characters = c.People.Where(cp => cp.Role == PersonRole.Character).Select(cp => cp.Person).OrderBy(p => p.NormalizedName)
            .Select(p => new Librariann.Models.DTOs.Person.PersonDto { Id = p.Id, Name = p.Name, CoverImageLocked = p.CoverImageLocked, PrimaryColor = p.PrimaryColor, SecondaryColor = p.SecondaryColor, CoverImage = p.CoverImage, Aliases = p.Aliases.Select(a => a.Alias).ToList(), Description = p.Description, Asin = p.Asin, AniListId = p.AniListId, MalId = p.MalId, HardcoverId = p.HardcoverId, OpenLibraryId = p.OpenLibraryId }).ToList(),
        Pencillers = c.People.Where(cp => cp.Role == PersonRole.Penciller).Select(cp => cp.Person).OrderBy(p => p.NormalizedName)
            .Select(p => new Librariann.Models.DTOs.Person.PersonDto { Id = p.Id, Name = p.Name, CoverImageLocked = p.CoverImageLocked, PrimaryColor = p.PrimaryColor, SecondaryColor = p.SecondaryColor, CoverImage = p.CoverImage, Aliases = p.Aliases.Select(a => a.Alias).ToList(), Description = p.Description, Asin = p.Asin, AniListId = p.AniListId, MalId = p.MalId, HardcoverId = p.HardcoverId, OpenLibraryId = p.OpenLibraryId }).ToList(),
        Inkers = c.People.Where(cp => cp.Role == PersonRole.Inker).Select(cp => cp.Person).OrderBy(p => p.NormalizedName)
            .Select(p => new Librariann.Models.DTOs.Person.PersonDto { Id = p.Id, Name = p.Name, CoverImageLocked = p.CoverImageLocked, PrimaryColor = p.PrimaryColor, SecondaryColor = p.SecondaryColor, CoverImage = p.CoverImage, Aliases = p.Aliases.Select(a => a.Alias).ToList(), Description = p.Description, Asin = p.Asin, AniListId = p.AniListId, MalId = p.MalId, HardcoverId = p.HardcoverId, OpenLibraryId = p.OpenLibraryId }).ToList(),
        Imprints = c.People.Where(cp => cp.Role == PersonRole.Imprint).Select(cp => cp.Person).OrderBy(p => p.NormalizedName)
            .Select(p => new Librariann.Models.DTOs.Person.PersonDto { Id = p.Id, Name = p.Name, CoverImageLocked = p.CoverImageLocked, PrimaryColor = p.PrimaryColor, SecondaryColor = p.SecondaryColor, CoverImage = p.CoverImage, Aliases = p.Aliases.Select(a => a.Alias).ToList(), Description = p.Description, Asin = p.Asin, AniListId = p.AniListId, MalId = p.MalId, HardcoverId = p.HardcoverId, OpenLibraryId = p.OpenLibraryId }).ToList(),
        Colorists = c.People.Where(cp => cp.Role == PersonRole.Colorist).Select(cp => cp.Person).OrderBy(p => p.NormalizedName)
            .Select(p => new Librariann.Models.DTOs.Person.PersonDto { Id = p.Id, Name = p.Name, CoverImageLocked = p.CoverImageLocked, PrimaryColor = p.PrimaryColor, SecondaryColor = p.SecondaryColor, CoverImage = p.CoverImage, Aliases = p.Aliases.Select(a => a.Alias).ToList(), Description = p.Description, Asin = p.Asin, AniListId = p.AniListId, MalId = p.MalId, HardcoverId = p.HardcoverId, OpenLibraryId = p.OpenLibraryId }).ToList(),
        Letterers = c.People.Where(cp => cp.Role == PersonRole.Letterer).Select(cp => cp.Person).OrderBy(p => p.NormalizedName)
            .Select(p => new Librariann.Models.DTOs.Person.PersonDto { Id = p.Id, Name = p.Name, CoverImageLocked = p.CoverImageLocked, PrimaryColor = p.PrimaryColor, SecondaryColor = p.SecondaryColor, CoverImage = p.CoverImage, Aliases = p.Aliases.Select(a => a.Alias).ToList(), Description = p.Description, Asin = p.Asin, AniListId = p.AniListId, MalId = p.MalId, HardcoverId = p.HardcoverId, OpenLibraryId = p.OpenLibraryId }).ToList(),
        Editors = c.People.Where(cp => cp.Role == PersonRole.Editor).Select(cp => cp.Person).OrderBy(p => p.NormalizedName)
            .Select(p => new Librariann.Models.DTOs.Person.PersonDto { Id = p.Id, Name = p.Name, CoverImageLocked = p.CoverImageLocked, PrimaryColor = p.PrimaryColor, SecondaryColor = p.SecondaryColor, CoverImage = p.CoverImage, Aliases = p.Aliases.Select(a => a.Alias).ToList(), Description = p.Description, Asin = p.Asin, AniListId = p.AniListId, MalId = p.MalId, HardcoverId = p.HardcoverId, OpenLibraryId = p.OpenLibraryId }).ToList(),
        Translators = c.People.Where(cp => cp.Role == PersonRole.Translator).Select(cp => cp.Person).OrderBy(p => p.NormalizedName)
            .Select(p => new Librariann.Models.DTOs.Person.PersonDto { Id = p.Id, Name = p.Name, CoverImageLocked = p.CoverImageLocked, PrimaryColor = p.PrimaryColor, SecondaryColor = p.SecondaryColor, CoverImage = p.CoverImage, Aliases = p.Aliases.Select(a => a.Alias).ToList(), Description = p.Description, Asin = p.Asin, AniListId = p.AniListId, MalId = p.MalId, HardcoverId = p.HardcoverId, OpenLibraryId = p.OpenLibraryId }).ToList(),
        Teams = c.People.Where(cp => cp.Role == PersonRole.Team).Select(cp => cp.Person).OrderBy(p => p.NormalizedName)
            .Select(p => new Librariann.Models.DTOs.Person.PersonDto { Id = p.Id, Name = p.Name, CoverImageLocked = p.CoverImageLocked, PrimaryColor = p.PrimaryColor, SecondaryColor = p.SecondaryColor, CoverImage = p.CoverImage, Aliases = p.Aliases.Select(a => a.Alias).ToList(), Description = p.Description, Asin = p.Asin, AniListId = p.AniListId, MalId = p.MalId, HardcoverId = p.HardcoverId, OpenLibraryId = p.OpenLibraryId }).ToList(),
        Locations = c.People.Where(cp => cp.Role == PersonRole.Location).Select(cp => cp.Person).OrderBy(p => p.NormalizedName)
            .Select(p => new Librariann.Models.DTOs.Person.PersonDto { Id = p.Id, Name = p.Name, CoverImageLocked = p.CoverImageLocked, PrimaryColor = p.PrimaryColor, SecondaryColor = p.SecondaryColor, CoverImage = p.CoverImage, Aliases = p.Aliases.Select(a => a.Alias).ToList(), Description = p.Description, Asin = p.Asin, AniListId = p.AniListId, MalId = p.MalId, HardcoverId = p.HardcoverId, OpenLibraryId = p.OpenLibraryId }).ToList(),

        Genres = c.Genres.Select(g => new Librariann.Models.DTOs.Metadata.GenreTagDto { Id = g.Id, Title = g.Title }).ToList(),
        Tags = c.Tags.Select(t => new Librariann.Models.DTOs.Metadata.TagDto { Id = t.Id, Title = t.Title }).ToList(),

        Language = c.Language,
        Count = c.Count,
        TotalCount = c.TotalCount,

        LanguageLocked = c.LanguageLocked,
        SummaryLocked = c.SummaryLocked,
        AgeRatingLocked = c.AgeRatingLocked,
        GenresLocked = c.GenresLocked,
        TagsLocked = c.TagsLocked,
        WriterLocked = c.WriterLocked,
        CharacterLocked = c.CharacterLocked,
        ColoristLocked = c.ColoristLocked,
        EditorLocked = c.EditorLocked,
        InkerLocked = c.InkerLocked,
        ImprintLocked = c.ImprintLocked,
        LettererLocked = c.LettererLocked,
        PencillerLocked = c.PencillerLocked,
        PublisherLocked = c.PublisherLocked,
        TranslatorLocked = c.TranslatorLocked,
        TeamLocked = c.TeamLocked,
        LocationLocked = c.LocationLocked,
        CoverArtistLocked = c.CoverArtistLocked,
        ReleaseDateLocked = c.ReleaseDateLocked,
        TitleNameLocked = c.TitleNameLocked,
        SortOrderLocked = c.SortOrderLocked,

        CoverImage = c.CoverImage,
        PrimaryColor = c.PrimaryColor,
        SecondaryColor = c.SecondaryColor,

        AniListId = c.AniListId,
        MalId = c.MalId,
        HardcoverId = c.HardcoverId,
        MetronId = c.MetronId,
        ComicVineId = c.ComicVineId,
        MangaBakaId = c.MangaBakaId,
        CbrId = c.CbrId,

        // StandaloneChapterDto-only fields
        SeriesId = c.Volume.SeriesId,
        VolumeTitle = c.Volume.Name,
        LibraryId = c.Volume.Series.LibraryId,
        LibraryType = c.Volume.Series.Library.Type,
    };

    public static ChapterDto ToChapterDto(this Chapter c, int userId) => ToChapterDtoExpression(userId).Compile()(c);
    public static StandaloneChapterDto ToStandaloneChapterDto(this Chapter c, int userId) => ToStandaloneChapterDtoExpression(userId).Compile()(c);
}

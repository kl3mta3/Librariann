using System.Linq;
using Librariann.Models.DTOs;
using Librariann.Models.DTOs.Metadata;
using Librariann.Models.DTOs.Person;
using Librariann.Models.Entities;

namespace Librariann.Models.Mapping;

/// <summary>
/// Explicit replacement for <c>CreateMap&lt;ChapterDto, TachiyomiChapterDto&gt;()</c> and
/// <c>CreateMap&lt;Chapter, TachiyomiChapterDto&gt;()</c>. Both were bare/flat maps with no <c>ForMember</c> and,
/// critically, the <c>Chapter</c>-sourced one does <b>not</b> call <c>MapChapterBase</c> the way
/// <c>Chapter&#8594;ChapterDto</c>/<c>StandaloneChapterDto</c> do — so unlike those, this one never populates the
/// 13 role-filtered People collections or the per-user progress fields (they stay at <see cref="ChapterDto"/>'s
/// own declared defaults: empty lists, zero, <c>DateTime.MinValue</c>). Only used via plain in-memory
/// <c>Map&lt;TachiyomiChapterDto&gt;()</c> calls (never <c>ProjectTo</c>), so no <c>Expression</c> form is needed.
/// </summary>
public static class TachiyomiChapterMapping
{
    /// <summary>
    /// Flat copy of every <see cref="ChapterDto"/> field (source and destination share the same shape via
    /// inheritance) plus the destination-only <see cref="TachiyomiChapterDto.Number"/>, which AutoMapper matched
    /// by name against <see cref="ChapterDto"/>'s own obsolete <c>Number</c> field — the whole point of this DTO
    /// per its own doc comment ("Number field was removed in v0.8.0, but Tachiyomi needs it for the hacks").
    /// </summary>
    public static TachiyomiChapterDto ToTachiyomiChapterDto(this ChapterDto c) => new()
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
        Title = c.Title,
        Files = c.Files,
        PagesRead = c.PagesRead,
        TotalReads = c.TotalReads,
        LastReadingProgressUtc = c.LastReadingProgressUtc,
        LastReadingProgress = c.LastReadingProgress,
        CoverImageLocked = c.CoverImageLocked,
        VolumeId = c.VolumeId,
        CreatedUtc = c.CreatedUtc,
        LastModifiedUtc = c.LastModifiedUtc,
        Created = c.Created,
        ReleaseDate = c.ReleaseDate,
        TitleName = c.TitleName,
        Summary = c.Summary,
        AgeRating = c.AgeRating,
        WordCount = c.WordCount,
        VolumeTitle = c.VolumeTitle,
        MinHoursToRead = c.MinHoursToRead,
        MaxHoursToRead = c.MaxHoursToRead,
        AvgHoursToRead = c.AvgHoursToRead,
        WebLinks = c.WebLinks,
        ISBN = c.ISBN,
        Writers = c.Writers,
        CoverArtists = c.CoverArtists,
        Publishers = c.Publishers,
        Characters = c.Characters,
        Pencillers = c.Pencillers,
        Inkers = c.Inkers,
        Imprints = c.Imprints,
        Colorists = c.Colorists,
        Letterers = c.Letterers,
        Editors = c.Editors,
        Translators = c.Translators,
        Teams = c.Teams,
        Locations = c.Locations,
        Genres = c.Genres,
        Tags = c.Tags,
        PublicationStatus = c.PublicationStatus,
        Language = c.Language,
        Count = c.Count,
        TotalCount = c.TotalCount,
        LanguageLocked = c.LanguageLocked,
        SummaryLocked = c.SummaryLocked,
        AgeRatingLocked = c.AgeRatingLocked,
        PublicationStatusLocked = c.PublicationStatusLocked,
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

    /// <summary>
    /// Flat-convention fields only: scalars that match by name, plus <c>Files</c>/<c>Genres</c>/<c>Tags</c> (which
    /// also match by name on both <see cref="Chapter"/> and <see cref="ChapterDto"/>). Deliberately does
    /// <b>not</b> populate the role-filtered People collections or per-user progress fields — see class remarks.
    /// </summary>
    public static TachiyomiChapterDto ToTachiyomiChapterDto(this Chapter c) => new()
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
        Genres = c.Genres.Select(g => new GenreTagDto { Id = g.Id, Title = g.Title }).ToList(),
        Tags = c.Tags.Select(t => new TagDto { Id = t.Id, Title = t.Title }).ToList(),
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
}

using System;
using System.Linq;
using System.Linq.Expressions;
using Librariann.Models.DTOs;
using Librariann.Models.Entities.Enums;
using Librariann.Models.Entities.Metadata;

namespace Librariann.Models.Mapping;

/// <summary>
/// Explicit replacement for <c>CreateMap&lt;SeriesMetadata, SeriesMetadataDto&gt;()</c>
/// (<c>AutoMapperProfiles.cs</c>). No per-user parameterization needed — every call site uses a plain
/// <c>ProjectTo</c>. Same 12-role people-filter shape as <see cref="ChapterMapping"/>, but faithfully preserves
/// one real difference from the original profile: every role orders by <c>Person.NormalizedName</c> AFTER
/// selecting the <see cref="Librariann.Models.Entities.Person.Person"/>, EXCEPT <c>Characters</c>, which orders
/// by the join row's <c>OrderWeight</c> BEFORE selecting the Person — don't "fix" that inconsistency.
/// </summary>
public static class SeriesMetadataMapping
{
    public static readonly Expression<Func<SeriesMetadata, SeriesMetadataDto>> ToSeriesMetadataDtoExpression = sm => new SeriesMetadataDto
    {
        Id = sm.Id,
        Summary = sm.Summary,

        Genres = sm.Genres.OrderBy(g => g.NormalizedTitle).AsQueryable().Select(GenreTagMapping.ToGenreTagDtoExpression).ToList(),
        Tags = sm.Tags.OrderBy(t => t.NormalizedTitle).AsQueryable().Select(GenreTagMapping.ToTagDtoExpression).ToList(),

        Writers = sm.People.Where(p => p.Role == PersonRole.Writer).Select(p => p.Person).OrderBy(p => p.NormalizedName)
            .AsQueryable().Select(PersonMapping.ToPersonDtoExpression).ToList(),
        CoverArtists = sm.People.Where(p => p.Role == PersonRole.CoverArtist).Select(p => p.Person).OrderBy(p => p.NormalizedName)
            .AsQueryable().Select(PersonMapping.ToPersonDtoExpression).ToList(),
        Publishers = sm.People.Where(p => p.Role == PersonRole.Publisher).Select(p => p.Person).OrderBy(p => p.NormalizedName)
            .AsQueryable().Select(PersonMapping.ToPersonDtoExpression).ToList(),
        // Characters is the one role ordered by the join row's OrderWeight, before selecting Person - not
        // by NormalizedName like every other role here. Faithful to the original profile.
        Characters = sm.People.Where(p => p.Role == PersonRole.Character).OrderBy(p => p.OrderWeight).Select(p => p.Person)
            .AsQueryable().Select(PersonMapping.ToPersonDtoExpression).ToList(),
        Pencillers = sm.People.Where(p => p.Role == PersonRole.Penciller).Select(p => p.Person).OrderBy(p => p.NormalizedName)
            .AsQueryable().Select(PersonMapping.ToPersonDtoExpression).ToList(),
        Inkers = sm.People.Where(p => p.Role == PersonRole.Inker).Select(p => p.Person).OrderBy(p => p.NormalizedName)
            .AsQueryable().Select(PersonMapping.ToPersonDtoExpression).ToList(),
        Imprints = sm.People.Where(p => p.Role == PersonRole.Imprint).Select(p => p.Person).OrderBy(p => p.NormalizedName)
            .AsQueryable().Select(PersonMapping.ToPersonDtoExpression).ToList(),
        Colorists = sm.People.Where(p => p.Role == PersonRole.Colorist).Select(p => p.Person).OrderBy(p => p.NormalizedName)
            .AsQueryable().Select(PersonMapping.ToPersonDtoExpression).ToList(),
        Letterers = sm.People.Where(p => p.Role == PersonRole.Letterer).Select(p => p.Person).OrderBy(p => p.NormalizedName)
            .AsQueryable().Select(PersonMapping.ToPersonDtoExpression).ToList(),
        Editors = sm.People.Where(p => p.Role == PersonRole.Editor).Select(p => p.Person).OrderBy(p => p.NormalizedName)
            .AsQueryable().Select(PersonMapping.ToPersonDtoExpression).ToList(),
        Translators = sm.People.Where(p => p.Role == PersonRole.Translator).Select(p => p.Person).OrderBy(p => p.NormalizedName)
            .AsQueryable().Select(PersonMapping.ToPersonDtoExpression).ToList(),
        Teams = sm.People.Where(p => p.Role == PersonRole.Team).Select(p => p.Person).OrderBy(p => p.NormalizedName)
            .AsQueryable().Select(PersonMapping.ToPersonDtoExpression).ToList(),
        Locations = sm.People.Where(p => p.Role == PersonRole.Location).Select(p => p.Person).OrderBy(p => p.NormalizedName)
            .AsQueryable().Select(PersonMapping.ToPersonDtoExpression).ToList(),

        AgeRating = sm.AgeRating,
        ReleaseYear = sm.ReleaseYear,
        Language = sm.Language,
        MaxCount = sm.MaxCount,
        TotalCount = sm.TotalCount,
        PublicationStatus = sm.PublicationStatus,
        WebLinks = sm.WebLinks,

        LanguageLocked = sm.LanguageLocked,
        SummaryLocked = sm.SummaryLocked,
        AgeRatingLocked = sm.AgeRatingLocked,
        PublicationStatusLocked = sm.PublicationStatusLocked,
        GenresLocked = sm.GenresLocked,
        TagsLocked = sm.TagsLocked,
        WriterLocked = sm.WriterLocked,
        CharacterLocked = sm.CharacterLocked,
        ColoristLocked = sm.ColoristLocked,
        EditorLocked = sm.EditorLocked,
        InkerLocked = sm.InkerLocked,
        ImprintLocked = sm.ImprintLocked,
        LettererLocked = sm.LettererLocked,
        PencillerLocked = sm.PencillerLocked,
        PublisherLocked = sm.PublisherLocked,
        TranslatorLocked = sm.TranslatorLocked,
        TeamLocked = sm.TeamLocked,
        LocationLocked = sm.LocationLocked,
        CoverArtistLocked = sm.CoverArtistLocked,
        ReleaseYearLocked = sm.ReleaseYearLocked,

        SeriesId = sm.SeriesId,
    };

    private static readonly Func<SeriesMetadata, SeriesMetadataDto> CompiledToSeriesMetadataDto = ToSeriesMetadataDtoExpression.Compile();

    public static SeriesMetadataDto ToSeriesMetadataDto(this SeriesMetadata sm) => CompiledToSeriesMetadataDto(sm);
}

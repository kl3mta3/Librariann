using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Librariann.Common.Extensions;
using Librariann.Models.Entities;
using Librariann.Models.Entities.Enums;

namespace Librariann.Models.Metadata;
#nullable enable

/// <summary>
/// A representation of a ComicInfo.xml file
/// </summary>
/// <remarks>See reference of the loose spec here: https://anansi-project.github.io/docs/comicinfo/documentation</remarks>
public class ComicInfo
{
    public string Summary { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Series { get; set; } = string.Empty;
    /// <summary>
    /// Localized Series name. Not standard.
    /// </summary>
    public string LocalizedSeries { get; set; } = string.Empty;
    public string SeriesSort { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    /// <summary>
    /// The total number of items in the series.
    /// </summary>
    [System.ComponentModel.DefaultValueAttribute(0)]
    public int Count { get; set; } = 0;
    public string Volume { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public int PageCount { get; set; }
    // ReSharper disable once InconsistentNaming
    /// <summary>
    /// IETF BCP 47 Code to represent the language of the content
    /// </summary>
    public string LanguageISO { get; set; } = string.Empty;

    // ReSharper disable once InconsistentNaming
    /// <summary>
    /// ISBN for the underlying document
    /// </summary>
    /// <remarks>ComicInfo.xml will actually output a GTIN (Global Trade Item Number) and it is the responsibility of the Parser to extract the ISBN. EPub will return ISBN.</remarks>
    public string Isbn { get; set; } = string.Empty;
    /// <summary>
    /// This is only for deserialization and used within <see cref="ArchiveService"/>. Use <see cref="Isbn"/> for the actual value.
    /// </summary>
    public string GTIN { get; set; } = string.Empty;
    /// <summary>
    /// This is the link to where the data was scraped from
    /// </summary>
    /// <remarks>This can be comma-separated</remarks>
    public string Web { get; set; } = string.Empty;
    [System.ComponentModel.DefaultValueAttribute(0)]
    public int Day { get; set; } = 0;
    [System.ComponentModel.DefaultValueAttribute(0)]
    public int Month { get; set; } = 0;
    [System.ComponentModel.DefaultValueAttribute(0)]
    public int Year { get; set; } = 0;


    /// <summary>
    /// Rating based on the content. Think PG-13, R for movies. See <see cref="AgeRating"/> for valid types
    /// </summary>
    public string AgeRating { get; set; } = string.Empty;
    /// <summary>
    /// User's rating of the content
    /// </summary>
    public float UserRating { get; set; }
    /// <summary>
    /// Can contain multiple comma separated strings, each create a <see cref="CollectionTag"/>
    /// </summary>
    public string SeriesGroup { get; set; } = string.Empty;

    /// <summary>
    /// Can contain multiple comma separated numbers that match with StoryArcNumber
    /// </summary>
    public string StoryArc { get; set; } = string.Empty;
    /// <summary>
    /// Can contain multiple comma separated numbers that match with StoryArc
    /// </summary>
    public string StoryArcNumber { get; set; } = string.Empty;
    public string AlternateNumber { get; set; } = string.Empty;
    public string AlternateSeries { get; set; } = string.Empty;

    /// <summary>
    /// Not used
    /// </summary>
    [System.ComponentModel.DefaultValueAttribute(0)]
    public int AlternateCount { get; set; } = 0;

    /// <summary>
    /// This is Epub only: calibre:title_sort
    /// Represents the sort order for the title
    /// </summary>
    public string TitleSort { get; set; } = string.Empty;
    /// <summary>
    /// This comes from ComicInfo and is free form text. We use this to validate against a set of tags and mark a file as
    /// special.
    /// </summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// The translator, can be comma separated. This is part of ComicInfo.xml draft v2.1
    /// </summary>
    /// See https://github.com/anansi-project/comicinfo/issues/2 for information about this tag
    public string Translator { get; set; } = string.Empty;
    /// <summary>
    /// Misc tags. This is part of ComicInfo.xml draft v2.1
    /// </summary>
    /// See https://github.com/anansi-project/comicinfo/issues/1 for information about this tag
    public string Tags { get; set; } = string.Empty;

    /// <summary>
    /// This is the Author. For Books, we map creator tag in OPF to this field. Comma separated if multiple.
    /// </summary>
    public string Writer { get; set; } = string.Empty;
    public string Penciller { get; set; } = string.Empty;
    public string Inker { get; set; } = string.Empty;
    public string Colorist { get; set; } = string.Empty;
    public string Letterer { get; set; } = string.Empty;
    public string CoverArtist { get; set; } = string.Empty;
    public string Editor { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string Imprint { get; set; } = string.Empty;
    public string Characters { get; set; } = string.Empty;
    public string Teams { get; set; } = string.Empty;
    public string Locations { get; set; } = string.Empty;

    public IList<string> GetPeopleForRole(PersonRole role) => role switch
    {
        PersonRole.Writer => SplitNames(Writer),
        PersonRole.Penciller => SplitNames(Penciller),
        PersonRole.Inker => SplitNames(Inker),
        PersonRole.Colorist => SplitNames(Colorist),
        PersonRole.Letterer => SplitNames(Letterer),
        PersonRole.CoverArtist => SplitNames(CoverArtist),
        PersonRole.Editor => SplitNames(Editor),
        PersonRole.Publisher => SplitNames(Publisher),
        PersonRole.Translator => SplitNames(Translator),
        PersonRole.Imprint => SplitNames(Imprint),
        PersonRole.Character => SplitNames(Characters),
        PersonRole.Team => SplitNames(Teams),
        PersonRole.Location => SplitNames(Locations),
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
    };

    // Different tagging tools (ComicRack, ComicTagger, calibre, etc.) join multiple people in a role
    // with different delimiters - comma is most common, but semicolon shows up often enough (e.g.
    // "Brian Herbert; Kevin J. Anderson") that treating it as literal name text silently produces one
    // garbled Person instead of two real ones. Semicolon is split first and treated as an unambiguous
    // person separator; comma is handled per-segment below since it's genuinely ambiguous.
    private static string[] SplitNames(string value) =>
        value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .SelectMany(SplitCommaSegment)
            .ToArray();

    /// <summary>
    /// Comma is overloaded: it separates multiple people in a list ("Brian Herbert, Kevin J. Anderson"), but
    /// it's also the "Last, First" single-name format many library/ebook tools embed ("Sanderson, Brandon").
    /// Naively splitting the latter on comma produces two permanent, unrelated Person rows ("Sanderson" and
    /// "Brandon") instead of one "Brandon Sanderson" - confirmed as a real, reproducible bug against an actual
    /// library (an EPUB tagged "Sanderson, Brandon" as its author). The distinguishing shape: a genuine
    /// multi-person comma list almost always has at least a first+last per person, so exactly two comma
    /// segments that are *each a single word* (no internal whitespace) is reassembled as one "First Last" name
    /// instead of split into two people. Three or more segments, or any segment with a space in it, are left as
    /// a real list - this only catches the specific ambiguous shape, not multi-person lists in general.
    /// </summary>
    private static IEnumerable<string> SplitCommaSegment(string segment)
    {
        var parts = segment.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 2 && !parts[0].Contains(' ') && !parts[1].Contains(' '))
        {
            return [$"{parts[1]} {parts[0]}"];
        }
        return parts;
    }

    public static AgeRating ConvertAgeRatingToEnum(string value)
    {
        if (string.IsNullOrEmpty(value)) return Entities.Enums.AgeRating.Unknown;
        return Enum.GetValues<AgeRating>()
            .SingleOrDefault(t => t.ToDescription().ToUpperInvariant().Equals(value.ToUpperInvariant()), Entities.Enums.AgeRating.Unknown);
    }
}

using System.Collections.Generic;
using System.Linq;
using Librariann.Models.DTOs.LibrariannPlus.Metadata;
using Librariann.Models.Entities.Enums;
using Librariann.Models.Entities.MetadataMatching;

namespace Librariann.Models.Mapping;

/// <summary>Explicit replacement for <c>CreateMap&lt;MetadataSettings, MetadataSettingsDto&gt;()</c>.</summary>
public static class MetadataSettingsMapping
{
    public static MetadataSettingsDto ToMetadataSettingsDto(this MetadataSettings s) => new()
    {
        Enabled = s.Enabled,
        EnableExtendedMetadataProcessing = s.EnableExtendedMetadataProcessing,
        EnableSummary = s.EnableSummary,
        EnablePublicationStatus = s.EnablePublicationStatus,
        EnableAgeRating = s.EnableAgeRating,
        EnableRelationships = s.EnableRelationships,
        EnablePeople = s.EnablePeople,
        EnableStartDate = s.EnableStartDate,
        EnableLocalizedName = s.EnableLocalizedName,
        EnableName = s.EnableName,
        EnableCoverImage = s.EnableCoverImage,
        EnableChapterSummary = s.EnableChapterSummary,
        EnableChapterReleaseDate = s.EnableChapterReleaseDate,
        EnableChapterTitle = s.EnableChapterTitle,
        EnableChapterPublisher = s.EnableChapterPublisher,
        EnableChapterCoverImage = s.EnableChapterCoverImage,
        EnableVolumeCoverImage = s.EnableVolumeCoverImage,
        EnableGenres = s.EnableGenres,
        EnableTags = s.EnableTags,
        FirstLastPeopleNaming = s.FirstLastPeopleNaming,
        ExternalAgeRatingMappings = s.ExternalAgeRatingMappings,
        GlobalLanguageTitleSettings = new SeriesNameLanguageDto
        {
            Name = s.GlobalNameLanguages ?? string.Empty,
            LocalizedName = s.GlobalLocalizedNameLanguages ?? string.Empty,
        },
        LibraryLanguageTitleOverrides = (s.LibraryLanguageTitleOverrides ?? new Dictionary<int, SeriesNameLanguage>())
            .ToDictionary(kv => kv.Key, kv => kv.Value.ToSeriesNameLanguageDto()),
        Overrides = s.Overrides ?? [],
        PersonRoles = s.PersonRoles,
        FilterAboveWeight = s.FilterAboveWeight,
        Blacklist = s.Blacklist ?? [],
        Whitelist = s.Whitelist ?? [],
        AgeRatingMappings = s.AgeRatingMappings ?? new Dictionary<string, AgeRating>(),
        FieldMappings = s.FieldMappings?.Select(f => f.ToMetadataFieldMappingDto()).ToList() ?? [],
    };
}

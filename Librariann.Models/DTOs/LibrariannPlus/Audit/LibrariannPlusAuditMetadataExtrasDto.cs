using Librariann.Models.DTOs.LibrariannPlus.Audit;
using Librariann.Models.Entities.Enums.Audit;
using System.ComponentModel.DataAnnotations;

namespace Librariann.Models.DTOs.LibrariannPlus;
#nullable enable

/// <summary>
/// Extra context for non-diff Metadata events (cover updates, person operations, metadata fetches).
/// Projected from AuditLogSeriesCoverParamsDto, AuditLogChapterCoverParamsDto,
/// AuditLogPersonAliasParamsDto, AuditLogPersonCoverParamsDto, AuditLogMetadataFetchParamsDto.
/// </summary>
public sealed record LibrariannPlusAuditMetadataExtrasDto
{
    // CoverUpdated, ChapterCoverUpdated, PersonCoverUpdated
    public string? CoverUrl { get; init; }

    // ChapterCoverUpdated
    public string? IssueNumber { get; init; }

    // VolumeCoverUpdated
    public string? VolumeNumber { get; init; }

    // PersonAliasAdded, PersonCoverUpdated
    public string? PersonName { get; init; }

    // PersonAliasAdded
    public string? AliasAdded { get; init; }

    // MetadataFetched - why the fetch fired
    [EnumDataType(typeof(MetadataFetchTrigger))]
    public MetadataFetchTrigger? FetchTrigger { get; init; }

    public static LibrariannPlusAuditMetadataExtrasDto? From(AuditLogSeriesCoverParamsDto? p) =>
        p is null ? null : new LibrariannPlusAuditMetadataExtrasDto { CoverUrl = p.CoverUrl };

    public static LibrariannPlusAuditMetadataExtrasDto? From(AuditLogChapterCoverParamsDto? p) =>
        p is null ? null : new LibrariannPlusAuditMetadataExtrasDto { CoverUrl = p.CoverUrl, IssueNumber = p.IssueNumber };

    public static LibrariannPlusAuditMetadataExtrasDto? From(AuditLogVolumeCoverParamsDto? p) =>
        p is null ? null : new LibrariannPlusAuditMetadataExtrasDto { CoverUrl = p.CoverUrl, VolumeNumber = p.VolumeNumber };

    public static LibrariannPlusAuditMetadataExtrasDto? From(AuditLogPersonAliasParamsDto? p) =>
        p is null ? null : new LibrariannPlusAuditMetadataExtrasDto { PersonName = p.PersonName, AliasAdded = p.AliasAdded };

    public static LibrariannPlusAuditMetadataExtrasDto? From(AuditLogPersonCoverParamsDto? p) =>
        p is null ? null : new LibrariannPlusAuditMetadataExtrasDto { PersonName = p.PersonName, CoverUrl = p.ImageUrl };

    public static LibrariannPlusAuditMetadataExtrasDto? From(AuditLogMetadataFetchParamsDto? p) =>
        p is null ? null : new LibrariannPlusAuditMetadataExtrasDto { FetchTrigger = p.Trigger };
}
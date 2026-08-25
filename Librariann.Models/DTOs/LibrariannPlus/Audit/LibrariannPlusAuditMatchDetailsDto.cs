using Librariann.Models.DTOs.LibrariannPlus.Audit;
using Librariann.Models.Entities.Enums;

namespace Librariann.Models.DTOs.LibrariannPlus;
#nullable enable

/// <summary>
/// Match-specific context surfaced on a Librariann+ audit entry.
/// Projected from AuditLogMatch*ParamsDtos based on EventType.
/// Not returned directly by the API - each From() overload maps one source type.
/// </summary>
public sealed record LibrariannPlusAuditMatchDetailsDto
{
    // SeriesMatched, SeriesMatchCleared
    public string? MatchedName { get; init; }

    // SeriesMatched - external ID snapshots before and after the match
    public AuditLogMatchExternalIdsParamsDto? Before { get; init; }
    public AuditLogMatchExternalIdsParamsDto? After { get; init; }

    // SeriesMatchFailed, SeriesBlacklisted
    public string? Reason { get; init; }

    // SeriesDontMatchSet
    public bool? DontMatch { get; init; }

    // SeriesMetadataProviderOverrideSet
    public MetadataProvider? PreviousProvider { get; init; }
    public MetadataProvider? NewProvider { get; init; }
    /// <summary>
    /// False when the Series fell back to its Library's default provider
    /// </summary>
    public bool? IsProviderOverride { get; init; }

    public static LibrariannPlusAuditMatchDetailsDto? From(AuditLogMatchedParamsDto? p) =>
        p is null ? null : new LibrariannPlusAuditMatchDetailsDto { MatchedName = p.MatchedName, Before = p.Before, After = p.After };

    public static LibrariannPlusAuditMatchDetailsDto? From(AuditLogMatchClearedParamsDto? p) =>
        p is null ? null : new LibrariannPlusAuditMatchDetailsDto { MatchedName = p.MatchedName };

    public static LibrariannPlusAuditMatchDetailsDto? From(AuditLogMatchFailureParamsDto? p) =>
        p is null ? null : new LibrariannPlusAuditMatchDetailsDto { Reason = p.Reason };

    public static LibrariannPlusAuditMatchDetailsDto? From(AuditLogMatchDontMatchParamsDto? p) =>
        p is null ? null : new LibrariannPlusAuditMatchDetailsDto { DontMatch = p.DontMatch };

    public static LibrariannPlusAuditMatchDetailsDto? From(AuditLogMatchProviderOverrideParamsDto? p) =>
        p is null ? null : new LibrariannPlusAuditMatchDetailsDto
        {
            PreviousProvider = p.PreviousProvider, NewProvider = p.NewProvider, IsProviderOverride = p.IsOverride
        };
}

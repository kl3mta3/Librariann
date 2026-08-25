using Librariann.Models.DTOs.LibrariannPlus.Audit;

namespace Librariann.Models.DTOs.LibrariannPlus;
#nullable enable

/// <summary>
/// Sync-specific context surfaced on a Librariann+ audit entry.
/// Projected from AuditLogSync*ParamsDtos based on EventType.
/// </summary>
public sealed record LibrariannPlusAuditSyncDetailsDto
{
    // CollectionSynced
    public string? CollectionName { get; init; }
    public string? StackId { get; init; }
    public int? ItemCount { get; init; }
    public int? MissingCount { get; init; }
    public string? CollectionUrl { get; init; }

    // CollectionItemAdded
    public string? SeriesName { get; init; }
    public int? SeriesId { get; init; }

    // SyncCompleted (WantToRead)
    public string? UserName { get; init; }
    public bool? HasMal { get; init; }
    public bool? HasAniList { get; init; }
    public int? SeriesMatched { get; init; }

    public static LibrariannPlusAuditSyncDetailsDto? From(AuditLogCollectionSyncedParamsDto? p) =>
        p is null ? null : new LibrariannPlusAuditSyncDetailsDto { CollectionName = p.CollectionName, StackId = p.StackId,
            ItemCount = p.ItemCount, MissingCount = p.MissingCount, CollectionUrl = p.Url };

    public static LibrariannPlusAuditSyncDetailsDto? From(AuditLogCollectionItemParamsDto? p) =>
        p is null ? null : new LibrariannPlusAuditSyncDetailsDto { CollectionName = p.CollectionName,
            SeriesName = p.SeriesName, SeriesId = p.SeriesId, CollectionUrl = p.Url };

    public static LibrariannPlusAuditSyncDetailsDto? From(AuditLogWantToReadSyncCompletedParamsDto? p) =>
        p is null ? null : new LibrariannPlusAuditSyncDetailsDto { UserName = p.UserName, HasMal = p.HasMal,
            HasAniList = p.HasAniList, SeriesMatched = p.SeriesMatched };

    public static LibrariannPlusAuditSyncDetailsDto? From(AuditLogCollectionStartedParamsDto? p) =>
        p is null ? null : new LibrariannPlusAuditSyncDetailsDto { CollectionName = p.CollectionName,
            StackId = p.StackId, ItemCount = p.TotalItems };

    public static LibrariannPlusAuditSyncDetailsDto? From(AuditLogCollectionFailedParamsDto? p) =>
        p is null ? null : new LibrariannPlusAuditSyncDetailsDto { CollectionName = p.CollectionName };

    public static LibrariannPlusAuditSyncDetailsDto? From(AuditLogWantToReadSyncParamsDto? p) =>
        p is null ? null : new LibrariannPlusAuditSyncDetailsDto { UserName = p.UserName, HasMal = p.HasMal,
            HasAniList = p.HasAniList };
}

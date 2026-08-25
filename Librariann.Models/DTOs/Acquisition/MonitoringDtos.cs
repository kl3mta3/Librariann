using System;
using System.ComponentModel.DataAnnotations;
using Librariann.Models.DTOs.Metadata;
using Librariann.Models.Entities.Acquisition;

namespace Librariann.Models.DTOs.Acquisition;

public sealed record MonitoringTargetDto(
    int Id,
    int CreatedByUserId,
    MonitoringTargetKind Kind,
    LibrariannMediaType MediaType,
    int? LibrarySeriesId,
    int QualityProfileId,
    string Title,
    string Author,
    string Isbn,
    string Language,
    string ExternalProviderKey,
    string ExternalItemId,
    bool MonitorMissing,
    bool MonitorFuture,
    bool AutomaticGrabEnabled,
    int? DownloadClientId,
    int MinimumAutomaticGrabScore,
    DateTime? LastAutomaticGrabAtUtc,
    bool IsEnabled,
    int SearchIntervalHours,
    DateTime? LastSearchAtUtc,
    DateTime NextSearchAtUtc,
    string LastSearchSummary,
    DateTime? LastCatalogSyncAtUtc,
    string CatalogSummary,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record UpsertMonitoringTargetRequest
{
    public int Id { get; init; }
    [EnumDataType(typeof(MonitoringTargetKind))] public MonitoringTargetKind Kind { get; init; }
    [EnumDataType(typeof(LibrariannMediaType))] public LibrariannMediaType MediaType { get; init; }
    [Range(1, int.MaxValue)] public int? LibrarySeriesId { get; init; }
    [Range(1, int.MaxValue)] public int QualityProfileId { get; init; }
    [Required, StringLength(512)] public string Title { get; init; } = string.Empty;
    [StringLength(256)] public string Author { get; init; } = string.Empty;
    [StringLength(32)] public string Isbn { get; init; } = string.Empty;
    [Required, StringLength(32)] public string Language { get; init; } = "English";
    [StringLength(100)] public string ExternalProviderKey { get; init; } = string.Empty;
    [StringLength(512)] public string ExternalItemId { get; init; } = string.Empty;
    public bool MonitorMissing { get; init; } = true;
    public bool MonitorFuture { get; init; } = true;
    public bool AutomaticGrabEnabled { get; init; }
    [Range(1, int.MaxValue)] public int? DownloadClientId { get; init; }
    [Range(0, 500)] public int MinimumAutomaticGrabScore { get; init; } = 90;
    public bool IsEnabled { get; init; } = true;
    [Range(1, 24 * 30)] public int SearchIntervalHours { get; init; } = 24;
}

public sealed record MonitoringSearchRunDto(
    int Id,
    int MonitoringTargetId,
    int? WantedItemId,
    MonitoringSearchStatus Status,
    string Query,
    int ResultCount,
    int ApprovedCount,
    string BestReleaseTitle,
    int? BestReleaseScore,
    string Summary,
    bool WasGrabbed,
    string GrabSummary,
    string DecisionSnapshotJson,
    DateTime StartedAtUtc,
    DateTime CompletedAtUtc);

public sealed record WantedItemDto(
    int Id,
    int MonitoringTargetId,
    string ProviderKey,
    string ExternalItemId,
    string Title,
    string Author,
    string Series,
    string Sequence,
    int? PublicationYear,
    WantedItemStatus Status,
    int? LibrarySeriesId,
    DateTime FirstSeenAtUtc,
    DateTime LastSeenAtUtc,
    DateTime? LastSearchAtUtc,
    DateTime NextSearchAtUtc,
    string LastSearchSummary);

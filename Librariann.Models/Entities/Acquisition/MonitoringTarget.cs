using System;
using Librariann.Models.DTOs.Metadata;

namespace Librariann.Models.Entities.Acquisition;

public enum MonitoringTargetKind
{
    Book = 1,
    Series = 2,
    Author = 3,
}

/// <summary>
/// A library-centric acquisition intent. This records what Librariann should look for; it never stores a
/// provider download URL or credentials.
/// </summary>
public sealed class MonitoringTarget
{
    public int Id { get; set; }
    public int CreatedByUserId { get; set; }
    public MonitoringTargetKind Kind { get; set; }
    public LibrariannMediaType MediaType { get; set; }
    public int? LibrarySeriesId { get; set; }
    public int QualityProfileId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Isbn { get; set; } = string.Empty;
    public string Language { get; set; } = "English";
    public string ExternalProviderKey { get; set; } = string.Empty;
    public string ExternalItemId { get; set; } = string.Empty;
    public bool MonitorMissing { get; set; } = true;
    public bool MonitorFuture { get; set; } = true;
    public bool AutomaticGrabEnabled { get; set; }
    public int? DownloadClientId { get; set; }
    public int MinimumAutomaticGrabScore { get; set; } = 90;
    public DateTime? LastAutomaticGrabAtUtc { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int SearchIntervalHours { get; set; } = 24;
    public DateTime? LastSearchAtUtc { get; set; }
    public DateTime NextSearchAtUtc { get; set; } = DateTime.UtcNow;
    public string LastSearchSummary { get; set; } = string.Empty;
    public DateTime? LastCatalogSyncAtUtc { get; set; }
    public string CatalogSummary { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

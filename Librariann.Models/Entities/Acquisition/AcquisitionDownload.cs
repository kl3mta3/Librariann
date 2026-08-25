using System;
using Librariann.Models.DTOs.Acquisition;

namespace Librariann.Models.Entities.Acquisition;

public enum AcquisitionDownloadStatus
{
    Queued = 1,
    Downloading = 2,
    Completed = 3,
    ImportPending = 4,
    Importing = 5,
    NeedsManualMatch = 6,
    Imported = 7,
    Failed = 8,
    Removed = 9,
}

/// <summary>
/// Durable bridge between a release grab, its external download-client job, and the import pipeline.
/// </summary>
public sealed class AcquisitionDownload
{
    public int Id { get; set; }
    public int RequestedByUserId { get; set; }
    public int IntegrationProviderConfigurationId { get; set; }
    public int? MonitoringTargetId { get; set; }
    public int? WantedItemId { get; set; }
    public string DownloadClientName { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public string ReleaseTitle { get; set; } = string.Empty;
    public AcquisitionMediaFormat Format { get; set; }
    public DownloadProtocol Protocol { get; set; }
    public AcquisitionDownloadStatus Status { get; set; } = AcquisitionDownloadStatus.Queued;
    public double Progress { get; set; }
    public string OutputPath { get; set; } = string.Empty;
    public string ImportedPath { get; set; } = string.Empty;
    /// <summary>
    /// Series that ultimately owns the imported file. This is populated immediately for monitored imports and
    /// reconciled after the library scan for manually matched imports.
    /// </summary>
    public int? ImportedSeriesId { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public int ConsecutivePollFailures { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastPolledAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? ImportedAtUtc { get; set; }
    /// <summary>
    /// When Librariann queued the provenance-safe metadata refresh that follows the import scan.
    /// External provider results are deliberately not auto-applied; they remain reviewable.
    /// </summary>
    public DateTime? MetadataRefreshQueuedAtUtc { get; set; }
    /// <summary>
    /// When the external download-client job was removed. Imported records keep their Imported status so cleanup
    /// does not erase library history.
    /// </summary>
    public DateTime? ExternalRemovedAtUtc { get; set; }
}

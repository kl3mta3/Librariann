using System;

namespace Librariann.Models.Entities.Acquisition;

public enum WantedItemStatus
{
    Missing = 1,
    Owned = 2,
    Downloading = 3,
    Ignored = 4,
}

/// <summary>
/// A catalog item discovered beneath an author/series/book monitoring intent. Ownership is reconciled against
/// local library items; provider identity keeps refreshes stable when titles change.
/// </summary>
public sealed class WantedItem
{
    public int Id { get; set; }
    public int MonitoringTargetId { get; set; }
    public string ProviderKey { get; set; } = string.Empty;
    public string ExternalItemId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Series { get; set; } = string.Empty;
    public string Sequence { get; set; } = string.Empty;
    public int? PublicationYear { get; set; }
    public WantedItemStatus Status { get; set; } = WantedItemStatus.Missing;
    public int? LibrarySeriesId { get; set; }
    public DateTime FirstSeenAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastSearchAtUtc { get; set; }
    public DateTime NextSearchAtUtc { get; set; } = DateTime.UtcNow;
    public string LastSearchSummary { get; set; } = string.Empty;
}

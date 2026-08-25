using System;

namespace Librariann.Models.Entities.Acquisition;

public enum MonitoringSearchStatus
{
    CandidateFound = 1,
    NoApprovedCandidate = 2,
    ProviderFailure = 3,
}

/// <summary>
/// Durable, secret-free audit record for an automatic monitoring search.
/// </summary>
public sealed class MonitoringSearchRun
{
    public int Id { get; set; }
    public int MonitoringTargetId { get; set; }
    public int? WantedItemId { get; set; }
    public MonitoringSearchStatus Status { get; set; }
    public string Query { get; set; } = string.Empty;
    public int ResultCount { get; set; }
    public int ApprovedCount { get; set; }
    public string BestReleaseTitle { get; set; } = string.Empty;
    public int? BestReleaseScore { get; set; }
    public string Summary { get; set; } = string.Empty;
    public bool WasGrabbed { get; set; }
    public string GrabSummary { get; set; } = string.Empty;
    public string DecisionSnapshotJson { get; set; } = "[]";
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime CompletedAtUtc { get; set; } = DateTime.UtcNow;
}

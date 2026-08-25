namespace Librariann.Models.DTOs.Dashboard;

/// <summary>
/// A missing catalog entry tied to a monitored series the current user may access. It deliberately contains no
/// indexer, download-client, provider-secret, or filesystem information.
/// </summary>
public sealed record MissingSeriesItemDto(
    int WantedItemId,
    int MonitoringTargetId,
    int SourceSeriesId,
    int LibraryId,
    string SourceSeriesTitle,
    string MissingTitle,
    string Author,
    string Series,
    string Sequence,
    int? PublicationYear);

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Librariann.Models.Entities.Acquisition;

namespace Librariann.Models.DTOs.Acquisition;

public enum AcquisitionMediaFormat
{
    Unknown = 0,
    Epub = 1,
    Azw3 = 2,
    Mobi = 3,
    Pdf = 4,
    Cbz = 10,
    Cbr = 11,
    Cb7 = 12,
}

public enum IndexerProtocol
{
    Torznab = 1,
    Newznab = 2,
}

public enum DownloadProtocol
{
    Torrent = 1,
    Usenet = 2,
}

public enum DownloadClientKind
{
    QBittorrent = 1,
    Sabnzbd = 2,
    UTorrent = 3,
    Transmission = 10,
    Deluge = 11,
    NzbGet = 12,
    RTorrent = 13,
}

public enum ReleaseRejectionCode
{
    WrongLanguage = 1,
    UnwantedFormat = 2,
    BelowMinimumSize = 3,
    AboveMaximumSize = 4,
    TitleMismatch = 5,
    AuthorMismatch = 6,
    WrongEdition = 7,
    AlreadyOwned = 8,
    NotAnUpgrade = 9,
    MissingDownloadUrl = 10,
}

public sealed record ProviderTestResult(bool IsSuccess, string Message, TimeSpan Elapsed);

public sealed record IndexerCapabilities(
    bool SupportsSearch,
    bool SupportsRss,
    IReadOnlyCollection<DownloadProtocol> Protocols,
    IReadOnlyCollection<int> Categories);

/// <summary>
/// The provider-neutral result used by search, scoring, grabbing, history, and the UI.
/// Provider-specific response objects must not escape their adapter.
/// </summary>
public sealed record ReleaseCandidate
{
    public string ProviderKey { get; init; } = string.Empty;
    public string ProviderReleaseId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string Edition { get; init; } = string.Empty;
    public string Language { get; init; } = string.Empty;
    public AcquisitionMediaFormat Format { get; init; }
    public DownloadProtocol Protocol { get; init; }
    public long SizeBytes { get; init; }
    public DateTimeOffset PublishedAt { get; init; }
    public int? Seeders { get; init; }
    public int? Peers { get; init; }
    public bool IsRetail { get; init; }
    [JsonIgnore]
    public Uri? DownloadUri { get; init; }
    public Uri? DetailsUri { get; init; }
    [JsonIgnore]
    public IReadOnlyDictionary<string, string> ProviderData { get; init; } = new Dictionary<string, string>();
}

public sealed record IndexerSearchRequest
{
    public string Query { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string Series { get; init; } = string.Empty;
    public string Isbn { get; init; } = string.Empty;
    public int? Volume { get; init; }
    public int? Issue { get; init; }
    public IReadOnlyCollection<int> Categories { get; init; } = Array.Empty<int>();
}

public sealed record ReleaseEvaluationContext
{
    public string ExpectedTitle { get; init; } = string.Empty;
    public string ExpectedAuthor { get; init; } = string.Empty;
    public string ExpectedEdition { get; init; } = string.Empty;
    public IReadOnlyCollection<string> AllowedLanguages { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<AcquisitionMediaFormat, int> FormatScores { get; init; } = new Dictionary<AcquisitionMediaFormat, int>();
    public AcquisitionMediaFormat? OwnedFormat { get; init; }
    public long? MinimumSizeBytes { get; init; }
    public long? MaximumSizeBytes { get; init; }
    public bool PreferRetail { get; init; } = true;
    public bool UpgradeAllowed { get; init; } = true;
    public int CutoffScore { get; init; }
}

public sealed record ReleaseRejection(ReleaseRejectionCode Code, string Message);

public sealed record ReleaseDecision(
    ReleaseCandidate Release,
    int Score,
    IReadOnlyCollection<ReleaseRejection> Rejections)
{
    public bool IsApproved => Rejections.Count == 0;
    public string? GrabToken { get; init; }
}

public sealed record InteractiveSearchRequest
{
    [Range(1, int.MaxValue)]
    public int QualityProfileId { get; init; }

    [Required]
    public IndexerSearchRequest Search { get; init; } = new();

    [Required]
    public ReleaseEvaluationContext Evaluation { get; init; } = new();
}

public sealed record ProviderSearchFailure(string ProviderKey, string ProviderName, string Message);

public sealed record InteractiveSearchResponse(
    IReadOnlyCollection<ReleaseDecision> Results,
    IReadOnlyCollection<ProviderSearchFailure> ProviderFailures);

public sealed record DownloadGrabRequest(
    Uri DownloadUri,
    string ReleaseTitle,
    string Category,
    IReadOnlyCollection<string> Tags);

public sealed record GrabReleaseRequest
{
    [Required, StringLength(128, MinimumLength = 32)]
    public string GrabToken { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int DownloadClientId { get; init; }
}

public sealed record GrabReleaseResponse(string ExternalId, string DownloadClientName, string ReleaseTitle);

public sealed record DownloadClientOption(int Id, string Name, DownloadClientKind Kind, DownloadProtocol Protocol);

public sealed record AcquisitionDownloadDto(
    int Id,
    int RequestedByUserId,
    int DownloadClientId,
    string DownloadClientName,
    string ExternalId,
    string ReleaseTitle,
    AcquisitionMediaFormat Format,
    DownloadProtocol Protocol,
    AcquisitionDownloadStatus Status,
    double Progress,
    string OutputPath,
    string ImportedPath,
    int? ImportedSeriesId,
    string ErrorMessage,
    DateTime CreatedAtUtc,
    DateTime? LastPolledAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? ImportedAtUtc,
    DateTime? MetadataRefreshQueuedAtUtc,
    DateTime? ExternalRemovedAtUtc);

public sealed record RemoveAcquisitionDownloadRequest(bool DeleteData = false);

public sealed record ImportCandidate(string FileName, string RelativePath, AcquisitionMediaFormat Format, long SizeBytes);

public sealed record ImportAnalysisResult(
    int DownloadId,
    IReadOnlyCollection<ImportCandidate> Candidates,
    bool NeedsManualMatch,
    string Message);

public sealed record ImportDestinationOption(
    int LibraryId,
    string LibraryName,
    int FolderId,
    string FolderPath,
    IReadOnlyCollection<AcquisitionMediaFormat> SupportedFormats);

public sealed record CommitImportRequest
{
    [Range(1, int.MaxValue)]
    public int DownloadId { get; init; }

    [Range(1, int.MaxValue)]
    public int LibraryId { get; init; }

    [Range(1, int.MaxValue)]
    public int FolderId { get; init; }

    [Required, StringLength(2048)]
    public string CandidateRelativePath { get; init; } = string.Empty;

    [StringLength(1024)]
    public string DestinationSubdirectory { get; init; } = string.Empty;

    [StringLength(255)]
    public string DestinationBaseName { get; init; } = string.Empty;
}

public sealed record CommitImportResult(int DownloadId, string FileName, int LibraryId, string LibraryName);

public sealed record DownloadClientItem(
    string ExternalId,
    string Name,
    string Status,
    double Progress,
    string OutputPath,
    bool IsComplete,
    string? ErrorMessage);

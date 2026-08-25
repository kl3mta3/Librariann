using System;
using System.Collections.Generic;
using Librariann.Models.Entities.Enums;

namespace Librariann.Models.DTOs.Client;

public sealed record ClientCapabilitiesDto(
    bool CanRead,
    bool CanDownloadFiles,
    bool CanSearchIndexers,
    bool CanGrabReleases,
    bool CanManageMetadata,
    bool CanManageLibraries,
    bool CanManageAcquisition);

public sealed record ClientLibraryDto(
    string Id,
    int LibraryId,
    string Name,
    LibraryType Type,
    string CoverApiPath,
    string BrowsePath);

public sealed record ClientMediaItemDto(
    string Id,
    int SeriesId,
    string Title,
    int LibraryId,
    string LibraryName,
    LibraryType LibraryType,
    int Pages,
    int PagesRead,
    decimal Progress,
    string CoverApiPath,
    string DetailPath,
    string Reason);

public sealed record ClientRailDto(
    string Id,
    string Title,
    string Reason,
    IReadOnlyCollection<ClientMediaItemDto> Items);

public sealed record ClientMissingItemDto(
    string Id,
    int WantedItemId,
    int MonitoringTargetId,
    int SourceSeriesId,
    int LibraryId,
    string SourceSeriesTitle,
    string MissingTitle,
    string Author,
    string Series,
    string Sequence,
    int? PublicationYear,
    string CoverApiPath,
    string SourceSeriesPath,
    string Reason);

/// <summary>
/// Stable, presentation-neutral home payload for embedded and external clients. It intentionally excludes local
/// filesystem paths and administrative configuration.
/// </summary>
public sealed record ClientHomeDto(
    string ApiVersion,
    string Product,
    string ServerId,
    string EmbedPath,
    ClientCapabilitiesDto Capabilities,
    IReadOnlyCollection<ClientLibraryDto> Libraries,
    IReadOnlyCollection<ClientRailDto> Rails,
    IReadOnlyCollection<ClientMissingItemDto> MissingItems);

public sealed record ClientOfflinePartDto(
    string Id,
    int ChapterId,
    int VolumeId,
    string Title,
    float SortOrder,
    MangaFormat Format,
    int Pages,
    int PagesRead,
    long Bytes,
    DateTime LastModifiedUtc,
    string CoverApiPath,
    string ReaderPath,
    string DownloadApiPath);

/// <summary>
/// Describes authorized content that a future offline-capable client may download. This payload contains no local
/// filesystem paths. Download URLs remain authenticated and permission checked by the normal download controller.
/// </summary>
public sealed record ClientOfflineManifestDto(
    string ApiVersion,
    string ServerId,
    string Id,
    int SeriesId,
    string Title,
    DateTime RevisionUtc,
    long Bytes,
    string ProgressReadApiPath,
    string ProgressWriteApiPath,
    IReadOnlyCollection<ClientOfflinePartDto> Parts);

using System.Collections.Generic;

namespace Librariann.Models.DTOs.LibrariannPlus.Audit;
#nullable enable

public sealed record AuditLogMetadataChangesParamsDto
{
    public IList<MetadataFieldChangeDto> Changes { get; init; } = [];
}

public sealed record AuditLogChapterCoverParamsDto
{
    public string IssueNumber { get; init; } = string.Empty;
    public string CoverUrl { get; init; } = string.Empty;
}

public sealed record AuditLogSeriesCoverParamsDto
{
    public string SeriesName { get; init; } = string.Empty;
    public string CoverUrl { get; init; } = string.Empty;
}

public sealed record AuditLogVolumeCoverParamsDto
{
    public string VolumeNumber { get; init; } = string.Empty;
    public string CoverUrl { get; init; } = string.Empty;
}

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Librariann.Models.DTOs.Metadata;

namespace Librariann.Models.DTOs.Acquisition;

public sealed record QualityProfileDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public LibrariannMediaType MediaType { get; init; }
    public string Language { get; init; } = string.Empty;
    public bool UpgradeAllowed { get; init; }
    public bool PreferRetail { get; init; }
    public AcquisitionMediaFormat CutoffFormat { get; init; }
    public long? MinimumSizeBytes { get; init; }
    public long? MaximumSizeBytes { get; init; }
    public IReadOnlyDictionary<AcquisitionMediaFormat, int> FormatScores { get; init; } =
        new Dictionary<AcquisitionMediaFormat, int>();
}

public sealed record UpsertQualityProfileRequest
{
    public int Id { get; init; }
    [Required, StringLength(100)] public string Name { get; init; } = string.Empty;
    [EnumDataType(typeof(LibrariannMediaType))] public LibrariannMediaType MediaType { get; init; }
    [Required, StringLength(32)] public string Language { get; init; } = "English";
    public bool UpgradeAllowed { get; init; } = true;
    public bool PreferRetail { get; init; } = true;
    [EnumDataType(typeof(AcquisitionMediaFormat))] public AcquisitionMediaFormat CutoffFormat { get; init; }
    [Range(0, long.MaxValue)] public long? MinimumSizeBytes { get; init; }
    [Range(0, long.MaxValue)] public long? MaximumSizeBytes { get; init; }
    [Required] public Dictionary<AcquisitionMediaFormat, int> FormatScores { get; init; } = [];
}

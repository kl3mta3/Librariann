using System;
using System.Collections.Generic;
using Librariann.Models.DTOs.Acquisition;
using Librariann.Models.DTOs.Metadata;

namespace Librariann.Models.Entities.Acquisition;

public sealed class QualityProfile
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public LibrariannMediaType MediaType { get; set; }
    public string Language { get; set; } = "English";
    public bool UpgradeAllowed { get; set; } = true;
    public bool PreferRetail { get; set; } = true;
    public AcquisitionMediaFormat CutoffFormat { get; set; }
    public long? MinimumSizeBytes { get; set; }
    public long? MaximumSizeBytes { get; set; }
    public Dictionary<AcquisitionMediaFormat, int> FormatScores { get; set; } = [];
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

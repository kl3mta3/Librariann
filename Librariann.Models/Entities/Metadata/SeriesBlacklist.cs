using System;

namespace Librariann.Models.Entities.Metadata;

/// <summary>
/// A blacklist of Series for Librariann+
/// </summary>
[Obsolete("Librariann v0.8.5 moved the implementation to Series.IsBlacklisted")]
public class SeriesBlacklist
{
    public int Id { get; set; }
    public DateTime LastChecked { get; set; } = DateTime.UtcNow;

    public int SeriesId { get; set; }
    public Series Series { get; set; }
}

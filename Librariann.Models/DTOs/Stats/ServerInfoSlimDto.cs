using System;

namespace Librariann.Models.DTOs.Stats;
#nullable enable

/// <summary>
/// This is just for the Server tab on UI
/// </summary>
public sealed record ServerInfoSlimDto
{
    /// <summary>
    /// Unique Id that represents a unique install
    /// </summary>
    public required string InstallId { get; set; }
    /// <summary>
    /// If the Librariann install is using Docker
    /// </summary>
    public bool IsDocker { get; set; }
    /// <summary>
    /// Version of Librariann
    /// </summary>
    public required string LibrariannVersion { get; set; }
    /// <summary>
    /// The Date Librariann was first installed
    /// </summary>
    public DateTime? FirstInstallDate { get; set; }
    /// <summary>
    /// The Version of Librariann on the first run
    /// </summary>
    public string? FirstInstallVersion { get; set; }

}

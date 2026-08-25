namespace Librariann.Models.DTOs.Settings;

/// <summary>
/// Progress of an in-flight (or just-finished) ffmpeg download/install - backs the Install button's
/// progress display in Settings -> Media, next to the ffmpeg path field. Polled by the frontend while
/// InProgress is true. Same shape as KokoroInstallStatusDto, deliberately - same UI pattern, different
/// download.
/// </summary>
public sealed record FfmpegInstallStatusDto
{
    public bool InProgress { get; set; }
    public long BytesDownloaded { get; set; }
    public long TotalBytes { get; set; }
    /// <summary>Set once an install attempt finishes - true on success, false on failure. Null before any
    /// install has ever been attempted this server session.</summary>
    public bool? Success { get; set; }
    public string? Error { get; set; }
}

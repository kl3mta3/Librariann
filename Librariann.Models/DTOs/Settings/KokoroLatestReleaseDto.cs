namespace Librariann.Models.DTOs.Settings;

/// <summary>
/// Latest GitHub release info for github.com/kl3mta3/Librariann-Kokoro-Server - backs the "Check for Updates"
/// button in Settings -> Media. Informational only for now: this does not download/install anything, it just
/// tells the admin what the latest available version is and links to it. See the "Auto-install / process
/// management" section of docs/kokoro-tts-integration.md for what's deliberately not built yet.
/// </summary>
public sealed record KokoroLatestReleaseDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public string? TagName { get; set; }
    public string? Name { get; set; }
    public string? HtmlUrl { get; set; }
    public string? PublishedAtUtc { get; set; }

    /// <summary>Name of the release's install .zip asset, e.g. "Librariann-Kokoro-Server.zip" - null if the
    /// release has no zip asset at all (nothing installable).</summary>
    public string? AssetName { get; set; }
    public string? AssetDownloadUrl { get; set; }
    public long AssetSizeBytes { get; set; }
}

using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs.Settings;

namespace Librariann.API.Services;

/// <summary>
/// Downloads a static ffmpeg build (from github.com/BtbN/FFmpeg-Builds, an already-established provider
/// of pre-built ffmpeg binaries with no separate account/API key needed) and extracts it to a default
/// install folder, then points <see cref="Librariann.Models.Entities.Enums.ServerSettingKey.FfmpegPath"/>
/// at the extracted binary - the "Install ffmpeg" button in Settings -> Media, for admins who don't
/// already have ffmpeg on their system PATH. Scanning for an existing on-PATH ffmpeg remains the other,
/// unchanged option (users can still just type "ffmpeg" and let PATH resolve it) - this is purely an
/// additional way to get a working ffmpeg without leaving Librariann.
/// </summary>
public interface IFfmpegInstallService
{
    /// <summary>Current progress of an in-flight or just-finished install, for the frontend to poll.</summary>
    FfmpegInstallStatusDto GetStatus();

    /// <summary>
    /// Kicks off the download+extract in the background and returns immediately - the frontend polls
    /// GetStatus() for progress. No-ops (returns the current status) if an install is already in progress.
    /// On success, also syncs the new path to a managed Kokoro install if one exists and
    /// KokoroSyncFfmpegPath is enabled - same as any other FfmpegPath change.
    /// </summary>
    FfmpegInstallStatusDto StartInstall();
}

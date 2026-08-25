using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs.Settings;

namespace Librariann.API.Services;

/// <summary>
/// Downloads the latest github.com/kl3mta3/Librariann-Kokoro-Server release .zip and extracts it to a default
/// install folder, then points <see cref="Librariann.Models.Entities.Enums.ServerSettingKey.KokoroExecutablePath"/>
/// at it - the "Install"/"Download" button in Settings -> Media for when nothing is installed yet. This is the
/// one-click counterpart to IKokoroProcessService (which only manages a process that's already on disk).
/// </summary>
public interface IKokoroInstallService
{
    /// <summary>Current progress of an in-flight or just-finished install, for the frontend to poll.</summary>
    KokoroInstallStatusDto GetStatus();

    /// <summary>
    /// Kicks off the download+extract in the background and returns immediately - the frontend polls
    /// GetStatus() for progress. No-ops (returns the current status) if an install is already in progress.
    /// </summary>
    KokoroInstallStatusDto StartInstall();
}

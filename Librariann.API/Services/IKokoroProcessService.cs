using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs.Settings;

namespace Librariann.API.Services;

/// <summary>
/// Starts, stops, and reports on a Librariann-Kokoro-Server process Librariann itself launched from a local
/// install folder (<see cref="Librariann.Models.Entities.Enums.ServerSettingKey.KokoroExecutablePath"/>).
/// Deliberately narrow in scope: this does NOT download or install anything (that's a separate, not-yet-built
/// phase - see docs/kokoro-tts-integration.md), and it never touches a Kokoro server the admin runs/manages
/// themselves - only a process this service's own StartAsync() call actually spawned. That distinction matters
/// because a "Stop" button controlling a process the admin didn't ask Librariann to manage would be surprising
/// and potentially destructive.
/// </summary>
public interface IKokoroProcessService
{
    /// <summary>Current state of the process Librariann itself started, if any - never inspects/reports on an
    /// externally-run Kokoro server, even if KokoroEndpointUrl happens to point at one. Async because IsInstalled
    /// requires reading the configured install folder from settings.</summary>
    Task<KokoroProcessStatusDto> GetStatusAsync(CancellationToken ct = default);

    /// <summary>
    /// Launches LKS.Server(.exe) from the configured KokoroExecutablePath folder, passing GPU/URL settings as
    /// environment variable overrides (Kokoro__UseGpu, ASPNETCORE_URLS) rather than editing the install's own
    /// appsettings.json. No-ops (returns the existing status) if Librariann already has one running.
    /// </summary>
    Task<KokoroProcessStatusDto> StartAsync(CancellationToken ct = default);

    /// <summary>Stops the managed process, if Librariann has one running. No-ops otherwise.</summary>
    Task<KokoroProcessStatusDto> StopAsync(CancellationToken ct = default);

    /// <summary>
    /// Writes Librariann's own FfmpegPath setting into the installed LKS's appsettings.json (Ffmpeg:Path) -
    /// keeps the two in sync so the admin only ever manages one ffmpeg path, not two. No-ops if nothing is
    /// installed at KokoroExecutablePath. Only touches the config file on disk - an already-*running* Kokoro
    /// process read its config at startup and won't pick this up until restarted (Stop then Start).
    /// </summary>
    Task SyncFfmpegPathAsync(CancellationToken ct = default);
}

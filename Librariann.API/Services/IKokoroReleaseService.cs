using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs.Settings;

namespace Librariann.API.Services;

/// <summary>
/// Checks github.com/kl3mta3/Librariann-Kokoro-Server's GitHub Releases for the latest published version -
/// backs the admin "Check for Updates" button in Settings -> Media. Informational only: this does not download
/// or install anything (see the "Auto-install / process management" section of docs/kokoro-tts-integration.md
/// for what's deliberately not built yet).
/// </summary>
public interface IKokoroReleaseService
{
    Task<KokoroLatestReleaseDto> GetLatestReleaseAsync(CancellationToken ct = default);
}

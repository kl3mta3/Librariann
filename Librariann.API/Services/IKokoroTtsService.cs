using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs.Reader;

namespace Librariann.API.Services;

/// <summary>
/// Forwards a text chunk to a self-hosted Kokoro TTS server (configured via
/// <see cref="Librariann.Models.Entities.Enums.ServerSettingKey.KokoroEndpointUrl"/>) and returns synthesized
/// audio. See docs/kokoro-tts-integration.md for the exact request/response contract a Kokoro server must
/// implement - it's the OpenAI-compatible `/v1/audio/speech` shape, which off-the-shelf Kokoro servers
/// (e.g. kokoro-fastapi) already speak with no custom glue needed.
/// </summary>
public interface IKokoroTtsService
{
    /// <summary>
    /// Returns null if Kokoro isn't configured (empty endpoint URL) - callers should treat that as "fall back
    /// to the browser's own TTS", not an error.
    /// </summary>
    Task<KokoroSynthesisResult?> SynthesizeAsync(string text, string? voiceId, double speed, CancellationToken ct = default);

    /// <summary>
    /// Returns null if Kokoro isn't configured. An empty list (as opposed to null) means Kokoro is configured
    /// but returned no voices, which is worth surfacing differently to the user.
    /// </summary>
    Task<string[]?> GetVoicesAsync(CancellationToken ct = default);

    /// <summary>
    /// Pings the configured Kokoro server's `/v1/status` endpoint - backs the admin "Check Status" button in
    /// Settings -> Media. Never throws: an unreachable/erroring server comes back as <c>IsReachable = false</c>,
    /// not an exception.
    /// </summary>
    Task<KokoroStatusDto> GetStatusAsync(CancellationToken ct = default);
}

public sealed record KokoroSynthesisResult(byte[] Audio, string ContentType);

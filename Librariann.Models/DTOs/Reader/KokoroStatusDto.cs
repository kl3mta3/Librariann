namespace Librariann.Models.DTOs.Reader;

/// <summary>
/// Result of pinging the configured Kokoro TTS server's `/v1/status` endpoint - backs the "Check Status" button
/// in Settings -> Media. See docs/kokoro-tts-integration.md for the contract.
/// </summary>
public sealed record KokoroStatusDto
{
    /// <summary>False if <see cref="Entities.Enums.ServerSettingKey.KokoroEndpointUrl"/> is empty - nothing was attempted.</summary>
    public bool IsConfigured { get; set; }

    /// <summary>True if the configured server responded. False if configured but unreachable/erroring.</summary>
    public bool IsReachable { get; set; }

    public string? ModelPrecision { get; set; }
    public bool? GpuActive { get; set; }
    public bool? GpuRequested { get; set; }
    public string? DefaultVoice { get; set; }
    public int? VoiceCount { get; set; }
    public string? Version { get; set; }
    public double? UptimeSeconds { get; set; }
}

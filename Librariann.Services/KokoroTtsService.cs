using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Database;
using Librariann.API.Services;
using Librariann.Models.DTOs.Reader;
using Librariann.Models.Entities.Enums;
using Microsoft.Extensions.Logging;

namespace Librariann.Services;

public class KokoroTtsService(IUnitOfWork unitOfWork, IHttpClientFactory httpClientFactory, ILogger<KokoroTtsService> logger)
    : IKokoroTtsService
{
    private const string DefaultVoice = "af_heart";

    // Matches OpenAI's /v1/audio/speech request shape - what off-the-shelf Kokoro servers (kokoro-fastapi etc.)
    // already implement. See docs/kokoro-tts-integration.md for the full contract.
    private sealed record SpeechRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("input")] string Input,
        [property: JsonPropertyName("voice")] string Voice,
        [property: JsonPropertyName("response_format")] string ResponseFormat,
        [property: JsonPropertyName("speed")] double Speed);

    private sealed record VoicesResponse([property: JsonPropertyName("voices")] string[] Voices);

    // Matches LKS's /v1/status shape (an LKS-specific extra, not part of the required contract - see
    // docs/kokoro-tts-integration.md). A server that doesn't implement it just makes GetStatusAsync report
    // unreachable, same as any other failure.
    private sealed record StatusResponse(
        [property: JsonPropertyName("model_precision")] string? ModelPrecision,
        [property: JsonPropertyName("gpu_active")] bool? GpuActive,
        [property: JsonPropertyName("gpu_requested")] bool? GpuRequested,
        [property: JsonPropertyName("default_voice")] string? DefaultVoice,
        [property: JsonPropertyName("voice_count")] int? VoiceCount,
        [property: JsonPropertyName("version")] string? Version,
        [property: JsonPropertyName("uptime_seconds")] double? UptimeSeconds);

    public async Task<KokoroSynthesisResult?> SynthesizeAsync(string text, string? voiceId, double speed, CancellationToken ct = default)
    {
        var baseUrl = await GetEndpointUrlAsync(ct);
        if (baseUrl == null) return null;

        using var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);

        var request = new SpeechRequest("kokoro", text, string.IsNullOrWhiteSpace(voiceId) ? DefaultVoice : voiceId,
            "mp3", speed <= 0 ? 1.0 : speed);

        try
        {
            using var response = await client.PostAsJsonAsync("v1/audio/speech", request, ct);
            response.EnsureSuccessStatusCode();
            var audio = await response.Content.ReadAsByteArrayAsync(ct);
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "audio/mpeg";
            return new KokoroSynthesisResult(audio, contentType);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Kokoro TTS synthesis request to {BaseUrl} failed", baseUrl);
            return null;
        }
    }

    public async Task<string[]?> GetVoicesAsync(CancellationToken ct = default)
    {
        var baseUrl = await GetEndpointUrlAsync(ct);
        if (baseUrl == null) return null;

        using var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(10);

        try
        {
            var result = await client.GetFromJsonAsync<VoicesResponse>("v1/audio/voices", ct);
            return result?.Voices ?? [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Kokoro TTS voice list request to {BaseUrl} failed", baseUrl);
            return [];
        }
    }

    public async Task<KokoroStatusDto> GetStatusAsync(CancellationToken ct = default)
    {
        var baseUrl = await GetEndpointUrlAsync(ct);
        if (baseUrl == null) return new KokoroStatusDto {IsConfigured = false, IsReachable = false};

        using var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(10);

        try
        {
            var status = await client.GetFromJsonAsync<StatusResponse>("v1/status", ct);
            if (status == null) return new KokoroStatusDto {IsConfigured = true, IsReachable = false};

            return new KokoroStatusDto
            {
                IsConfigured = true,
                IsReachable = true,
                ModelPrecision = status.ModelPrecision,
                GpuActive = status.GpuActive,
                GpuRequested = status.GpuRequested,
                DefaultVoice = status.DefaultVoice,
                VoiceCount = status.VoiceCount,
                Version = status.Version,
                UptimeSeconds = status.UptimeSeconds,
            };
        }
        catch (Exception ex)
        {
            // /v1/status is an LKS-specific extra (see docs/kokoro-tts-integration.md) - a Kokoro server that
            // doesn't implement it looks identical here to one that's actually down. Acceptable trade-off: LKS
            // is the server this button is meant for.
            logger.LogWarning(ex, "Kokoro TTS status request to {BaseUrl} failed", baseUrl);
            return new KokoroStatusDto {IsConfigured = true, IsReachable = false};
        }
    }

    private async Task<string?> GetEndpointUrlAsync(CancellationToken ct)
    {
        var url = (await unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.KokoroEndpointUrl)).Value;
        if (string.IsNullOrWhiteSpace(url)) return null;
        return url.EndsWith('/') ? url : url + "/";
    }
}

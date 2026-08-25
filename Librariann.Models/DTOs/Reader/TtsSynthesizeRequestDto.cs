namespace Librariann.Models.DTOs.Reader;

/// <summary>
/// Body for <c>POST reader/tts/synthesize</c>. See docs/kokoro-tts-integration.md.
/// </summary>
public sealed record TtsSynthesizeRequestDto
{
    public required string Text { get; init; }
    public string? VoiceId { get; init; }
    public double Speed { get; init; } = 1.0;
}

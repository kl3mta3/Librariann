using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs.Reader;
using Librariann.Models.Entities.Enums;

namespace Librariann.API.Services;

public sealed record AudiobookProbeResult(double DurationSeconds, int Bitrate, IReadOnlyList<AudiobookChapterMarkerDto> ChapterMarkers);

/// <summary>
/// Reads metadata (duration, bitrate, embedded M4B chapter markers) from an audio file via ffprobe, and extracts
/// embedded cover art via ffmpeg. Audiobooks are streamed as their original file - this service never transcodes
/// or writes any audio output, only reads metadata and (optionally) a single embedded image.
/// </summary>
public interface IAudiobookMetadataService
{
    Task<AudiobookProbeResult> ProbeAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Extracts embedded cover art (M4B/MP3 attached picture) and writes a thumbnail via the same pipeline as
    /// every other format. Returns an empty string if the file has no embedded art or extraction fails.
    /// </summary>
    Task<string> GetCoverImageAsync(string filePath, string fileName, string outputDirectory, EncodeFormat encodeFormat, CoverImageSize size = CoverImageSize.Default, CancellationToken ct = default);
}

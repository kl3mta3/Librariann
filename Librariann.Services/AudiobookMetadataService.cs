using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Database;
using Librariann.API.Services;
using Librariann.Models.DTOs.Reader;
using Librariann.Models.Entities.Enums;
using Microsoft.Extensions.Logging;

namespace Librariann.Services;

/// <summary>
/// Reads audio metadata (duration, bitrate, embedded M4B chapter markers) via ffprobe. Never writes any audio
/// output - audiobooks are streamed as their original file, not transcoded. See
/// <see cref="IAudiobookMetadataService"/>.
/// </summary>
public class AudiobookMetadataService(ILogger<AudiobookMetadataService> logger, IUnitOfWork unitOfWork, IImageService imageService)
    : IAudiobookMetadataService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<AudiobookProbeResult> ProbeAsync(string filePath, CancellationToken ct = default)
    {
        var empty = new AudiobookProbeResult(0, 0, []);
        try
        {
            var ffmpegPath = (await unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.FfmpegPath, ct)).Value;
            var ffprobePath = ResolveFfprobePath(ffmpegPath);

            var json = await RunProcessAsync(ffprobePath,
                $"-i \"{filePath}\" -print_format json -show_format -show_chapters -loglevel error", ct);
            if (string.IsNullOrWhiteSpace(json)) return empty;

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            double duration = 0;
            var bitrate = 0;
            if (root.TryGetProperty("format", out var format))
            {
                if (format.TryGetProperty("duration", out var durationProp) &&
                    double.TryParse(durationProp.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedDuration))
                {
                    duration = parsedDuration;
                }
                if (format.TryGetProperty("bit_rate", out var bitrateProp) &&
                    int.TryParse(bitrateProp.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedBitrate))
                {
                    bitrate = parsedBitrate / 1000; // bps -> kbps
                }
            }

            var markers = new List<AudiobookChapterMarkerDto>();
            if (root.TryGetProperty("chapters", out var chapters) && chapters.ValueKind == JsonValueKind.Array)
            {
                foreach (var chapter in chapters.EnumerateArray())
                {
                    if (!chapter.TryGetProperty("start_time", out var startProp) ||
                        !chapter.TryGetProperty("end_time", out var endProp)) continue;
                    if (!double.TryParse(startProp.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var start)) continue;
                    if (!double.TryParse(endProp.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var end)) continue;

                    string? title = null;
                    if (chapter.TryGetProperty("tags", out var tags) && tags.TryGetProperty("title", out var titleProp))
                    {
                        title = titleProp.GetString();
                    }

                    markers.Add(new AudiobookChapterMarkerDto {Title = title, StartSeconds = start, EndSeconds = end});
                }
            }

            return new AudiobookProbeResult(duration, bitrate, markers);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[AudiobookMetadataService] Unable to probe audio metadata for {FilePath}", filePath);
            return empty;
        }
    }

    public async Task<string> GetCoverImageAsync(string filePath, string fileName, string outputDirectory,
        EncodeFormat encodeFormat, CoverImageSize size = CoverImageSize.Default, CancellationToken ct = default)
    {
        var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid()}.jpg");
        try
        {
            var ffmpegPath = (await unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.FfmpegPath, ct)).Value;
            await RunProcessAsync(ffmpegPath,
                $"-y -i \"{filePath}\" -an -vcodec copy \"{tempFile}\"", ct);

            if (!System.IO.File.Exists(tempFile) || new System.IO.FileInfo(tempFile).Length == 0) return string.Empty;

            return imageService.WriteCoverThumbnail(tempFile, fileName, outputDirectory, encodeFormat, size);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "[AudiobookMetadataService] No embedded cover art found for {FilePath}", filePath);
            return string.Empty;
        }
        finally
        {
            try { if (System.IO.File.Exists(tempFile)) System.IO.File.Delete(tempFile); } catch (Exception) { /* Swallow */ }
        }
    }

    /// <summary>
    /// ffprobe ships alongside ffmpeg in every standard distribution. Rather than adding a second settings key,
    /// derive its path from FfmpegPath: if that's a bare command name (PATH-resolved), assume ffprobe is too;
    /// if it's an absolute path, swap the executable name in the same directory.
    /// </summary>
    private static string ResolveFfprobePath(string ffmpegPath)
    {
        if (string.IsNullOrWhiteSpace(ffmpegPath) || ffmpegPath == "ffmpeg") return "ffprobe";

        var directory = System.IO.Path.GetDirectoryName(ffmpegPath);
        if (string.IsNullOrEmpty(directory)) return "ffprobe";

        var probeName = OperatingSystem.IsWindows() ? "ffprobe.exe" : "ffprobe";
        return System.IO.Path.Combine(directory, probeName);
    }

    private static async Task<string> RunProcessAsync(string fileName, string arguments, CancellationToken ct)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        process.Start();
        var stdOutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stdErrTask = process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        var stdOut = await stdOutTask;
        await stdErrTask;

        return process.ExitCode == 0 ? stdOut : string.Empty;
    }
}

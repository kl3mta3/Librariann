using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;
using Librariann.API.Database;
using Librariann.API.Services;
using Librariann.Common.Helpers;
using Librariann.Models.DTOs.Settings;
using Librariann.Models.Entities.Enums;
using Librariann.Services.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace Librariann.Services;

/// <inheritdoc cref="IFfmpegInstallService"/>
/// <remarks>
/// Singleton for the same reason as KokoroInstallService - the download runs detached from any one HTTP
/// request, so progress has to live somewhere that outlives it. Same IServiceScopeFactory pattern for
/// reaching the Scoped IUnitOfWork/IKokoroProcessService.
/// </remarks>
public sealed class FfmpegInstallService(
    IServiceScopeFactory scopeFactory,
    ILogger<FfmpegInstallService> logger) : IFfmpegInstallService
{
#pragma warning disable S1075
    private const string GithubLatestReleaseUrl = "https://api.github.com/repos/BtbN/FFmpeg-Builds/releases/latest";
#pragma warning restore S1075

    // Matches the stable, numbered release channel (e.g. ffmpeg-n9.0-latest-win64-lgpl-9.0.zip) rather
    // than the "master-latest" rolling-dev-build channel BtbN also publishes, and specifically the
    // static (non-"-shared-") LGPL build - LGPL keeps this redistributable alongside Librariann without
    // GPL obligations, and static avoids needing separate DLLs alongside the exe.
    private static readonly Regex WindowsAssetPattern = new(@"^ffmpeg-n[\d.]+-latest-win64-lgpl-[\d.]+\.zip$", RegexOptions.IgnoreCase);
    private static readonly Regex LinuxAssetPattern = new(@"^ffmpeg-n[\d.]+-latest-linux64-lgpl-[\d.]+\.tar\.xz$", RegexOptions.IgnoreCase);

    private readonly object _lock = new();
    private bool _inProgress;
    private long _bytesDownloaded;
    private long _totalBytes;
    private bool? _success;
    private string? _error;

    private sealed record GithubRelease(List<GithubAsset>? Assets);
    private sealed record GithubAsset(string Name, string Browser_Download_Url, long Size);

    public FfmpegInstallStatusDto GetStatus()
    {
        lock (_lock)
        {
            return new FfmpegInstallStatusDto
            {
                InProgress = _inProgress,
                BytesDownloaded = _bytesDownloaded,
                TotalBytes = _totalBytes,
                Success = _success,
                Error = _error,
            };
        }
    }

    public FfmpegInstallStatusDto StartInstall()
    {
        lock (_lock)
        {
            if (_inProgress) return GetStatus();
            _inProgress = true;
            _bytesDownloaded = 0;
            _totalBytes = 0;
            _success = null;
            _error = null;
        }

        // Detached on purpose - the controller action returns immediately and the frontend polls GetStatus().
        _ = Task.Run(RunInstallAsync);

        return GetStatus();
    }

    private async Task RunInstallAsync()
    {
        var ct = CancellationToken.None;
        string? tempArchivePath = null;
            // IDirectoryService is Scoped, so it is resolved per-operation from a scope rather than captured
            // in this Singleton - a captive dependency there fails DI validation on startup.
            using var scope = scopeFactory.CreateScope();
            var directoryService = scope.ServiceProvider.GetRequiredService<IDirectoryService>();

        try
        {
            var (assetName, downloadUrl, sizeBytes) = await GetAssetForCurrentOsAsync(ct);
            lock (_lock) { _totalBytes = sizeBytes; }

            var installFolder = Path.Combine(directoryService.ConfigDirectory, "ffmpeg");
            directoryService.ExistOrCreate(installFolder);

            tempArchivePath = Path.Combine(Path.GetTempPath(), $"ffmpeg-install-{Guid.NewGuid():N}-{assetName}");
            await DownloadWithProgressAsync(downloadUrl, tempArchivePath, ct);

            logger.LogInformation("Extracting ffmpeg install to {Folder}", installFolder);
            Extract(tempArchivePath, installFolder);

            var exeName = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
            // BtbN's builds nest the actual binary under a version-named folder's bin/ subdirectory
            // (e.g. ffmpeg-n9.0-latest-win64-lgpl-9.0/bin/ffmpeg.exe), not at the extract root - search
            // for it rather than assume a fixed depth, since that version-named folder changes every
            // release.
            var exePath = Directory.EnumerateFiles(installFolder, exeName, SearchOption.AllDirectories).FirstOrDefault()
                ?? throw new InvalidOperationException($"Extracted the download but couldn't find {exeName} anywhere inside it.");

            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var setting = await unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.FfmpegPath, ct);
            setting.Value = exePath;
            unitOfWork.SettingsRepository.Update(setting);
            await unitOfWork.CommitAsync(ct);

            // Keeps a managed Kokoro install's own ffmpeg path in sync, same as any other FfmpegPath
            // change (SettingsService.UpdateSettings) - honors the KokoroSyncFfmpegPath toggle and
            // no-ops if nothing is installed, so this is always safe to call unconditionally.
            var kokoroProcessService = scope.ServiceProvider.GetRequiredService<IKokoroProcessService>();
            await kokoroProcessService.SyncFfmpegPathAsync(ct);

            lock (_lock)
            {
                _success = true;
                _inProgress = false;
            }
            logger.LogInformation("ffmpeg install complete at {Path}", exePath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ffmpeg install failed");
            Fail(ex.Message);
        }
        finally
        {
            if (tempArchivePath != null && File.Exists(tempArchivePath))
            {
                try { File.Delete(tempArchivePath); } catch (IOException) { /* best-effort cleanup */ }
            }
        }
    }

    private async Task<(string AssetName, string DownloadUrl, long SizeBytes)> GetAssetForCurrentOsAsync(CancellationToken ct)
    {
        var pattern = OperatingSystem.IsWindows() ? WindowsAssetPattern
            : OperatingSystem.IsLinux() ? LinuxAssetPattern
            : throw new InvalidOperationException("Automatic ffmpeg install isn't supported on this OS yet - install ffmpeg manually and set the path above.");

        FlurlConfiguration.ConfigureClientForUrl(GithubLatestReleaseUrl);
        var release = await GithubLatestReleaseUrl
            .WithGithubHeaders()
            .GetJsonAsync<GithubRelease>(cancellationToken: ct);

        var asset = (release.Assets ?? []).FirstOrDefault(a => pattern.IsMatch(a.Name))
            ?? throw new InvalidOperationException("Couldn't find a matching ffmpeg build in the latest release - the release's asset naming may have changed.");

        return (asset.Name, asset.Browser_Download_Url, asset.Size);
    }

    private async Task DownloadWithProgressAsync(string url, string destinationPath, CancellationToken ct)
    {
        // A dedicated, long-timeout client rather than IHttpClientFactory's default (100s) - this is a
        // one-off, potentially 100MB+ download, not a typical short-lived API call.
        using var client = new HttpClient {Timeout = TimeSpan.FromMinutes(30)};
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        if (response.Content.Headers.ContentLength is { } contentLength)
        {
            lock (_lock) { _totalBytes = contentLength; }
        }

        await using var httpStream = await response.Content.ReadAsStreamAsync(ct);
        await using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);

        var buffer = new byte[81920];
        int bytesRead;
        while ((bytesRead = await httpStream.ReadAsync(buffer, ct)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
            lock (_lock) { _bytesDownloaded += bytesRead; }
        }
    }

    /// <summary>
    /// Windows builds are .zip (built-in ZipFile is enough); Linux builds are .tar.xz, a streamed
    /// container+compression combo the BCL has no support for at all - SharpCompress (already a
    /// dependency, used elsewhere for CBR/7z archive handling) reads it the same way it reads those.
    /// </summary>
    private static void Extract(string archivePath, string destinationFolder)
    {
        if (archivePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            ZipFile.ExtractToDirectory(archivePath, destinationFolder, overwriteFiles: true);
            return;
        }

        using var stream = File.OpenRead(archivePath);
        using var reader = ReaderFactory.OpenReader(stream, new ReaderOptions());
        while (reader.MoveToNextEntry())
        {
            if (reader.Entry.IsDirectory) continue;
            reader.WriteEntryToDirectory(destinationFolder, new ExtractionOptions {ExtractFullPath = true, Overwrite = true});
        }
    }

    private void Fail(string error)
    {
        lock (_lock)
        {
            _success = false;
            _error = error;
            _inProgress = false;
        }
    }
}

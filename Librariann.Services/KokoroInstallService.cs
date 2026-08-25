using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Database;
using Librariann.API.Services;
using Librariann.Models.DTOs.Settings;
using Librariann.Models.Entities.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Librariann.Services;

/// <inheritdoc cref="IKokoroInstallService"/>
/// <remarks>
/// Singleton - the download runs detached from any one HTTP request (a 350MB+ zip is well past any reasonable
/// request timeout), so progress has to live somewhere that outlives the request that kicked it off. Same
/// IServiceScopeFactory pattern as KokoroProcessService/TtsRequestQueueService for reaching the Scoped
/// IUnitOfWork/IKokoroReleaseService.
/// </remarks>
public sealed class KokoroInstallService(
    IServiceScopeFactory scopeFactory,
    ILogger<KokoroInstallService> logger) : IKokoroInstallService
{
    private readonly object _lock = new();
    private bool _inProgress;
    private long _bytesDownloaded;
    private long _totalBytes;
    private bool? _success;
    private string? _error;

    public KokoroInstallStatusDto GetStatus()
    {
        lock (_lock)
        {
            return new KokoroInstallStatusDto
            {
                InProgress = _inProgress,
                BytesDownloaded = _bytesDownloaded,
                TotalBytes = _totalBytes,
                Success = _success,
                Error = _error,
            };
        }
    }

    public KokoroInstallStatusDto StartInstall()
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
        string? tempZipPath = null;
        try
        {
            using var scope = scopeFactory.CreateScope();
            var releaseService = scope.ServiceProvider.GetRequiredService<IKokoroReleaseService>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            // IDirectoryService is Scoped, so it is resolved from the scope rather than captured in this
            // Singleton - a captive dependency there fails DI validation on startup.
            var directoryService = scope.ServiceProvider.GetRequiredService<IDirectoryService>();

            // Resolved fresh server-side rather than trusting a URL the frontend might pass in - this is the
            // one piece of the flow that writes to disk and updates a setting, so it shouldn't trust client input.
            var release = await releaseService.GetLatestReleaseAsync(ct);
            if (!release.Success || string.IsNullOrWhiteSpace(release.AssetDownloadUrl))
            {
                Fail(release.ErrorMessage ?? "Couldn't find a downloadable release asset.");
                return;
            }

            lock (_lock) { _totalBytes = release.AssetSizeBytes; }

            var existingFolder = (await unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.KokoroExecutablePath, ct)).Value;
            var installFolder = string.IsNullOrWhiteSpace(existingFolder)
                ? Path.Combine(directoryService.ConfigDirectory, "kokoro")
                : existingFolder;
            directoryService.ExistOrCreate(installFolder);

            tempZipPath = Path.Combine(Path.GetTempPath(), $"lks-install-{Guid.NewGuid():N}.zip");
            await DownloadWithProgressAsync(release.AssetDownloadUrl, tempZipPath, ct);

            logger.LogInformation("Extracting Kokoro install to {Folder}", installFolder);
            ZipFile.ExtractToDirectory(tempZipPath, installFolder, overwriteFiles: true);

            // GitHub release zips commonly wrap their contents in a single top-level folder matching
            // the repo name (e.g. Librariann-Kokoro-Server/LKS.Server.exe) rather than putting files at
            // the zip root - if the executable isn't directly in installFolder, look one level down for
            // whichever extracted subfolder actually has it instead of pointing KokoroExecutablePath at
            // a folder one level too shallow (StartAsync would then never find the exe to launch).
            var resolvedFolder = KokoroProcessService.IsInstalledAt(installFolder)
                ? installFolder
                : Directory.GetDirectories(installFolder).FirstOrDefault(KokoroProcessService.IsInstalledAt)
                  ?? installFolder;

            var existingSetting = await unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.KokoroExecutablePath, ct);
            existingSetting.Value = resolvedFolder;
            unitOfWork.SettingsRepository.Update(existingSetting);
            await unitOfWork.CommitAsync(ct);

            // Copy Librariann's own ffmpeg path into the freshly-installed appsettings.json, same as happens
            // whenever FfmpegPath changes later (SettingsService.UpdateSettings) - one path to manage, not two.
            var kokoroProcessService = scope.ServiceProvider.GetRequiredService<IKokoroProcessService>();
            await kokoroProcessService.SyncFfmpegPathAsync(ct);

            lock (_lock)
            {
                _success = true;
                _inProgress = false;
            }
            logger.LogInformation("Kokoro install complete at {Folder}", resolvedFolder);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Kokoro install failed");
            Fail(ex.Message);
        }
        finally
        {
            if (tempZipPath != null && File.Exists(tempZipPath))
            {
                try { File.Delete(tempZipPath); } catch (IOException) { /* best-effort cleanup */ }
            }
        }
    }

    private async Task DownloadWithProgressAsync(string url, string destinationPath, CancellationToken ct)
    {
        // A dedicated, long-timeout client rather than IHttpClientFactory's default (100s) - this is a one-off
        // 350MB+ download, not a typical short-lived API call.
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

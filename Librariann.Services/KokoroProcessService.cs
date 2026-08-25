using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Database;
using Librariann.API.Services;
using Librariann.Models.DTOs.Settings;
using Librariann.Models.Entities.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Librariann.Services;

/// <inheritdoc cref="IKokoroProcessService"/>
/// <remarks>
/// Singleton (the whole point is remembering a live Process handle across requests), so - same as
/// TtsRequestQueueService - it can't constructor-inject the Scoped IUnitOfWork directly and instead creates a
/// short-lived scope per settings read. Registers an ApplicationStopping hook so a Kokoro process Librariann
/// started doesn't outlive Librariann itself as an orphan.
/// </remarks>
public sealed class KokoroProcessService : IKokoroProcessService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KokoroProcessService> _logger;
    private readonly object _lock = new();
    private Process? _process;

    public KokoroProcessService(IServiceScopeFactory scopeFactory, ILogger<KokoroProcessService> logger, IHostApplicationLifetime lifetime)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        lifetime.ApplicationStopping.Register(Stop);
    }

    public async Task<KokoroProcessStatusDto> GetStatusAsync(CancellationToken ct = default)
    {
        bool isManaged;
        bool isRunning;
        int? processId;
        lock (_lock)
        {
            isManaged = _process != null;
            isRunning = _process is {HasExited: false};
            processId = isRunning ? _process!.Id : null;
        }

        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var folder = (await unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.KokoroExecutablePath, ct)).Value;

        return new KokoroProcessStatusDto
        {
            IsManaged = isManaged,
            IsRunning = isRunning,
            ProcessId = processId,
            IsInstalled = IsInstalledAt(folder),
        };
    }

    // Internal (not private) so KokoroInstallService can reuse this to find where a freshly-extracted
    // release actually put the executable, rather than duplicating the exe-name logic.
    internal static bool IsInstalledAt(string? folder)
    {
        if (string.IsNullOrWhiteSpace(folder)) return false;
        return File.Exists(ResolveExePath(folder));
    }

    internal static string ResolveExePath(string folder)
    {
        var exeName = OperatingSystem.IsWindows() ? "LKS.Server.exe" : "LKS.Server";
        return Path.Combine(folder, exeName);
    }

    public async Task<KokoroProcessStatusDto> StartAsync(CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (_process is {HasExited: false})
            {
                return new KokoroProcessStatusDto {IsManaged = true, IsRunning = true, ProcessId = _process.Id};
            }
        }

        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var folder = (await unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.KokoroExecutablePath, ct)).Value;
        var useGpu = (await unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.KokoroUseGpu, ct)).Value;
        var endpointUrl = (await unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.KokoroEndpointUrl, ct)).Value;

        if (string.IsNullOrWhiteSpace(folder))
        {
            return new KokoroProcessStatusDto
            {
                IsManaged = false, IsRunning = false,
                Error = "Kokoro executable folder isn't set - point it at a Librariann-Kokoro-Server install first.",
            };
        }

        var exePath = ResolveExePath(folder);
        if (!File.Exists(exePath))
        {
            return new KokoroProcessStatusDto
            {
                IsManaged = false, IsRunning = false,
                Error = $"Couldn't find {Path.GetFileName(exePath)} in {folder}.",
            };
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            WorkingDirectory = folder,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        // Environment-variable overrides rather than editing the install's own appsettings.json - .NET's config
        // binding treats double-underscore as the nesting separator (Kokoro__UseGpu -> Kokoro:UseGpu), and
        // ASPNETCORE_URLS is Kestrel's own standard override, so a fresh LKS install works immediately without
        // the admin also having to hand-edit its config to match whatever KokoroEndpointUrl was set to.
        startInfo.EnvironmentVariables["Kokoro__UseGpu"] = useGpu;
        if (!string.IsNullOrWhiteSpace(endpointUrl))
        {
            startInfo.EnvironmentVariables["ASPNETCORE_URLS"] = endpointUrl;
        }

        try
        {
            var process = new Process {StartInfo = startInfo, EnableRaisingEvents = true};
            process.OutputDataReceived += (_, e) => { if (e.Data != null) _logger.LogInformation("[Kokoro] {Line}", e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data != null) _logger.LogWarning("[Kokoro] {Line}", e.Data); };
            process.Exited += (_, _) => _logger.LogWarning("Kokoro process (managed by Librariann) exited unexpectedly with code {Code}", process.ExitCode);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            lock (_lock)
            {
                _process = process;
            }

            _logger.LogInformation("Started Kokoro process {ExePath} (pid {Pid})", exePath, process.Id);
            return new KokoroProcessStatusDto {IsManaged = true, IsRunning = true, ProcessId = process.Id};
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Kokoro process at {ExePath}", exePath);
            return new KokoroProcessStatusDto {IsManaged = false, IsRunning = false, Error = ex.Message};
        }
    }

    public Task<KokoroProcessStatusDto> StopAsync(CancellationToken ct = default)
    {
        Stop();
        return GetStatusAsync(ct);
    }

    private void Stop()
    {
        lock (_lock)
        {
            if (_process == null) return;

            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill(entireProcessTree: true);
                    _process.WaitForExit(5000);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error stopping the managed Kokoro process");
            }
            finally
            {
                _process.Dispose();
                _process = null;
            }
        }
    }

    public async Task SyncFfmpegPathAsync(CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var syncEnabledValue = (await unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.KokoroSyncFfmpegPath, ct)).Value;
        if (!bool.TryParse(syncEnabledValue, out var syncEnabled) || !syncEnabled) return;

        var folder = (await unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.KokoroExecutablePath, ct)).Value;
        if (!IsInstalledAt(folder)) return;

        var ffmpegPath = (await unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.FfmpegPath, ct)).Value;
        if (string.IsNullOrWhiteSpace(ffmpegPath)) return;

        var configPath = Path.Combine(folder!, "appsettings.json");
        if (!File.Exists(configPath)) return;

        try
        {
            var json = await File.ReadAllTextAsync(configPath, ct);
            var root = JsonNode.Parse(json)?.AsObject() ?? [];
            if (root["Ffmpeg"] is not JsonObject ffmpegSection)
            {
                ffmpegSection = [];
                root["Ffmpeg"] = ffmpegSection;
            }
            ffmpegSection["Path"] = ffmpegPath;

            await File.WriteAllTextAsync(configPath,
                root.ToJsonString(new JsonSerializerOptions {WriteIndented = true}), ct);
            _logger.LogInformation(
                "Synced Librariann's ffmpeg path ({Path}) into the managed Kokoro install's appsettings.json",
                ffmpegPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to sync ffmpeg path into the managed Kokoro install's appsettings.json at {ConfigPath}",
                configPath);
        }
    }

    public void Dispose() => Stop();
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;
using Librariann.API.Services;
using Librariann.Common.Helpers;
using Librariann.Models.DTOs.Settings;
using Librariann.Services.Extensions;
using Microsoft.Extensions.Logging;

namespace Librariann.Services;

public class KokoroReleaseService(ILogger<KokoroReleaseService> logger) : IKokoroReleaseService
{
#pragma warning disable S1075
    private const string GithubLatestReleaseUrl = "https://api.github.com/repos/kl3mta3/Librariann-Kokoro-Server/releases/latest";
#pragma warning restore S1075

    // Own shape rather than reusing VersionUpdaterService's GithubReleaseMetadata - that one doesn't carry
    // assets (Librariann's own release checks don't need to download anything), this one does.
    private sealed record KokoroGithubRelease(
        string Tag_Name, string Name, string Html_Url, string Published_At, List<KokoroGithubAsset>? Assets);

    private sealed record KokoroGithubAsset(string Name, string Browser_Download_Url, long Size);

    public async Task<KokoroLatestReleaseDto> GetLatestReleaseAsync(CancellationToken ct = default)
    {
        try
        {
            FlurlConfiguration.ConfigureClientForUrl(GithubLatestReleaseUrl);

            var release = await GithubLatestReleaseUrl
                .WithGithubHeaders()
                .GetJsonAsync<KokoroGithubRelease>(cancellationToken: ct);

            // Only one asset exists as of the current release (a single self-contained Windows zip) - if a
            // future release ships more than one (e.g. per-OS builds), prefer a .zip whose name mentions the
            // current OS, falling back to the first .zip present so this doesn't just break outright.
            var zipAssets = (release.Assets ?? []).Where(a => a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)).ToList();
            var osHint = OperatingSystem.IsWindows() ? "win" : OperatingSystem.IsLinux() ? "linux" : OperatingSystem.IsMacOS() ? "osx" : null;
            var asset = (osHint != null ? zipAssets.FirstOrDefault(a => a.Name.Contains(osHint, StringComparison.OrdinalIgnoreCase)) : null)
                ?? zipAssets.FirstOrDefault();

            return new KokoroLatestReleaseDto
            {
                Success = true,
                TagName = release.Tag_Name,
                Name = release.Name,
                HtmlUrl = release.Html_Url,
                PublishedAtUtc = release.Published_At,
                AssetName = asset?.Name,
                AssetDownloadUrl = asset?.Browser_Download_Url,
                AssetSizeBytes = asset?.Size ?? 0,
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to check the latest Librariann-Kokoro-Server release on GitHub");
            return new KokoroLatestReleaseDto {Success = false, ErrorMessage = ex.Message};
        }
    }
}

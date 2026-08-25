using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Services.Acquisition;
using Librariann.Models.DTOs.Acquisition;

namespace Librariann.Services.Acquisition;

/// <summary>
/// Adapter for the classic µTorrent WebUI API. The configured base URL should point to its /gui path.
/// </summary>
public sealed partial class UTorrentDownloadClient(string providerKey, HttpClient client) : IDownloadClient
{
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private string _token = string.Empty;

    public string ProviderKey => providerKey;
    public DownloadClientKind Kind => DownloadClientKind.UTorrent;
    public DownloadProtocol Protocol => DownloadProtocol.Torrent;

    public void Dispose()
    {
        _tokenLock.Dispose();
        client.Dispose();
    }

    public async Task<ProviderTestResult> TestAsync(CancellationToken cancellationToken = default)
    {
        var timer = Stopwatch.StartNew();
        try
        {
            await GetItemsAsync(cancellationToken);
            return new ProviderTestResult(true, "Connected to µTorrent WebUI.", timer.Elapsed);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return new ProviderTestResult(false, "Unable to connect or authenticate to µTorrent WebUI.", timer.Elapsed);
        }
    }

    public async Task<string> AddDownloadAsync(DownloadGrabRequest request, CancellationToken cancellationToken = default)
    {
        var token = await GetTokenAsync(cancellationToken);
        var path = $"?token={Uri.EscapeDataString(token)}&action=add-url&s={Uri.EscapeDataString(request.DownloadUri.ToString())}";
        using var response = await client.GetAsync(path, cancellationToken);
        response.EnsureSuccessStatusCode();

        var hash = MagnetHash(request.DownloadUri);
        if (string.IsNullOrEmpty(hash))
        {
            var items = await GetItemsAsync(cancellationToken);
            hash = items.FirstOrDefault(item => string.Equals(item.Name, request.ReleaseTitle,
                       StringComparison.OrdinalIgnoreCase))?.ExternalId
                   ?? throw new HttpRequestException("µTorrent accepted the download but did not return a matching torrent.");
        }

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            using var labelResponse = await client.GetAsync(
                $"?token={Uri.EscapeDataString(token)}&action=setprops&hash={Uri.EscapeDataString(hash)}" +
                $"&s=label&v={Uri.EscapeDataString(request.Category.Trim())}", cancellationToken);
            labelResponse.EnsureSuccessStatusCode();
        }

        return hash;
    }

    public async Task<DownloadClientItem?> GetStatusAsync(string externalId, CancellationToken cancellationToken = default) =>
        (await GetItemsAsync(cancellationToken)).FirstOrDefault(item => item.ExternalId.Equals(externalId, StringComparison.OrdinalIgnoreCase));

    public async Task<IReadOnlyCollection<DownloadClientItem>> GetCompletedAsync(CancellationToken cancellationToken = default) =>
        (await GetItemsAsync(cancellationToken)).Where(item => item.IsComplete).ToArray();

    public async Task RemoveAsync(string externalId, bool deleteData, CancellationToken cancellationToken = default)
    {
        var token = await GetTokenAsync(cancellationToken);
        var action = deleteData ? "removedata" : "remove";
        using var response = await client.GetAsync($"?token={Uri.EscapeDataString(token)}&action={action}&hash={Uri.EscapeDataString(externalId)}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task<IReadOnlyCollection<DownloadClientItem>> GetItemsAsync(CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync(cancellationToken);
        using var response = await client.GetAsync($"?token={Uri.EscapeDataString(token)}&list=1", cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        if (!document.RootElement.TryGetProperty("torrents", out var torrents)) return [];

        var items = new List<DownloadClientItem>();
        foreach (var torrent in torrents.EnumerateArray())
        {
            if (torrent.ValueKind != JsonValueKind.Array || torrent.GetArrayLength() < 5) continue;
            var hash = Text(torrent[0]);
            var statusBits = Long(torrent[1]);
            var name = Text(torrent[2]);
            var progress = Math.Clamp(Long(torrent[4]) / 1000d, 0, 1);
            var outputPath = torrent.GetArrayLength() > 26 ? Text(torrent[26]) : string.Empty;
            var message = torrent.GetArrayLength() > 22 ? Text(torrent[22]) : string.Empty;
            var complete = progress >= 1;
            items.Add(new DownloadClientItem(hash, name, Status(statusBits, complete), progress, outputPath, complete,
                message.Contains("Error", StringComparison.OrdinalIgnoreCase) ? message : null));
        }
        return items;
    }

    private async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_token)) return _token;
        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrEmpty(_token)) return _token;
            using var response = await client.GetAsync("token.html", cancellationToken);
            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            var match = TokenPattern().Match(html);
            if (!match.Success) throw new HttpRequestException("µTorrent WebUI did not return an authentication token.");
            _token = WebUtility.HtmlDecode(match.Groups[1].Value);
            return _token;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private static string Status(long bits, bool complete)
    {
        if ((bits & 16) != 0) return complete ? "Seeding" : "Downloading";
        if ((bits & 32) != 0) return "Paused";
        if ((bits & 1) != 0) return "Started";
        return complete ? "Completed" : "Stopped";
    }

    private static string MagnetHash(Uri uri)
    {
        if (!uri.Scheme.Equals("magnet", StringComparison.OrdinalIgnoreCase)) return string.Empty;
        var match = MagnetHashPattern().Match(Uri.UnescapeDataString(uri.ToString()));
        return match.Success ? match.Groups[1].Value.ToUpperInvariant() : string.Empty;
    }

    private static string Text(JsonElement value) => value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : value.ToString();
    private static long Long(JsonElement value) => value.TryGetInt64(out var result) ? result : long.TryParse(Text(value), NumberStyles.Integer, CultureInfo.InvariantCulture, out result) ? result : 0;

    [GeneratedRegex("<div[^>]+id=[\"']token[\"'][^>]*>([^<]+)</div>", RegexOptions.IgnoreCase)]
    private static partial Regex TokenPattern();

    [GeneratedRegex("[?&]xt=urn:btih:([A-Za-z0-9]+)", RegexOptions.IgnoreCase)]
    private static partial Regex MagnetHashPattern();
}

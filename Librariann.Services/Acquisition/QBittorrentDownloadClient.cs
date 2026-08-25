using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Services.Acquisition;
using Librariann.Models.DTOs.Acquisition;

namespace Librariann.Services.Acquisition;

public sealed class QBittorrentDownloadClient(
    string providerKey,
    HttpClient client,
    string username,
    string password) : IDownloadClient
{
    private readonly SemaphoreSlim _authenticationLock = new(1, 1);
    private bool _authenticated;

    public string ProviderKey => providerKey;
    public DownloadClientKind Kind => DownloadClientKind.QBittorrent;
    public DownloadProtocol Protocol => DownloadProtocol.Torrent;

    public void Dispose()
    {
        _authenticationLock.Dispose();
        client.Dispose();
    }

    public async Task<ProviderTestResult> TestAsync(CancellationToken cancellationToken = default)
    {
        var timer = Stopwatch.StartNew();
        try
        {
            await AuthenticateAsync(cancellationToken);
            using var response = await client.GetAsync("api/v2/app/version", cancellationToken);
            response.EnsureSuccessStatusCode();
            return new ProviderTestResult(true, "Connected to qBittorrent.", timer.Elapsed);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return new ProviderTestResult(false, "Unable to connect or authenticate to qBittorrent.", timer.Elapsed);
        }
    }

    public async Task<string> AddDownloadAsync(DownloadGrabRequest request, CancellationToken cancellationToken = default)
    {
        await AuthenticateAsync(cancellationToken);
        var values = new List<KeyValuePair<string, string>>
        {
            new("urls", request.DownloadUri.ToString()),
            new("category", request.Category),
        };
        if (request.Tags.Count > 0) values.Add(new("tags", string.Join(',', request.Tags)));
        using var response = await client.PostAsync("api/v2/torrents/add", new FormUrlEncodedContent(values), cancellationToken);
        response.EnsureSuccessStatusCode();

        var hash = MagnetHash(request.DownloadUri);
        if (!string.IsNullOrEmpty(hash)) return hash;
        var items = await GetItemsAsync("all", cancellationToken);
        return items.FirstOrDefault(item => string.Equals(item.Name, request.ReleaseTitle, StringComparison.OrdinalIgnoreCase))?.ExternalId
               ?? throw new HttpRequestException("qBittorrent accepted the download but did not return a matching torrent.");
    }

    public async Task<DownloadClientItem?> GetStatusAsync(string externalId, CancellationToken cancellationToken = default) =>
        (await GetItemsAsync("all", cancellationToken, externalId)).FirstOrDefault();

    public Task<IReadOnlyCollection<DownloadClientItem>> GetCompletedAsync(CancellationToken cancellationToken = default) =>
        GetItemsAsync("completed", cancellationToken);

    public async Task RemoveAsync(string externalId, bool deleteData, CancellationToken cancellationToken = default)
    {
        await AuthenticateAsync(cancellationToken);
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["hashes"] = externalId,
            ["deleteFiles"] = deleteData ? "true" : "false",
        });
        using var response = await client.PostAsync("api/v2/torrents/delete", content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task AuthenticateAsync(CancellationToken cancellationToken)
    {
        if (_authenticated) return;
        await _authenticationLock.WaitAsync(cancellationToken);
        try
        {
            if (_authenticated) return;
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["username"] = username,
                ["password"] = password,
            });
            using var response = await client.PostAsync("api/v2/auth/login", content, cancellationToken);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!body.Trim().Equals("Ok.", StringComparison.OrdinalIgnoreCase))
                throw new HttpRequestException("qBittorrent rejected the credentials.");
            _authenticated = true;
        }
        finally
        {
            _authenticationLock.Release();
        }
    }

    private async Task<IReadOnlyCollection<DownloadClientItem>> GetItemsAsync(string filter,
        CancellationToken cancellationToken, string? hash = null)
    {
        await AuthenticateAsync(cancellationToken);
        var path = $"api/v2/torrents/info?filter={Uri.EscapeDataString(filter)}";
        if (!string.IsNullOrEmpty(hash)) path += $"&hashes={Uri.EscapeDataString(hash)}";
        using var response = await client.GetAsync(path, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var torrents = JsonSerializer.Deserialize<List<QBittorrentItem>>(json, JsonOptions) ?? [];
        return torrents.Select(item => new DownloadClientItem(item.Hash, item.Name, item.State,
            Math.Clamp(item.Progress, 0, 1), item.SavePath, item.Progress >= 1, string.Empty)).ToArray();
    }

    private static string MagnetHash(Uri uri)
    {
        if (!uri.Scheme.Equals("magnet", StringComparison.OrdinalIgnoreCase)) return string.Empty;
        foreach (var part in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = part.Split('=', 2);
            if (pair.Length != 2 || !pair[0].Equals("xt", StringComparison.OrdinalIgnoreCase)) continue;
            var value = Uri.UnescapeDataString(pair[1]);
            const string prefix = "urn:btih:";
            if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return value[prefix.Length..].ToUpperInvariant();
        }
        return string.Empty;
    }

    private static readonly JsonSerializerOptions JsonOptions = new() {PropertyNameCaseInsensitive = true};

    private sealed record QBittorrentItem(
        [property: JsonPropertyName("hash")] string Hash,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("state")] string State,
        [property: JsonPropertyName("progress")] double Progress,
        [property: JsonPropertyName("save_path")] string SavePath);
}

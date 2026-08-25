using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Services.Acquisition;
using Librariann.Models.DTOs.Acquisition;

namespace Librariann.Services.Acquisition;

public sealed class SabnzbdDownloadClient(string providerKey, HttpClient client, string apiKey) : IDownloadClient
{
    public string ProviderKey => providerKey;
    public DownloadClientKind Kind => DownloadClientKind.Sabnzbd;
    public DownloadProtocol Protocol => DownloadProtocol.Usenet;
    public void Dispose() => client.Dispose();

    public async Task<ProviderTestResult> TestAsync(CancellationToken cancellationToken = default)
    {
        var timer = Stopwatch.StartNew();
        try
        {
            using var document = await SendAsync(new Dictionary<string, string> {{"mode", "version"}}, cancellationToken);
            var version = document.RootElement.TryGetProperty("version", out var value) ? value.GetString() : null;
            return new ProviderTestResult(!string.IsNullOrWhiteSpace(version),
                string.IsNullOrWhiteSpace(version) ? "SABnzbd did not return a version." : $"Connected to SABnzbd {version}.", timer.Elapsed);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return new ProviderTestResult(false, "Unable to connect or authenticate to SABnzbd.", timer.Elapsed);
        }
    }

    public async Task<string> AddDownloadAsync(DownloadGrabRequest request, CancellationToken cancellationToken = default)
    {
        using var document = await SendAsync(new Dictionary<string, string>
        {
            ["mode"] = "addurl",
            ["name"] = request.DownloadUri.ToString(),
            ["cat"] = request.Category,
        }, cancellationToken);
        if (!document.RootElement.TryGetProperty("status", out var status) || !status.GetBoolean())
            throw new HttpRequestException("SABnzbd rejected the download.");
        if (!document.RootElement.TryGetProperty("nzo_ids", out var ids) || ids.GetArrayLength() == 0)
            throw new HttpRequestException("SABnzbd did not return a job identifier.");
        return ids[0].GetString() ?? throw new HttpRequestException("SABnzbd returned an empty job identifier.");
    }

    public async Task<DownloadClientItem?> GetStatusAsync(string externalId, CancellationToken cancellationToken = default)
    {
        var queued = await GetQueueAsync(cancellationToken);
        var item = queued.FirstOrDefault(candidate => candidate.ExternalId == externalId);
        if (item is not null) return item;
        return (await GetCompletedAsync(cancellationToken)).FirstOrDefault(candidate => candidate.ExternalId == externalId);
    }

    public async Task<IReadOnlyCollection<DownloadClientItem>> GetCompletedAsync(CancellationToken cancellationToken = default)
    {
        using var document = await SendAsync(new Dictionary<string, string> {{"mode", "history"}, {"limit", "100"}}, cancellationToken);
        if (!document.RootElement.TryGetProperty("history", out var history) || !history.TryGetProperty("slots", out var slots)) return [];
        return slots.EnumerateArray().Select(slot => new DownloadClientItem(
            Text(slot, "nzo_id"), Text(slot, "name"), Text(slot, "status"), 1,
            Text(slot, "storage"), Text(slot, "status").Equals("Completed", StringComparison.OrdinalIgnoreCase),
            Text(slot, "fail_message"))).ToArray();
    }

    public async Task RemoveAsync(string externalId, bool deleteData, CancellationToken cancellationToken = default)
    {
        var queued = await GetQueueAsync(cancellationToken);
        if (queued.Any(item => item.ExternalId.Equals(externalId, StringComparison.OrdinalIgnoreCase)))
        {
            using var queueDeleteResponse = await SendAsync(new Dictionary<string, string>
            {
                ["mode"] = "queue",
                ["name"] = "delete",
                ["value"] = externalId,
                ["del_files"] = deleteData ? "1" : "0",
            }, cancellationToken);
            return;
        }

        using var historyDeleteResponse = await SendAsync(new Dictionary<string, string>
        {
            ["mode"] = "history",
            ["name"] = "delete",
            ["value"] = externalId,
            ["del_files"] = deleteData ? "1" : "0",
            ["archive"] = "0",
        }, cancellationToken);
    }

    private async Task<IReadOnlyCollection<DownloadClientItem>> GetQueueAsync(CancellationToken cancellationToken)
    {
        using var document = await SendAsync(new Dictionary<string, string> {{"mode", "queue"}}, cancellationToken);
        if (!document.RootElement.TryGetProperty("queue", out var queue) || !queue.TryGetProperty("slots", out var slots)) return [];
        return slots.EnumerateArray().Select(slot =>
        {
            var percentage = double.TryParse(Text(slot, "percentage"), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                ? parsed / 100d : 0;
            return new DownloadClientItem(Text(slot, "nzo_id"), Text(slot, "filename"), Text(slot, "status"),
                Math.Clamp(percentage, 0, 1), string.Empty, false, null);
        }).ToArray();
    }

    private async Task<JsonDocument> SendAsync(Dictionary<string, string> values, CancellationToken cancellationToken)
    {
        values["output"] = "json";
        values["apikey"] = apiKey;
        using var response = await client.PostAsync("api", new FormUrlEncodedContent(values), cancellationToken);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
    }

    private static string Text(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) ? value.ToString() : string.Empty;
}

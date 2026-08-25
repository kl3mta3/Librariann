using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Librariann.API.Services.Acquisition;
using Librariann.Models.DTOs.Acquisition;

namespace Librariann.Services.Acquisition;

/// <summary>
/// Shared Torznab/Newznab adapter. Both protocols use the Newznab API shape and are
/// normalized here before results reach Librariann scoring or UI code.
/// </summary>
public sealed partial class NewznabIndexerProvider(
    string providerKey,
    IndexerProtocol protocol,
    HttpClient httpClient,
    string apiKey) : IIndexerProvider, IDisposable
{
    public string ProviderKey { get; } = providerKey;
    public IndexerProtocol Protocol { get; } = protocol;

    public async Task<ProviderTestResult> TestAsync(CancellationToken cancellationToken = default)
    {
        var timer = Stopwatch.StartNew();
        try
        {
            using var response = await httpClient.GetAsync(BuildQuery(("t", "caps")), cancellationToken);
            response.EnsureSuccessStatusCode();
            _ = XDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            return new ProviderTestResult(true, "Connection successful.", timer.Elapsed);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Xml.XmlException)
        {
            return new ProviderTestResult(false, "The provider did not return a valid capabilities response.", timer.Elapsed);
        }
    }

    public async Task<IndexerCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(BuildQuery(("t", "caps")), cancellationToken);
        response.EnsureSuccessStatusCode();
        var document = XDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var categories = document.Descendants()
            .Where(element => element.Name.LocalName is "category" or "subcat")
            .Select(element => int.TryParse(element.Attribute("id")?.Value, out var id) ? id : 0)
            .Where(id => id > 0)
            .Distinct()
            .ToArray();
        var supportsSearch = document.Descendants().Any(element =>
            element.Name.LocalName == "search" && !string.Equals(element.Attribute("available")?.Value, "no", StringComparison.OrdinalIgnoreCase));

        return new IndexerCapabilities(supportsSearch, true,
            protocol == IndexerProtocol.Torznab ? [DownloadProtocol.Torrent] : [DownloadProtocol.Usenet], categories);
    }

    public async Task<IReadOnlyCollection<ReleaseCandidate>> SearchAsync(IndexerSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var parameters = new List<(string, string)> { ("t", "search"), ("q", BuildSearchText(request)) };
        if (request.Categories.Count > 0) parameters.Add(("cat", string.Join(',', request.Categories)));

        using var response = await httpClient.GetAsync(BuildQuery(parameters.ToArray()), cancellationToken);
        response.EnsureSuccessStatusCode();
        var document = XDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

        return document.Descendants().Where(element => element.Name.LocalName == "item")
            .Select(ParseItem)
            .ToArray();
    }

    public void Dispose() => httpClient.Dispose();

    private ReleaseCandidate ParseItem(XElement item)
    {
        var attributes = item.Descendants()
            .Where(element => element.Name.LocalName == "attr")
            .Select(element => (Name: element.Attribute("name")?.Value, Value: element.Attribute("value")?.Value))
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Name))
            .GroupBy(pair => pair.Name!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last().Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);

        var title = ChildValue(item, "title");
        var enclosure = item.Elements().FirstOrDefault(element => element.Name.LocalName == "enclosure");
        var downloadText = enclosure?.Attribute("url")?.Value ?? ChildValue(item, "link");
        var sizeText = attributes.GetValueOrDefault("size") ?? enclosure?.Attribute("length")?.Value;
        var language = attributes.GetValueOrDefault("language") ?? attributes.GetValueOrDefault("lang") ?? string.Empty;

        return new ReleaseCandidate
        {
            ProviderKey = ProviderKey,
            ProviderReleaseId = ChildValue(item, "guid"),
            Title = title,
            Author = attributes.GetValueOrDefault("author") ?? string.Empty,
            Edition = attributes.GetValueOrDefault("edition") ?? string.Empty,
            Language = language,
            Format = InferFormat(attributes.GetValueOrDefault("booktype") ?? title),
            Protocol = protocol == IndexerProtocol.Torznab ? DownloadProtocol.Torrent : DownloadProtocol.Usenet,
            SizeBytes = long.TryParse(sizeText, out var size) ? size : 0,
            PublishedAt = DateTimeOffset.TryParse(ChildValue(item, "pubDate"), CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces, out var published) ? published : DateTimeOffset.MinValue,
            Seeders = ParseNullableInt(attributes.GetValueOrDefault("seeders")),
            Peers = ParseNullableInt(attributes.GetValueOrDefault("peers")),
            IsRetail = title.Contains("retail", StringComparison.OrdinalIgnoreCase),
            DownloadUri = ToAbsoluteUri(downloadText),
            DetailsUri = ToAbsoluteUri(ChildValue(item, "comments")),
            ProviderData = attributes,
        };
    }

    private string BuildQuery(params (string Key, string Value)[] parameters)
    {
        var all = parameters.ToList();
        if (!string.IsNullOrWhiteSpace(apiKey)) all.Add(("apikey", apiKey));
        return "?" + string.Join('&', all.Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
            .Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
    }

    private static string BuildSearchText(IndexerSearchRequest request) =>
        string.Join(' ', new[] { request.Query, request.Title, request.Author, request.Series, request.Isbn }
            .Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase));

    private Uri? ToAbsoluteUri(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return Uri.TryCreate(value, UriKind.Absolute, out var absolute)
            ? absolute
            : new Uri(httpClient.BaseAddress!, value);
    }

    private static string ChildValue(XElement item, string localName) =>
        item.Elements().FirstOrDefault(element => element.Name.LocalName == localName)?.Value?.Trim() ?? string.Empty;

    private static int? ParseNullableInt(string? value) => int.TryParse(value, out var number) ? number : null;

    private static AcquisitionMediaFormat InferFormat(string value)
    {
        var match = FormatRegex().Match(value);
        return match.Success ? match.Value.ToLowerInvariant() switch
        {
            "epub" => AcquisitionMediaFormat.Epub,
            "azw3" => AcquisitionMediaFormat.Azw3,
            "mobi" => AcquisitionMediaFormat.Mobi,
            "pdf" => AcquisitionMediaFormat.Pdf,
            "cbz" => AcquisitionMediaFormat.Cbz,
            "cbr" => AcquisitionMediaFormat.Cbr,
            "cb7" => AcquisitionMediaFormat.Cb7,
            _ => AcquisitionMediaFormat.Unknown,
        } : AcquisitionMediaFormat.Unknown;
    }

    [GeneratedRegex("\\b(epub|azw3|mobi|pdf|cbz|cbr|cb7)\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FormatRegex();
}

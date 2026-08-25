using System;
using System.Collections.Generic;
using System.Linq;

namespace Librariann.Common.Security;

/// <summary>
/// Builds a CSP frame-ancestors policy from exact HTTP(S) origins. Wildcards, credentials, paths, queries, and
/// fragments are rejected so enabling the Plex/embed surface does not become a global clickjacking bypass.
/// </summary>
public sealed record EmbeddingPolicy(
    string FrameAncestors,
    IReadOnlyCollection<string> AllowedOrigins,
    IReadOnlyCollection<string> RejectedOrigins)
{
    public bool AllowsExternalOrigins => AllowedOrigins.Count > 0;

    public static EmbeddingPolicy Create(IEnumerable<string>? configuredOrigins)
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rejected = new List<string>();
        foreach (var configured in configuredOrigins ?? [])
        {
            var candidate = configured?.Trim() ?? string.Empty;
            if (!TryNormalizeOrigin(candidate, out var origin))
            {
                if (candidate.Length > 0) rejected.Add(candidate);
                continue;
            }
            allowed.Add(origin);
        }

        var ordered = allowed.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray();
        var ancestors = ordered.Length == 0
            ? "frame-ancestors 'self';"
            : $"frame-ancestors 'self' {string.Join(' ', ordered)};";
        return new EmbeddingPolicy(ancestors, ordered, rejected);
    }

    private static bool TryNormalizeOrigin(string value, out string origin)
    {
        origin = string.Empty;
        if (value.Contains('*', StringComparison.Ordinal) ||
            !Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) ||
            string.IsNullOrWhiteSpace(uri.Host) || !string.IsNullOrEmpty(uri.UserInfo) ||
            (uri.AbsolutePath != "/" && uri.AbsolutePath.Length > 0) ||
            !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)) return false;

        origin = uri.GetComponents(UriComponents.SchemeAndServer, UriFormat.UriEscaped).TrimEnd('/');
        return true;
    }
}

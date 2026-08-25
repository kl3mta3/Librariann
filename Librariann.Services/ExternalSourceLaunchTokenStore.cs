using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using Librariann.API.Services;

namespace Librariann.Services;

public sealed class ExternalSourceLaunchTokenStore : IExternalSourceLaunchTokenStore
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(2);
    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);

    public string Issue(Uri destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (!destination.IsAbsoluteUri || destination.Scheme is not ("http" or "https"))
            throw new ArgumentException("External-source destinations must be absolute HTTP(S) URIs.",
                nameof(destination));

        CleanupExpired();
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _entries[token] = new Entry(destination, DateTimeOffset.UtcNow.Add(Lifetime));
        return token;
    }

    public bool TryTake(string token, out Uri? destination)
    {
        destination = null;
        if (string.IsNullOrWhiteSpace(token) || !_entries.TryRemove(token, out var entry)) return false;
        if (entry.ExpiresAt <= DateTimeOffset.UtcNow) return false;
        destination = entry.Destination;
        return true;
    }

    private void CleanupExpired()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in _entries)
        {
            if (entry.Value.ExpiresAt <= now) _entries.TryRemove(entry.Key, out _);
        }
    }

    private sealed record Entry(Uri Destination, DateTimeOffset ExpiresAt);
}

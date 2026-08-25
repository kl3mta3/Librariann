using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using Librariann.API.Services.Acquisition;
using Librariann.Models.DTOs.Acquisition;

namespace Librariann.Services.Acquisition;

public sealed class ReleaseTokenStore : IReleaseTokenStore
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);
    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);

    public string Issue(int userId, ReleaseCandidate release)
    {
        CleanupExpired();
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _entries[token] = new Entry(userId, release, DateTimeOffset.UtcNow.Add(Lifetime));
        return token;
    }

    public bool TryTake(int userId, string token, out ReleaseCandidate? release)
    {
        release = null;
        if (string.IsNullOrWhiteSpace(token) || !_entries.TryGetValue(token, out var entry)) return false;
        if (entry.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _entries.TryRemove(token, out _);
            return false;
        }
        if (entry.UserId != userId || !_entries.TryRemove(token, out var removed)) return false;
        release = removed.Release;
        return true;
    }

    private void CleanupExpired()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var item in _entries)
        {
            if (item.Value.ExpiresAt <= now) _entries.TryRemove(item.Key, out _);
        }
    }

    private sealed record Entry(int UserId, ReleaseCandidate Release, DateTimeOffset ExpiresAt);
}

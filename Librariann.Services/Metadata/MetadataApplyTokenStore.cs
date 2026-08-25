using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using Librariann.API.Services.Metadata;
using Librariann.Models.DTOs.Metadata;

namespace Librariann.Services.Metadata;

public sealed class MetadataApplyTokenStore : IMetadataApplyTokenStore
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);
    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);

    public string Issue(int userId, NormalizedMetadataCandidate candidate)
    {
        CleanupExpired();
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _entries[token] = new Entry(userId, candidate, DateTimeOffset.UtcNow.Add(Lifetime));
        return token;
    }

    public bool TryTake(int userId, string token, out NormalizedMetadataCandidate? candidate)
    {
        candidate = null;
        if (string.IsNullOrWhiteSpace(token) || !_entries.TryGetValue(token, out var entry)) return false;
        if (entry.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _entries.TryRemove(token, out _);
            return false;
        }
        // Do not let a different user invalidate a valid token merely by presenting it.
        if (entry.UserId != userId || !_entries.TryRemove(token, out var removed)) return false;
        candidate = removed.Candidate;
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

    private sealed record Entry(int UserId, NormalizedMetadataCandidate Candidate, DateTimeOffset ExpiresAt);
}

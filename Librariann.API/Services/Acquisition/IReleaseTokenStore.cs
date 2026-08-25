using Librariann.Models.DTOs.Acquisition;

namespace Librariann.API.Services.Acquisition;

/// <summary>
/// Keeps provider download URLs server-side and issues short-lived, user-bound references.
/// </summary>
public interface IReleaseTokenStore
{
    string Issue(int userId, ReleaseCandidate release);
    bool TryTake(int userId, string token, out ReleaseCandidate? release);
}

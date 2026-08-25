using Librariann.Models.DTOs.Metadata;

namespace Librariann.API.Services.Metadata;

/// <summary>
/// Keeps normalized provider results on the server and issues short-lived, single-use, user-bound references.
/// </summary>
public interface IMetadataApplyTokenStore
{
    string Issue(int userId, NormalizedMetadataCandidate candidate);
    bool TryTake(int userId, string token, out NormalizedMetadataCandidate? candidate);
}

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs.Collection;
using Librariann.Models.Entities.User;

namespace Librariann.API.Services;

public interface ICollectionTagService
{
    Task<bool> DeleteTag(int tagId, AppUser user, CancellationToken ct = default);
    Task<bool> UpdateTag(AppUserCollectionDto dto, int userId, CancellationToken ct = default);
    /// <summary>
    /// Removes series from Collection tag. Will recalculate max age rating.
    /// </summary>
    /// <param name="tag"></param>
    /// <param name="seriesIds"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<bool> RemoveTagFromSeries(AppUserCollection? tag, IEnumerable<int> seriesIds, CancellationToken ct = default);

    Task<string> GenerateCollectionCoverImage(int collectionId);
}

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Librariann.Common.Helpers;
using Librariann.Models.DTOs.Metadata;
using Librariann.Models.DTOs.Metadata.Browse;
using Librariann.Models.Entities;

namespace Librariann.API.Repositories;

public interface ITagRepository
{
    void Attach(Tag tag);
    void Remove(Tag tag);
    Task<IList<Tag>> GetAllTagsByNameAsync(IEnumerable<string> normalizedNames, CancellationToken ct = default);
    Task RemoveAllTagNoLongerAssociated(CancellationToken ct = default);
    Task<IList<TagDto>> GetAllTagDtosForLibrariesAsync(int userId, IList<int>? libraryIds = null, CancellationToken ct = default);
    Task<List<string>> GetAllTagsNotInListAsync(ICollection<string> tags, CancellationToken ct = default);
    Task<PagedList<BrowseTagDto>> GetBrowseableTag(int userId, UserParams userParams, CancellationToken ct = default);
}

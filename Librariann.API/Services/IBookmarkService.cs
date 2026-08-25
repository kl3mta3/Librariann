using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs.Reader;
using Librariann.Models.Entities.User;

namespace Librariann.API.Services;

public interface IBookmarkService
{
    Task DeleteBookmarkFiles(IEnumerable<AppUserBookmark> bookmarks, CancellationToken ct = default);
    Task<bool> BookmarkPage(AppUser userWithBookmarks, BookmarkDto bookmarkDto, string imageToBookmark, CancellationToken ct = default);
    Task<bool> RemoveBookmarkPage(AppUser userWithBookmarks, BookmarkDto bookmarkDto, CancellationToken ct = default);
    Task<IEnumerable<string>> GetBookmarkFilesById(int seriesId, IEnumerable<int> bookmarkIds, CancellationToken ct = default);
}

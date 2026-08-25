using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Librariann.Common.Helpers;
using Librariann.Models.DTOs.Metadata;
using Librariann.Models.DTOs.Metadata.Browse;
using Librariann.Models.Entities;

namespace Librariann.API.Repositories;

public interface IGenreRepository
{
    void Attach(Genre genre);
    Task<IList<Genre>> GetAllGenresByNamesAsync(IEnumerable<string> normalizedNames, CancellationToken ct = default);
    Task RemoveAllGenreNoLongerAssociated(CancellationToken ct = default);
    Task<IList<GenreTagDto>> GetAllGenreDtosForLibrariesAsync(int userId, IList<int>? libraryIds = null, QueryContext context = QueryContext.None, CancellationToken ct = default);
    Task<GenreTagDto?> GetRandomGenre(CancellationToken ct = default);
    Task<List<string>> GetAllGenresNotInListAsync(ICollection<string> genreNames, CancellationToken ct = default);
    Task<PagedList<BrowseGenreDto>> GetBrowseableGenre(int userId, UserParams userParams, CancellationToken ct = default);
}

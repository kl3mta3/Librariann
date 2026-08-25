using System.Collections.Generic;
using Librariann.Models.DTOs.Collection;
using Librariann.Models.DTOs.Metadata;
using Librariann.Models.DTOs.Person;
using Librariann.Models.DTOs.Reader;
using Librariann.Models.DTOs.ReadingLists;

namespace Librariann.Models.DTOs.Search;

/// <summary>
/// Represents all Search results for a query
/// </summary>
public sealed record SearchResultGroupDto
{
    public IEnumerable<LibraryDto> Libraries { get; set; } = default!;
    public IEnumerable<SearchResultDto> Series { get; set; } = default!;
    public IEnumerable<AppUserCollectionDto> Collections { get; set; } = default!;
    public IEnumerable<ReadingListDto> ReadingLists { get; set; } = default!;
    public IEnumerable<PersonDto> Persons { get; set; } = default!;
    public IEnumerable<GenreTagDto> Genres { get; set; } = default!;
    public IEnumerable<TagDto> Tags { get; set; } = default!;
    public IEnumerable<MangaFileDto> Files { get; set; } = default!;
    public IEnumerable<ChapterDto> Chapters { get; set; } = default!;
    public IEnumerable<BookmarkSearchResultDto> Bookmarks { get; set; } = default!;
    public IEnumerable<AnnotationDto> Annotations { get; set; } = default!;


}

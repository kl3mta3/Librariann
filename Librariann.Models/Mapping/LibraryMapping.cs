using System;
using System.Linq;
using System.Linq.Expressions;
using Librariann.Models.DTOs;
using Librariann.Models.Entities;

namespace Librariann.Models.Mapping;

/// <summary>
/// Explicit replacement for <c>CreateMap&lt;Library, LiteLibraryDto&gt;()</c> and
/// <c>CreateMap&lt;Library, LibraryDto&gt;()</c>.
/// </summary>
public static class LibraryMapping
{
    public static readonly Expression<Func<Library, LiteLibraryDto>> ToLiteLibraryDtoExpression = l => new LiteLibraryDto
    {
        Id = l.Id,
        Name = l.Name,
        Type = l.Type,
    };

    public static readonly Expression<Func<Library, LibraryDto>> ToLibraryDtoExpression = l => new LibraryDto
    {
        Id = l.Id,
        Name = l.Name,
        Type = l.Type,
        LastScanned = l.LastScanned,
        CoverImage = l.CoverImage,
        FolderWatching = l.FolderWatching,
        IncludeInDashboard = l.IncludeInDashboard,
        IncludeInRecommended = l.IncludeInRecommended,
        ManageCollections = l.ManageCollections,
        ManageReadingLists = l.ManageReadingLists,
        IncludeInSearch = l.IncludeInSearch,
        AllowScrobbling = l.AllowScrobbling,
        // NOT null-guarded on purpose: this Expression is used in EF Core .Select()/ProjectTo queries, which
        // translate navigation-collection access into a correlated SQL subquery without reading the actual CLR
        // property (safe regardless of Include state). `?? Enumerable.Empty<T>()` here would make EF Core throw
        // "could not be translated" instead — see ChapterMapping's XML doc for the full explanation.
        Folders = l.Folders.Select(x => x.Path).ToList(),
        LibraryFileTypes = l.LibraryFileTypes.Select(x => x.FileTypeGroup).ToList(),
        ExcludePatterns = l.LibraryExcludePatterns.Select(x => x.Pattern).ToList(),
        AllowMetadataMatching = l.AllowMetadataMatching,
        EnableMetadata = l.EnableMetadata,
        RemovePrefixForSortName = l.RemovePrefixForSortName,
        InheritWebLinksFromFirstChapter = l.InheritWebLinksFromFirstChapter,
        DefaultLanguage = l.DefaultLanguage,
        MetadataProvider = l.MetadataProvider,
    };

    private static readonly Func<Library, LiteLibraryDto> CompiledToLiteLibraryDto = ToLiteLibraryDtoExpression.Compile();
    private static readonly Func<Library, LibraryDto> CompiledToLibraryDto = ToLibraryDtoExpression.Compile();

    public static LiteLibraryDto ToLiteLibraryDto(this Library l) => CompiledToLiteLibraryDto(l);
    public static LibraryDto ToLibraryDto(this Library l) => CompiledToLibraryDto(l);
}

using Librariann.Models.DTOs.Progress;
using Librariann.Models.Entities.Progress;

namespace Librariann.Models.Mapping;

/// <summary>
/// Explicit replacement for <c>CreateMap&lt;AppUserReadingSessionActivityData, ReadingActivityDataDto&gt;()</c>.
/// AutoMapper had no explicit <c>ForMember</c> here, but its "flattening" convention resolved
/// <c>LibraryName</c>/<c>SeriesName</c>/<c>ChapterTitle</c> from the <c>Library.Name</c>/<c>Series.Name</c>/
/// <c>Chapter.Title</c> navigation properties by name convention — with a null-safe guard at each level, since
/// AutoMapper generates a null check before dereferencing a flattened navigation property. This matters in
/// practice: <see cref="Librariann.Database.Repositories.ReadingSessionRepository"/> deliberately does not
/// <c>.Include()</c> these navigations and instead enriches the names afterward from a separate batched lookup,
/// which only works if this mapping doesn't throw when they're unloaded.
/// </summary>
public static class ReadingActivityMapping
{
    public static ReadingActivityDataDto ToReadingActivityDataDto(this AppUserReadingSessionActivityData a) => new()
    {
        ChapterId = a.ChapterId,
        VolumeId = a.VolumeId,
        SeriesId = a.SeriesId,
        LibraryId = a.LibraryId,
        StartPage = a.StartPage,
        EndPage = a.EndPage,
        StartTimeUtc = a.StartTimeUtc,
        EndTimeUtc = a.EndTimeUtc,
        PagesRead = a.PagesRead,
        WordsRead = a.WordsRead,
        TotalPages = a.TotalPages,
        TotalWords = (int) a.TotalWords,
        LibraryName = a.Library?.Name!,
        SeriesName = a.Series?.Name!,
        ChapterTitle = a.Chapter?.Title!,
        ClientInfo = a.ClientInfo?.ToClientInfoDto(),
    };
}

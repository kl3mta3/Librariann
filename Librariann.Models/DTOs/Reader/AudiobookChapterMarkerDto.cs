namespace Librariann.Models.DTOs.Reader;
#nullable enable

/// <summary>
/// One embedded chapter marker extracted from an M4B's chapter atoms via ffprobe. Used to render scrubber tick
/// marks and drive in-file prev/next-chapter seeking for single-file audiobooks. See
/// <see cref="Librariann.Models.Entities.MangaFile.ChapterMarkersJson"/> for how this is persisted.
/// </summary>
public sealed record AudiobookChapterMarkerDto
{
    public string? Title { get; set; }
    public required double StartSeconds { get; set; }
    public required double EndSeconds { get; set; }
}

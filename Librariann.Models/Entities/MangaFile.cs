
using System;
using System.IO;
using Librariann.Models.Entities.Enums;
using Librariann.Models.Entities.Interfaces;

namespace Librariann.Models.Entities;

/// <summary>
/// Represents a wrapper to the underlying file. This provides information around file, like number of pages, format, etc.
/// </summary>
public class MangaFile : IEntityDate
{
    public int Id { get; set; }
    /// <summary>
    /// The filename without extension
    /// </summary>
    public string FileName { get; set; }
    /// <summary>
    /// Absolute path to the archive file
    /// </summary>
    public required string FilePath { get; set; }
    /// <summary>
    /// A hash of the document using Koreader's unique hashing algorithm
    /// </summary>
    public string? KoreaderHash { get; set; }
    /// <summary>
    /// Number of pages for the given file
    /// </summary>
    public int Pages { get; set; }
    public MangaFormat Format { get; set; }
    /// <summary>
    /// How many bytes make up this file
    /// </summary>
    public long Bytes { get; set; }
    /// <summary>
    /// File extension
    /// </summary>
    public string? Extension { get; set; }
    /// <summary>
    /// Duration of the audio in seconds, as reported by ffprobe. 0 for non-audio formats.
    /// </summary>
    public double DurationSeconds { get; set; }
    /// <summary>
    /// Source bitrate in kbps, as reported by ffprobe. 0 for non-audio formats.
    /// </summary>
    public int Bitrate { get; set; }
    /// <summary>
    /// JSON-serialized list of <see cref="Librariann.Models.DTOs.Reader.AudiobookChapterMarkerDto"/>, extracted
    /// from embedded M4B chapter atoms via ffprobe. Null/empty when the file has no embedded chapters (e.g. a
    /// multi-file audiobook where each MangaFile already IS a chapter) or is not an audio format.
    /// </summary>
    public string? ChapterMarkersJson { get; set; }
    /// <inheritdoc cref="IEntityDate.Created"/>
    public DateTime Created { get; set; }
    /// <summary>
    /// Last time underlying file was modified
    /// </summary>
    /// <remarks>This gets updated anytime the file is scanned</remarks>
    public DateTime LastModified { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime LastModifiedUtc { get; set; }

    /// <summary>
    /// Last time file analysis ran on this file
    /// </summary>
    public DateTime LastFileAnalysis { get; set; }
    public DateTime LastFileAnalysisUtc { get; set; }


    // Relationship Mapping
    public Chapter Chapter { get; set; } = null!;
    public int ChapterId { get; set; }


    /// <summary>
    /// Updates the Last Modified time of the underlying file to the LastWriteTime
    /// </summary>
    public void UpdateLastModified()
    {
        if (FilePath == null) return;
        LastModified = File.GetLastWriteTime(FilePath);
        LastModifiedUtc = File.GetLastWriteTimeUtc(FilePath);
    }

    public void UpdateLastFileAnalysis()
    {
        LastFileAnalysis = DateTime.Now;
        LastFileAnalysisUtc = DateTime.UtcNow;
    }
}

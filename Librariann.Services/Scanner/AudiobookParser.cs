using System.IO;
using Librariann.API.Services;
using Librariann.Models.Entities.Enums;
using Librariann.Models.Metadata;
using Librariann.Models.Parser;

namespace Librariann.Services.Scanner;

/// <summary>
/// Parses M4B/MP3/M4A audiobook files. Modeled directly on <see cref="PdfParser"/>: a single, non-archive file
/// per ParserInfo. Unlike PdfParser, there's no forced "always DefaultChapter" override - a lone M4B with no
/// chapter number in its filename naturally falls back to DefaultChapter via the normal chapter-regex miss, while
/// a folder of "Chapter 01.mp3" ... "Chapter NN.mp3" files naturally parses into separate chapters the same way
/// manga/comic multi-file series already do. No ComicInfo-equivalent metadata source exists for audio in v1.
/// </summary>
public class AudiobookParser(IDirectoryService directoryService) : DefaultParser(directoryService)
{
    public override ParserInfo? Parse(string filePath, string rootPath, string libraryRoot, LibraryType type, bool enableMetadata = true, ComicInfo? comicInfo = null)
    {
        var fileName = directoryService.FileSystem.Path.GetFileNameWithoutExtension(filePath);
        var ret = new ParserInfo
        {
            Filename = Path.GetFileName(filePath),
            Format = Parser.ParseFormat(filePath),
            Title = Parser.RemoveExtensionIfSupported(fileName)!,
            FullFilePath = Parser.NormalizePath(filePath),
            Series = string.Empty,
            ComicInfo = comicInfo,
            Chapters = Parser.ParseChapter(fileName, type),
            HasEndMarker = Parser.HasEndMarker(fileName)
        };

        ret.Series = Parser.ParseSeries(fileName, type);
        ret.Volumes = Parser.ParseVolume(fileName, type);

        if (ret.Series == string.Empty)
        {
            // Try to parse information out of each folder all the way to rootPath
            ParseFromFallbackFolders(filePath, rootPath, type, ref ret);
        }

        var edition = Parser.ParseEdition(fileName);
        if (!string.IsNullOrEmpty(edition))
        {
            ret.Series = Parser.CleanTitle(ret.Series.Replace(edition, string.Empty));
            ret.Edition = edition;
        }

        var isSpecial = Parser.IsSpecial(fileName, type);
        if (Parser.IsDefaultChapter(ret.Chapters) && Parser.IsLooseLeafVolume(ret.Volumes) && isSpecial)
        {
            ret.IsSpecial = true;
            ParseFromFallbackFolders(filePath, rootPath, type, ref ret);
        }

        if (Parser.HasSpecialMarker(fileName))
        {
            ret.IsSpecial = true;
            ret.SpecialIndex = Parser.ParseSpecialIndex(fileName);
            ret.Chapters = Parser.DefaultChapter;
            ret.Volumes = Parser.SpecialVolume;

            var tempRootPath = rootPath;
            if (rootPath.EndsWith("Specials") || rootPath.EndsWith("Specials/"))
            {
                tempRootPath = rootPath.Replace("Specials", string.Empty).TrimEnd('/');
            }

            ParseFromFallbackFolders(filePath, tempRootPath, type, ref ret);
        }

        // No ComicInfo-equivalent metadata source exists for audio files in v1 - UpdateFromComicInfo is a no-op
        // when ret.ComicInfo is null, kept here for forward-compatibility if ffprobe-derived metadata is ever
        // mapped into a synthetic ComicInfo later.
        if (enableMetadata)
        {
            UpdateFromComicInfo(ret);
        }

        if (string.IsNullOrEmpty(ret.Series))
        {
            ret.Series = Parser.CleanTitle(fileName);
        }

        if (ret.IsSpecial)
        {
            ret.Volumes = $"{Parser.SpecialVolumeNumber}";
        }

        FinalizeNumbers(ret);

        return string.IsNullOrEmpty(ret.Series) ? null : ret;
    }

    /// <summary>
    /// Only applicable for M4B/MP3/M4A files
    /// </summary>
    public override bool IsApplicable(string filePath, LibraryType type)
    {
        return Parser.IsAudio(filePath);
    }
}

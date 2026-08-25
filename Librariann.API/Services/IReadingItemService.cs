using Librariann.Models.Entities.Enums;
using Librariann.Models.Parser;

namespace Librariann.API.Services;

public interface IReadingItemService
{
    int GetNumberOfPages(string filePath, MangaFormat format);
    string GetCoverImage(string filePath, string fileName, MangaFormat format, EncodeFormat encodeFormat, CoverImageSize size = CoverImageSize.Default);
    void Extract(string fileFilePath, string targetDirectory, MangaFormat format, int imageCount = 1);
    ParserInfo? ParseFile(string path, string rootPath, string libraryRoot, LibraryType type, bool enableMetadata);
}

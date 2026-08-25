using System;
using System.Collections.Generic;
using Librariann.Models.Entities;

namespace Librariann.API.Services;

public interface IDownloadService
{
    Tuple<string, string, string> GetFirstFileDownload(IEnumerable<MangaFile> files);
    string GetContentTypeFromFile(string filepath);
}

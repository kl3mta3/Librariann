using System;
using System.IO.Abstractions;

namespace Librariann.API.Services;

public interface IFileService
{
    IFileSystem GetFileSystem();
    bool HasFileBeenModifiedSince(string filePath, DateTime time);
    bool Exists(string filePath);
    bool ValidateSha(string filepath, string sha);
}

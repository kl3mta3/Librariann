using System;
using System.Linq.Expressions;
using Librariann.Models.DTOs;
using Librariann.Models.DTOs.Stats;
using Librariann.Models.Entities;

namespace Librariann.Models.Mapping;

/// <summary>
/// Explicit replacement for <c>CreateMap&lt;MangaFile, MangaFileDto&gt;()</c> and
/// <c>CreateMap&lt;MangaFile, FileExtensionExportDto&gt;()</c>.
/// </summary>
public static class MangaFileMapping
{
    public static readonly Expression<Func<MangaFile, MangaFileDto>> ToMangaFileDtoExpression = f => new MangaFileDto
    {
        Id = f.Id,
        FilePath = f.FilePath,
        Pages = f.Pages,
        Bytes = f.Bytes,
        Format = f.Format,
        Created = f.Created,
        Extension = f.Extension,
        KoreaderHash = f.KoreaderHash,
    };

    public static readonly Expression<Func<MangaFile, FileExtensionExportDto>> ToFileExtensionExportDtoExpression = f => new FileExtensionExportDto
    {
        FilePath = f.FilePath,
        Extension = f.Extension,
    };

    private static readonly Func<MangaFile, MangaFileDto> CompiledToMangaFileDto = ToMangaFileDtoExpression.Compile();
    private static readonly Func<MangaFile, FileExtensionExportDto> CompiledToFileExtensionExportDto = ToFileExtensionExportDtoExpression.Compile();

    public static MangaFileDto ToMangaFileDto(this MangaFile f) => CompiledToMangaFileDto(f);
    public static FileExtensionExportDto ToFileExtensionExportDto(this MangaFile f) => CompiledToFileExtensionExportDto(f);
}

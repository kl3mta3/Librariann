using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Librariann.API.Services.Metadata;
using Librariann.Common;
using Librariann.Models.DTOs.Metadata;
using Librariann.Models.Metadata;

namespace Librariann.Services.Metadata;

public sealed class CbzMetadataFileWriter : IMetadataFileWriter
{
    private const string ComicInfoFileName = "ComicInfo.xml";
    private static readonly XmlSerializer Serializer = new(typeof(ComicInfo));

    public bool CanWrite(string extension) => extension.Equals(".cbz", StringComparison.OrdinalIgnoreCase);

    public Task WriteAsync(string temporaryFilePath, MetadataFileUpdate update,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var archive = ZipFile.Open(temporaryFilePath, ZipArchiveMode.Update);
        if (archive.Entries.Count > 100_000) throw new LibrariannException("metadata-archive-has-too-many-entries");
        var entries = archive.Entries.Where(IsComicInfo).ToArray();
        if (entries.Length > 1) throw new LibrariannException("metadata-archive-has-duplicate-comicinfo-files");
        var info = entries.Length == 1 ? Read(entries[0]) : new ComicInfo();
        Apply(info, update);
        foreach (var entry in entries) entry.Delete();
        var output = archive.CreateEntry(ComicInfoFileName, CompressionLevel.Optimal);
        using var stream = output.Open();
        using var writer = XmlWriter.Create(stream, new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = true,
            CloseOutput = false,
        });
        Serializer.Serialize(writer, info);
        return Task.CompletedTask;
    }

    public Task ValidateAsync(string temporaryFilePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var archive = ZipFile.OpenRead(temporaryFilePath);
        if (archive.Entries.Count == 0) throw new LibrariannException("metadata-archive-is-empty");
        var entries = archive.Entries.Where(IsComicInfo).ToArray();
        if (entries.Length != 1) throw new LibrariannException("metadata-archive-comicinfo-validation-failed");
        _ = Read(entries[0]);
        return Task.CompletedTask;
    }

    private static bool IsComicInfo(ZipArchiveEntry entry) =>
        Path.GetFileName(entry.FullName).Equals(ComicInfoFileName, StringComparison.OrdinalIgnoreCase);

    private static ComicInfo Read(ZipArchiveEntry entry)
    {
        if (entry.Length > 4 * 1024 * 1024) throw new LibrariannException("metadata-comicinfo-is-too-large");
        using var stream = entry.Open();
        using var reader = XmlReader.Create(stream, SecureXmlSettings());
        return Serializer.Deserialize(reader) as ComicInfo
               ?? throw new LibrariannException("metadata-comicinfo-is-invalid");
    }

    private static XmlReaderSettings SecureXmlSettings() => new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        MaxCharactersInDocument = 4 * 1024 * 1024,
        IgnoreComments = true,
    };

    private static void Apply(ComicInfo info, MetadataFileUpdate update)
    {
        if (update.Title is not null) info.Title = update.Title.Trim();
        if (update.Series is not null) info.Series = update.Series.Trim();
        if (update.Description is not null) info.Summary = update.Description.Trim();
        if (update.Language is not null) info.LanguageISO = update.Language.Trim();
        if (update.PublicationYear is >= 1 and <= 9999) info.Year = update.PublicationYear.Value;
        if (update.Authors is not null) info.Writer = string.Join(", ", update.Authors.Where(NotBlank).Select(value => value.Trim()));
        if (update.Genres is not null) info.Genre = string.Join(", ", update.Genres.Where(NotBlank).Select(value => value.Trim()));
        if (update.Isbn is not null) info.GTIN = update.Isbn.Trim();
        if (update.Publisher is not null) info.Publisher = update.Publisher.Trim();
    }

    private static bool NotBlank(string value) => !string.IsNullOrWhiteSpace(value);
}

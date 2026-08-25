using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Librariann.API.Services.Metadata;
using Librariann.Common;
using Librariann.Models.DTOs.Metadata;

namespace Librariann.Services.Metadata;

public sealed class EpubMetadataFileWriter : IMetadataFileWriter
{
    private const string ContainerPath = "META-INF/container.xml";
    private const string MimetypePath = "mimetype";
    private static readonly XNamespace DublinCore = "http://purl.org/dc/elements/1.1/";

    public bool CanWrite(string extension) => extension.Equals(".epub", StringComparison.OrdinalIgnoreCase);

    public Task WriteAsync(string temporaryFilePath, MetadataFileUpdate update,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var archive = ZipFile.Open(temporaryFilePath, ZipArchiveMode.Update);
        if (archive.Entries.Count > 100_000) throw new LibrariannException("metadata-archive-has-too-many-entries");
        var opfEntry = FindPackageEntry(archive);
        var package = ReadXml(opfEntry, 16 * 1024 * 1024);
        var metadata = package.Descendants().FirstOrDefault(element => element.Name.LocalName == "metadata")
                       ?? throw new LibrariannException("metadata-epub-package-has-no-metadata");

        Set(metadata, "title", update.Title);
        Set(metadata, "description", update.Description);
        Set(metadata, "language", update.Language);
        Set(metadata, "date", update.PublicationYear is >= 1 and <= 9999
            ? update.PublicationYear.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : null);
        if (update.Authors is not null)
        {
            metadata.Elements().Where(element => element.Name.LocalName == "creator").Remove();
            foreach (var author in update.Authors.Where(value => !string.IsNullOrWhiteSpace(value)))
                metadata.Add(new XElement(DublinCore + "creator", author.Trim()));
        }
        if (update.Publisher is not null) Set(metadata, "publisher", update.Publisher);
        if (update.Isbn is not null) SetIdentifier(metadata, update.Isbn);

        var packagePath = opfEntry.FullName;
        opfEntry.Delete();
        var output = archive.CreateEntry(packagePath, CompressionLevel.Optimal);
        using var stream = output.Open();
        using var writer = XmlWriter.Create(stream, new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = true,
            CloseOutput = false,
        });
        package.Save(writer);
        return Task.CompletedTask;
    }

    public Task ValidateAsync(string temporaryFilePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var archive = ZipFile.OpenRead(temporaryFilePath);
        var mimetype = GetSingleEntry(archive, MimetypePath);
        if (mimetype.Length > 128) throw new LibrariannException("metadata-epub-mimetype-is-invalid");
        using (var reader = new StreamReader(mimetype.Open(), Encoding.ASCII, false, 128, false))
        {
            if (!string.Equals(reader.ReadToEnd().Trim(), "application/epub+zip", StringComparison.Ordinal))
                throw new LibrariannException("metadata-epub-mimetype-is-invalid");
        }
        var package = ReadXml(FindPackageEntry(archive), 16 * 1024 * 1024);
        if (!package.Descendants().Any(element => element.Name.LocalName == "metadata"))
            throw new LibrariannException("metadata-epub-package-has-no-metadata");
        return Task.CompletedTask;
    }

    private static ZipArchiveEntry FindPackageEntry(ZipArchive archive)
    {
        var container = ReadXml(GetSingleEntry(archive, ContainerPath), 1024 * 1024);
        var packagePath = container.Descendants()
            .FirstOrDefault(element => element.Name.LocalName == "rootfile")?
            .Attributes().FirstOrDefault(attribute => attribute.Name.LocalName == "full-path")?.Value;
        if (string.IsNullOrWhiteSpace(packagePath) || packagePath.Split('/', '\\').Any(part => part == ".."))
            throw new LibrariannException("metadata-epub-package-path-is-invalid");
        return GetSingleEntry(archive, packagePath.Replace('\\', '/'));
    }

    private static ZipArchiveEntry GetSingleEntry(ZipArchive archive, string fullName)
    {
        var entries = archive.Entries.Where(entry =>
            string.Equals(entry.FullName, fullName, StringComparison.OrdinalIgnoreCase)).ToArray();
        return entries.Length switch
        {
            1 => entries[0],
            0 => throw new LibrariannException("metadata-epub-required-entry-is-missing"),
            _ => throw new LibrariannException("metadata-epub-has-duplicate-required-entries"),
        };
    }

    private static XDocument ReadXml(ZipArchiveEntry entry, long maximumCharacters)
    {
        if (entry.Length > maximumCharacters) throw new LibrariannException("metadata-epub-xml-entry-is-too-large");
        using var stream = entry.Open();
        using var reader = XmlReader.Create(stream, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = maximumCharacters,
            IgnoreComments = false,
        });
        return XDocument.Load(reader, LoadOptions.PreserveWhitespace);
    }

    private static void Set(XElement metadata, string localName, string? value)
    {
        if (value is null) return;
        var element = metadata.Elements().FirstOrDefault(candidate => candidate.Name.LocalName == localName);
        if (element is null) metadata.Add(new XElement(DublinCore + localName, value.Trim()));
        else element.Value = value.Trim();
    }

    private static void SetIdentifier(XElement metadata, string isbn)
    {
        var value = isbn.Trim();
        var identifier = metadata.Elements().FirstOrDefault(element => element.Name.LocalName == "identifier" &&
            (element.Value.Contains("isbn", StringComparison.OrdinalIgnoreCase) ||
             element.Attributes().Any(attribute => attribute.Value.Contains("isbn", StringComparison.OrdinalIgnoreCase))))
                         ?? metadata.Elements().FirstOrDefault(element => element.Name.LocalName == "identifier");
        if (identifier is null) metadata.Add(new XElement(DublinCore + "identifier", value));
        else identifier.Value = value;
    }
}

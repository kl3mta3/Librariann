using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Services.Metadata;
using Librariann.Common;
using Librariann.Models.DTOs.Metadata;

namespace Librariann.Services.Metadata;

public sealed class MetadataFileWriteCoordinator(IEnumerable<IMetadataFileWriter> writers)
    : IMetadataFileWriteCoordinator
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> FileLocks =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly IReadOnlyCollection<IMetadataFileWriter> _writers = writers.ToArray();

    public async Task<MetadataFileWriteResult> WriteAsync(string filePath, MetadataFileUpdate update,
        CancellationToken cancellationToken = default)
    {
        var sourcePath = Path.GetFullPath(filePath);
        if (!File.Exists(sourcePath)) throw new LibrariannException("metadata-file-does-not-exist");
        if ((File.GetAttributes(sourcePath) & FileAttributes.ReparsePoint) != 0)
            throw new LibrariannException("metadata-file-reparse-points-are-not-supported");

        var writer = _writers.SingleOrDefault(candidate => candidate.CanWrite(Path.GetExtension(sourcePath)))
                     ?? throw new LibrariannException("metadata-file-format-is-not-writable");
        var gate = FileLocks.GetOrAdd(sourcePath, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            return await WriteLockedAsync(sourcePath, writer, update, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    private static async Task<MetadataFileWriteResult> WriteLockedAsync(string sourcePath, IMetadataFileWriter writer,
        MetadataFileUpdate update, CancellationToken cancellationToken)
    {
        var source = new FileInfo(sourcePath);
        if (source.Length <= 0) throw new LibrariannException("metadata-file-is-empty");
        var directory = source.DirectoryName ?? throw new LibrariannException("metadata-file-directory-is-invalid");
        var backupDirectory = Path.Combine(directory, ".librariann-backups");
        Directory.CreateDirectory(backupDirectory);
        var suffix = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}";
        var backupPath = Path.Combine(backupDirectory, $"{source.Name}.{suffix}.bak");
        var temporaryPath = Path.Combine(directory, $".{source.Name}.{suffix}.librariann.tmp");

        File.Copy(sourcePath, backupPath, false);
        if (new FileInfo(backupPath).Length != source.Length)
            throw new IOException("The metadata backup did not match the source file length.");
        File.Copy(sourcePath, temporaryPath, false);
        try
        {
            await writer.WriteAsync(temporaryPath, update, cancellationToken);
            await writer.ValidateAsync(temporaryPath, cancellationToken);
            var updatedLength = new FileInfo(temporaryPath).Length;
            if (updatedLength <= 0) throw new IOException("The rewritten metadata file is empty.");

            // The temporary file lives beside the source so replacement cannot cross filesystems.
            File.Replace(temporaryPath, sourcePath, null, true);
            return new MetadataFileWriteResult(sourcePath, backupPath, updatedLength);
        }
        catch
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            throw;
        }
    }
}

using System.Collections.Generic;

namespace Librariann.Models.DTOs.Metadata;

public sealed record MetadataFileUpdate
{
    public string? Title { get; init; }
    public string? Series { get; init; }
    public string? Description { get; init; }
    public string? Language { get; init; }
    public int? PublicationYear { get; init; }
    public IReadOnlyCollection<string>? Authors { get; init; }
    public IReadOnlyCollection<string>? Genres { get; init; }
    public string? Isbn { get; init; }
    public string? Publisher { get; init; }
}

public sealed record MetadataFileWriteResult(string FilePath, string BackupPath, long BytesWritten);

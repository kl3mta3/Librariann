using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs.Metadata;

namespace Librariann.API.Services.Metadata;

public interface IMetadataFileWriter
{
    bool CanWrite(string extension);
    Task WriteAsync(string temporaryFilePath, MetadataFileUpdate update,
        CancellationToken cancellationToken = default);
    Task ValidateAsync(string temporaryFilePath, CancellationToken cancellationToken = default);
}

public interface IMetadataFileWriteCoordinator
{
    Task<MetadataFileWriteResult> WriteAsync(string filePath, MetadataFileUpdate update,
        CancellationToken cancellationToken = default);
}

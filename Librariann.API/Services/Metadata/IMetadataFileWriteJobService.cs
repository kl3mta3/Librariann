using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs.Metadata;

namespace Librariann.API.Services.Metadata;

public interface IMetadataFileWriteJobService
{
    Task WriteSeriesFilesAsync(int seriesId, MetadataFileUpdate update,
        CancellationToken cancellationToken = default);
}

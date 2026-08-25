using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs.Metadata;
using Librariann.Models.Entities.Metadata;

namespace Librariann.API.Services.Metadata;

public interface IMetadataProvenanceService
{
    Task<IReadOnlyCollection<MetadataProvenanceDto>> GetAllAsync(MetadataEntityType entityType, int entityId,
        CancellationToken cancellationToken = default);
    Task<MetadataRefreshPermission> CanRefreshAsync(MetadataEntityType entityType, int entityId,
        MetadataFieldKey field, string providerKey, CancellationToken cancellationToken = default);
    Task StageAsync(RecordMetadataProvenanceRequest request, CancellationToken cancellationToken = default);
    Task<MetadataProvenanceDto> RecordAsync(RecordMetadataProvenanceRequest request,
        CancellationToken cancellationToken = default);
}

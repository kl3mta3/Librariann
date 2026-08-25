using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.Entities.Metadata;

namespace Librariann.API.Repositories;

public interface IMetadataFieldProvenanceRepository
{
    Task<IReadOnlyList<MetadataFieldProvenance>> GetAllAsync(MetadataEntityType entityType, int entityId,
        CancellationToken cancellationToken = default);
    Task<MetadataFieldProvenance?> GetAsync(MetadataEntityType entityType, int entityId, MetadataFieldKey field,
        CancellationToken cancellationToken = default);
    void Add(MetadataFieldProvenance provenance);
}

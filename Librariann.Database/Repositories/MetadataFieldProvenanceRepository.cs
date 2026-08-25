using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Repositories;
using Librariann.Models.Entities.Metadata;
using Microsoft.EntityFrameworkCore;

namespace Librariann.Database.Repositories;

public sealed class MetadataFieldProvenanceRepository(DataContext context) : IMetadataFieldProvenanceRepository
{
    public async Task<IReadOnlyList<MetadataFieldProvenance>> GetAllAsync(MetadataEntityType entityType, int entityId,
        CancellationToken cancellationToken = default) => await context.MetadataFieldProvenances.AsNoTracking()
        .Where(item => item.EntityType == entityType && item.EntityId == entityId)
        .OrderBy(item => item.Field)
        .ToListAsync(cancellationToken);

    public Task<MetadataFieldProvenance?> GetAsync(MetadataEntityType entityType, int entityId, MetadataFieldKey field,
        CancellationToken cancellationToken = default) => context.MetadataFieldProvenances.FirstOrDefaultAsync(
        item => item.EntityType == entityType && item.EntityId == entityId && item.Field == field, cancellationToken);

    public void Add(MetadataFieldProvenance provenance) => context.MetadataFieldProvenances.Add(provenance);
}

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Repositories;
using Librariann.Models.Entities.Acquisition;
using Microsoft.EntityFrameworkCore;

namespace Librariann.Database.Repositories;

public sealed class QualityProfileRepository(DataContext context) : IQualityProfileRepository
{
    public async Task<IReadOnlyList<QualityProfile>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.QualityProfiles.AsNoTracking().OrderBy(profile => profile.MediaType).ThenBy(profile => profile.Name)
            .ToListAsync(cancellationToken);

    public Task<QualityProfile?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        context.QualityProfiles.FirstOrDefaultAsync(profile => profile.Id == id, cancellationToken);

    public void Add(QualityProfile profile) => context.QualityProfiles.Add(profile);
    public void Remove(QualityProfile profile) => context.QualityProfiles.Remove(profile);
}

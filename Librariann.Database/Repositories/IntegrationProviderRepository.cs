using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Repositories;
using Librariann.Models.Entities.Acquisition;
using Microsoft.EntityFrameworkCore;

namespace Librariann.Database.Repositories;

public sealed class IntegrationProviderRepository(DataContext context) : IIntegrationProviderRepository
{
    public async Task<IReadOnlyList<IntegrationProviderConfiguration>> GetAllAsync(CancellationToken ct = default) =>
        await context.IntegrationProviderConfigurations.AsNoTracking().OrderBy(provider => provider.Category)
            .ThenBy(provider => provider.Name).ToListAsync(ct);

    public Task<IntegrationProviderConfiguration?> GetAsync(int id, CancellationToken ct = default) =>
        context.IntegrationProviderConfigurations.FirstOrDefaultAsync(provider => provider.Id == id, ct);

    public void Add(IntegrationProviderConfiguration configuration) => context.Add(configuration);
    public void Update(IntegrationProviderConfiguration configuration) => context.Entry(configuration).State = EntityState.Modified;
    public void Remove(IntegrationProviderConfiguration configuration) => context.Remove(configuration);
}

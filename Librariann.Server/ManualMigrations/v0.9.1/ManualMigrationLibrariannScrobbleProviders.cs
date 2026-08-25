using System.Linq;
using System.Threading.Tasks;
using Librariann.Database;
using Librariann.Models.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Librariann.Server.ManualMigrations.v0._9._1;

/// <summary>
/// Librariann can have some ScrobbleEvents with Librariann as the provider. These should be rewritten to AniList
/// </summary>
public class ManualMigrationLibrariannScrobbleProviders : ManualMigration
{
    protected override string MigrationName => nameof(ManualMigrationLibrariannScrobbleProviders);
    protected override async Task ExecuteAsync(DataContext context, ILogger<Program> logger)
    {
        await context.ScrobbleEvent.Where(s => s.ScrobbleProvider == ScrobbleProvider.Librariann)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.ScrobbleProvider, ScrobbleProvider.AniList));
    }
}

using System.Linq;
using System.Threading.Tasks;
using Librariann.Database;
using Librariann.Models.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Librariann.Server.ManualMigrations.v0._9._1;

public class ManualMigrationMetadataProvider: ManualMigration
{
    protected override string MigrationName => nameof(ManualMigrationMetadataProvider);
    protected override async Task ExecuteAsync(DataContext context, ILogger<Program> logger)
    {
        // Cbr for Comic libraries
        await context.ExternalSeriesMetadata
            .Where(m => m.Series.Library.Type == LibraryType.Comic || m.Series.Library.Type == LibraryType.ComicVine)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.Provider, MetadataProvider.ComicBookRoundup));

        // MB for others
        await context.ExternalSeriesMetadata
            .Where(m => m.Series.Library.Type != LibraryType.Comic && m.Series.Library.Type != LibraryType.ComicVine)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.Provider, MetadataProvider.Mangabaka));
    }
}

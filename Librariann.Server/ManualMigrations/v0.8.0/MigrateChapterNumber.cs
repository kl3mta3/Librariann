using System;
using System.Threading.Tasks;
using Librariann.Common.Constants;
using Librariann.Common.EnvironmentInfo;
using Librariann.Database;
using Librariann.Models.Constants;
using Librariann.Models.Entities.History;
using Librariann.Services.Scanner;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Librariann.Server.ManualMigrations.v0._8._0;

/// <summary>
/// Introduced in v0.8.0, this migrates the existing Chapter Range -> Chapter Min/Max Number
/// </summary>
public static class MigrateChapterNumber
{
    public static async Task Migrate(DataContext dataContext, ILogger<Program> logger)
    {
        if (await dataContext.ManualMigrationHistory.AnyAsync(m => m.Name == "MigrateChapterNumber"))
        {
            return;
        }

        logger.LogCritical(
            "Running MigrateChapterNumber migration - Please be patient, this may take some time. This is not an error");

        // Get all volumes
        foreach (var chapter in dataContext.Chapter)
        {
            if (chapter.IsSpecial)
            {
                chapter.MinNumber = ParserConstants.DefaultChapterNumber;
                chapter.MaxNumber = ParserConstants.DefaultChapterNumber;
                continue;
            }
            chapter.MinNumber = Parser.MinNumberFromRange(chapter.Range);
            chapter.MaxNumber = Parser.MaxNumberFromRange(chapter.Range);
        }

        dataContext.ManualMigrationHistory.Add(new ManualMigrationHistory()
        {
            Name = "MigrateChapterNumber",
            ProductVersion = BuildInfo.Version.ToString(),
            RanAt = DateTime.UtcNow
        });

        await dataContext.SaveChangesAsync();
        logger.LogCritical(
            "Running MigrateChapterNumber migration - Completed. This is not an error");
    }
}

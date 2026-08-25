using System.IO;
using System;
using System.Linq;
using System.Threading.Tasks;
using Librariann.API.Services;
using Microsoft.Extensions.Logging;

namespace Librariann.Server.ManualMigrations.v0._7._14;

public static class MigrateEmailTemplates
{
    private static readonly string[] TemplateNames =
    [
        "EmailChange.html",
        "EmailConfirm.html",
        "EmailPasswordReset.html",
        "SendToDevice.html",
        "EmailTest.html"
    ];

    public static async Task Migrate(IDirectoryService directoryService, ILogger<Program> logger)
    {
        var files = directoryService.GetFiles(directoryService.CustomizedTemplateDirectory);
        if (files.Any())
        {
            return;
        }

        logger.LogCritical("Running MigrateEmailTemplates migration - Please be patient, this may take some time. This is not an error");

        // Copy packaged templates so migration never depends on an upstream service.
        foreach (var templateName in TemplateNames)
        {
            await CopyPackagedTemplate(templateName,
                Path.Join(directoryService.CustomizedTemplateDirectory, templateName), logger);
        }


        logger.LogCritical("Running MigrateEmailTemplates migration - Completed. This is not an error");
    }

    private static async Task CopyPackagedTemplate(string templateName, string filePath, ILogger<Program> logger)
    {
        var sourcePath = Path.Join(AppContext.BaseDirectory, "EmailTemplates", templateName);
        if (!File.Exists(sourcePath))
        {
            logger.LogError("Packaged email template {Template} was not found at {SourcePath}", templateName, sourcePath);
            return;
        }

        var content = await File.ReadAllTextAsync(sourcePath);
        await File.WriteAllTextAsync(filePath, content);
        logger.LogInformation("Packaged email template {Template} was copied successfully", templateName);
    }


}

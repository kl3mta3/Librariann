using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Librariann.API.Database;
using Librariann.API.Services;
using Librariann.API.Services.SignalR;
using Librariann.Common.EnvironmentInfo;
using Librariann.Models.DTOs.SignalR;
using Librariann.Models.Entities.Enums;
using Librariann.Services.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Librariann.Services;

public class BackupService(
    ILogger<BackupService> logger,
    IUnitOfWork unitOfWork,
    IDirectoryService directoryService,
    IEventHub eventHub)
    : IBackupService
{
    public const string BackupPassphraseEnvironmentVariable = "LIBRARIANN_BACKUP_PASSPHRASE";

    private readonly IList<string> _backupFiles =
    [
        "appsettings.json"
    ];

    /// <summary>
    /// Returns a list of all log files for Librariann
    /// </summary>
    /// <param name="rollFiles">If file rolling is enabled. Defaults to True.</param>
    /// <returns></returns>
    public IEnumerable<string> GetLogFiles(bool rollFiles = true)
    {
        var multipleFileRegex = rollFiles ? @"\d*" : string.Empty;
        var fi = directoryService.FileSystem.FileInfo.New(IBackupService.LogFile);

        var files = rollFiles
            ? directoryService.GetFiles(directoryService.LogDirectory,
                $@"{directoryService.FileSystem.Path.GetFileNameWithoutExtension(fi.Name)}{multipleFileRegex}\.log")
            : [directoryService.FileSystem.Path.Join(directoryService.LogDirectory, "librariann.log")];
        return files;
    }

    /// <summary>
    /// Will back up anything that needs to be backed up. This includes logs, setting files, bare minimum cover images (just locked and first cover).
    /// </summary>
    /// <param name="ct"></param>
    [AutomaticRetry(Attempts = 3, LogEvents = false, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public async Task BackupDatabase(CancellationToken ct = default)
    {
        logger.LogInformation("Beginning backup of Database at {BackupTime}", DateTime.Now);
        var backupDirectory = (await unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.BackupDirectory)).Value;

        logger.LogDebug("Backing up to {BackupDirectory}", backupDirectory);
        if (!directoryService.ExistOrCreate(backupDirectory))
        {
            logger.LogCritical("Could not write to {BackupDirectory}; aborting backup", backupDirectory);
            await eventHub.SendMessageAsync(MessageFactory.Error,
                MessageFactory.ErrorEvent("Backup Service Error",$"Could not write to {backupDirectory}; aborting backup"), ct: ct);
            return;
        }

        await SendProgress(0F, "Started backup", ct);
        await SendProgress(0.1F, "Copying core files", ct);

        var dateString = $"{DateTime.UtcNow.ToShortDateString()}_{DateTime.UtcNow:s}Z".Replace("/", "_").Replace(":", "_");
        var passphrase = Environment.GetEnvironmentVariable(BackupPassphraseEnvironmentVariable);
        if (!string.IsNullOrEmpty(passphrase) && passphrase.Length < 16)
        {
            throw new InvalidOperationException($"{BackupPassphraseEnvironmentVariable} must contain at least 16 characters");
        }

        var encryptArchive = !string.IsNullOrEmpty(passphrase);
        var extension = encryptArchive ? ".librariann-backup" : ".zip";
        var archivePath = directoryService.FileSystem.Path.Join(backupDirectory,
            $"librariann_backup_v{BuildInfo.Version}_{dateString}{extension}");
        var workingZipPath = encryptArchive
            ? directoryService.FileSystem.Path.Join(directoryService.TempDirectory, $"{dateString}.backup.zip")
            : archivePath;

        if (File.Exists(archivePath) || File.Exists(workingZipPath))
        {
            logger.LogCritical("{ArchiveFile} already exists, aborting", archivePath);
            await eventHub.SendMessageAsync(MessageFactory.Error,
                MessageFactory.ErrorEvent("Backup Service Error",$"{archivePath} already exists, aborting"), ct: ct);
            return;
        }

        var tempDirectory = Path.Join(directoryService.TempDirectory, dateString);
        directoryService.ExistOrCreate(tempDirectory);
        directoryService.ClearDirectory(tempDirectory);

        await SendProgress(0.1F, "Backing up database", ct);
        await BackupDatabaseFile(tempDirectory);

        await SendProgress(0.15F, "Copying config files", ct);
        await CopyConfigurationToBackup(tempDirectory, encryptArchive, ct);

        // Copy any csv's as those are used for manual migrations
        directoryService.CopyFilesToDirectory(
            directoryService.GetFilesWithCertainExtensions(directoryService.ConfigDirectory, @"\.csv"), tempDirectory);

        if (encryptArchive)
        {
            await SendProgress(0.2F, "Copying logs", ct);
            CopyLogsToBackupDirectory(tempDirectory);
        }
        else
        {
            logger.LogWarning("Creating a recovery-safe backup without logs or credential recovery keys. Set {Variable} to create a fully encrypted portable backup",
                BackupPassphraseEnvironmentVariable);
        }

        await SendProgress(0.25F, "Copying cover images", ct);
        await CopyCoverImagesToBackupDirectory(tempDirectory);

        await SendProgress(0.35F, "Copying templates images", ct);
        CopyTemplatesToBackupDirectory(tempDirectory);

        await SendProgress(0.5F, "Copying bookmarks", ct);
        await CopyBookmarksToBackupDirectory(tempDirectory);

        await SendProgress(0.6F, "Copying Fonts", ct);
        CopyFontsToBackupDirectory(tempDirectory);

        await SendProgress(0.85F, "Copying favicons", ct);
        CopyFaviconsToBackupDirectory(tempDirectory);

        try
        {
            await ZipFile.CreateFromDirectoryAsync(tempDirectory, workingZipPath, ct);
            if (encryptArchive)
            {
                await BackupArchiveEncryption.EncryptAsync(workingZipPath, archivePath, passphrase!, ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "There was an issue when archiving library backup");
            if (File.Exists(archivePath)) File.Delete(archivePath);
            throw;
        }
        finally
        {
            if (encryptArchive && File.Exists(workingZipPath)) File.Delete(workingZipPath);
            directoryService.ClearAndDeleteDirectory(tempDirectory);
        }

        logger.LogInformation("Database backup completed at {ArchivePath} (encrypted: {Encrypted})", archivePath,
            encryptArchive);
        await SendProgress(1F, "Completed backup", ct);
    }

    private async Task CopyConfigurationToBackup(string tempDirectory, bool includeSecrets, CancellationToken ct)
    {
        var appSettingsPath = directoryService.FileSystem.Path.Join(directoryService.ConfigDirectory,
            _backupFiles[0]);
        var destinationPath = directoryService.FileSystem.Path.Join(tempDirectory, _backupFiles[0]);

        if (includeSecrets)
        {
            directoryService.CopyFilesToDirectory([appSettingsPath], tempDirectory);
            var dataProtectionKeys = directoryService.FileSystem.Path.Join(directoryService.ConfigDirectory,
                "data-protection-keys");
            if (directoryService.FileSystem.Directory.Exists(dataProtectionKeys))
            {
                directoryService.CopyDirectoryToDirectory(dataProtectionKeys,
                    directoryService.FileSystem.Path.Join(tempDirectory, "data-protection-keys"));
            }
        }
        else
        {
            try
            {
                var root = JsonNode.Parse(await File.ReadAllTextAsync(appSettingsPath, ct))?.AsObject()
                           ?? new JsonObject();
                root["TokenKey"] = "REDACTED_REGENERATE_ON_RESTORE";
                if (root["OpenIdConnectSettings"] is JsonObject oidc)
                {
                    oidc["Secret"] = string.Empty;
                }

                await File.WriteAllTextAsync(destinationPath,
                    root.ToJsonString(new JsonSerializerOptions {WriteIndented = true}), ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not create sanitized appsettings backup; the file was omitted");
            }
        }

        var manifest = new
        {
            Product = "Librariann",
            Version = BuildInfo.Version.ToString(),
            CreatedUtc = DateTime.UtcNow,
            Encrypted = includeSecrets,
            IncludesCredentialRecoveryKeys = includeSecrets,
            RequiresCredentialReentry = !includeSecrets
        };
        await File.WriteAllTextAsync(Path.Join(tempDirectory, "backup-manifest.json"),
            JsonSerializer.Serialize(manifest, new JsonSerializerOptions {WriteIndented = true}), ct);
    }

    private void CopyLogsToBackupDirectory(string tempDirectory)
    {
        var files = GetLogFiles();
        directoryService.CopyFilesToDirectory(files, directoryService.FileSystem.Path.Join(tempDirectory, "logs"));
    }

    /// <summary>
    /// Creates a backup of the SQLite database using VACUUM INTO command.
    /// This method safely backs up the database while it's in use, without locking issues.
    /// </summary>
    /// <param name="tempDirectory">The directory where the backup file will be created</param>
    private async Task BackupDatabaseFile(string tempDirectory)
    {
        var backupPath = directoryService.FileSystem.Path.Join(tempDirectory, "librariann.db");

        // Validate the backup path to prevent SQL injection
        // The path must not contain single quotes which could break the SQL command
        if (backupPath.Contains('\''))
        {
            throw new ArgumentException("Backup path contains invalid characters", nameof(tempDirectory));
        }

        try
        {
            // Use VACUUM INTO to create a safe backup of the database while it's running
            // This creates a consistent snapshot without locking the main database
            // Note: VACUUM INTO requires a literal path and cannot use SQL parameters
            #pragma warning disable EF1002 // The backup path is validated above to not contain SQL injection characters
            await unitOfWork.DataContext.Database.ExecuteSqlRawAsync($"VACUUM INTO '{backupPath}'");
            #pragma warning restore EF1002
            logger.LogDebug("Database backup created successfully at {BackupPath}", backupPath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create database backup using VACUUM INTO at {BackupPath}", backupPath);
            throw new InvalidOperationException($"Failed to create database backup at {backupPath}", ex);
        }
    }

    private void CopyFaviconsToBackupDirectory(string tempDirectory)
    {
        directoryService.CopyDirectoryToDirectory(directoryService.FaviconDirectory, directoryService.FileSystem.Path.Join(tempDirectory, "favicons"));
    }

    private void CopyTemplatesToBackupDirectory(string tempDirectory)
    {
        directoryService.CopyDirectoryToDirectory(directoryService.TemplateDirectory, directoryService.FileSystem.Path.Join(tempDirectory, "templates"));
    }

    private async Task CopyCoverImagesToBackupDirectory(string tempDirectory)
    {
        var outputTempDir = Path.Join(tempDirectory, "covers");
        directoryService.ExistOrCreate(outputTempDir);

        try
        {
            var seriesImages = await unitOfWork.SeriesRepository.GetLockedCoverImagesAsync();
            directoryService.CopyFilesToDirectory(
                seriesImages.Select(s => directoryService.FileSystem.Path.Join(directoryService.CoverImageDirectory, s)), outputTempDir);

            var collectionTags = await unitOfWork.CollectionTagRepository.GetAllCoverImagesAsync();
            directoryService.CopyFilesToDirectory(
                collectionTags.Select(s => directoryService.FileSystem.Path.Join(directoryService.CoverImageDirectory, s)), outputTempDir);

            var chapterImages = await unitOfWork.ChapterRepository.GetCoverImagesForLockedChaptersAsync();
            directoryService.CopyFilesToDirectory(
                chapterImages.Select(s => directoryService.FileSystem.Path.Join(directoryService.CoverImageDirectory, s)), outputTempDir);

            var volumeImages = await unitOfWork.VolumeRepository.GetCoverImagesForLockedVolumesAsync();
            directoryService.CopyFilesToDirectory(
                volumeImages.Select(s => directoryService.FileSystem.Path.Join(directoryService.CoverImageDirectory, s)), outputTempDir);

            var libraryImages = await unitOfWork.LibraryRepository.GetAllCoverImagesAsync();
            directoryService.CopyFilesToDirectory(
                libraryImages.Select(s => directoryService.FileSystem.Path.Join(directoryService.CoverImageDirectory, s)), outputTempDir);

            var readingListImages = await unitOfWork.ReadingListRepository.GetAllCoverImagesAsync();
            directoryService.CopyFilesToDirectory(
                readingListImages.Select(s => directoryService.FileSystem.Path.Join(directoryService.CoverImageDirectory, s)), outputTempDir);
        }
        catch (IOException)
        {
            // Swallow exception. This can be a duplicate cover being copied as chapter and volumes can share same file.
        }

        if (!directoryService.GetFiles(outputTempDir, searchOption: SearchOption.AllDirectories).Any())
        {
            directoryService.ClearAndDeleteDirectory(outputTempDir);
        }
    }

    private async Task CopyBookmarksToBackupDirectory(string tempDirectory)
    {
        var bookmarkDirectory =
            (await unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.BookmarkDirectory)).Value;

        var outputTempDir = Path.Join(tempDirectory, "bookmarks");
        directoryService.ExistOrCreate(outputTempDir);

        try
        {
            directoryService.CopyDirectoryToDirectory(bookmarkDirectory, outputTempDir);
        }
        catch (IOException)
        {
            // Swallow exception.
        }

        if (!directoryService.GetFiles(outputTempDir, searchOption: SearchOption.AllDirectories).Any())
        {
            directoryService.ClearAndDeleteDirectory(outputTempDir);
        }
    }

    private void CopyFontsToBackupDirectory(string tempDirectory)
    {
        var outputTempDir = Path.Join(tempDirectory, "fonts");
        directoryService.ExistOrCreate(outputTempDir);

        try
        {
            directoryService.CopyDirectoryToDirectory(directoryService.EpubFontDirectory, outputTempDir);
        }
        catch (IOException ex)
        {
            logger.LogWarning(ex, "Failed to copy fonts to backup directory '{OutputTempDir}'. Fonts will not be included in the backup.", outputTempDir);
        }

        if (!directoryService.GetFiles(outputTempDir, searchOption: SearchOption.AllDirectories).Any())
        {
            directoryService.ClearAndDeleteDirectory(outputTempDir);
        }
    }

    private async Task SendProgress(float progress, string subtitle, CancellationToken ct = default)
    {
        await eventHub.SendMessageAsync(MessageFactory.NotificationProgress,
            MessageFactory.BackupDatabaseProgressEvent(progress, subtitle), ct: ct);
    }

}

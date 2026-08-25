using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Librariann.API.Database;
using Librariann.API.Repositories;
using Librariann.API.Services;
using Librariann.API.Services.Acquisition;
using Librariann.Common;
using Librariann.Models.DTOs.Acquisition;
using Librariann.Models.Entities.Enums;
using Librariann.Models.Entities.Acquisition;
using Microsoft.Extensions.Logging;

namespace Librariann.Services.Acquisition;

public sealed class AcquisitionImportService(
    IUnitOfWork unitOfWork,
    ITaskScheduler taskScheduler,
    ILogger<AcquisitionImportService> logger) : IAcquisitionImportService
{
    private const int MaximumCandidates = 5000;
    private const int CopyBufferSize = 1024 * 1024;

    public async Task<IReadOnlyCollection<ImportDestinationOption>> GetDestinationsAsync(CancellationToken cancellationToken = default)
    {
        var libraries = await unitOfWork.LibraryRepository.GetLibrariesAsync(
            LibraryIncludes.Folders | LibraryIncludes.FileTypes, false, cancellationToken);
        return libraries
            .OrderBy(library => library.Name, StringComparer.OrdinalIgnoreCase)
            .SelectMany(library => library.Folders.OrderBy(folder => folder.Path, StringComparer.OrdinalIgnoreCase)
                .Select(folder => new ImportDestinationOption(library.Id, library.Name, folder.Id, folder.Path,
                    SupportedFormats(library.LibraryFileTypes.Select(group => group.FileTypeGroup)))))
            .ToArray();
    }

    public async Task<ImportAnalysisResult> AnalyzeAsync(int downloadId, CancellationToken cancellationToken = default)
    {
        var download = await unitOfWork.AcquisitionDownloadRepository.GetAsync(downloadId, cancellationToken)
                       ?? throw new LibrariannException("acquisition-download-does-not-exist");
        var provider = await unitOfWork.IntegrationProviderRepository.GetAsync(download.IntegrationProviderConfigurationId, cancellationToken)
                       ?? throw new LibrariannException("download-client-does-not-exist");
        if (string.IsNullOrWhiteSpace(provider.LocalPath) || string.IsNullOrWhiteSpace(download.OutputPath))
            return await MarkAsync(download, [], true, "A valid local path mapping is required before import.", cancellationToken);

        var root = Path.GetFullPath(provider.LocalPath);
        var output = Path.GetFullPath(download.OutputPath);
        if (!IsWithin(root, output))
            return await MarkAsync(download, [], true, "The completed path is outside the configured import root.", cancellationToken);

        IReadOnlyCollection<ImportCandidate> candidates;
        try
        {
            candidates = Discover(output, root, cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return await MarkAsync(download, [], true, "Librariann could not safely inspect the completed download.", cancellationToken);
        }

        if (candidates.Count == 0)
            return await MarkAsync(download, candidates, true, "No supported reading-media files were found.", cancellationToken);
        var manual = candidates.Count != 1;
        return await MarkAsync(download, candidates, manual,
            manual ? "Multiple media files require manual matching." : "One import candidate is ready for matching.", cancellationToken);
    }

    /// <summary>
    /// Advances monitored downloads only when both the source and destination are unambiguous. Interactive grabs,
    /// author targets without an existing library series, multi-file releases, and unsafe paths remain manual.
    /// </summary>
    [DisableConcurrentExecution(timeoutInSeconds: 15 * 60)]
    public async Task ProcessPendingAutomaticImportsAsync(CancellationToken cancellationToken = default)
    {
        var downloads = await unitOfWork.AcquisitionDownloadRepository
            .GetPendingAutomaticImportsAsync(cancellationToken);
        foreach (var download in downloads)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var analysis = await AnalyzeAsync(download.Id, cancellationToken);
                if (analysis.NeedsManualMatch || analysis.Candidates.Count != 1) continue;

                var target = download.MonitoringTargetId.HasValue
                    ? await unitOfWork.MonitoringRepository.GetAsync(download.MonitoringTargetId.Value,
                        cancellationToken)
                    : null;
                if (target?.LibrarySeriesId is null)
                {
                    await RequireManualMatchAsync(download,
                        "Automatic import needs an existing library series destination.", cancellationToken);
                    continue;
                }
                var wanted = download.WantedItemId.HasValue
                    ? (await unitOfWork.MonitoringRepository.GetWantedAsync(target.Id, cancellationToken))
                        .SingleOrDefault(item => item.Id == download.WantedItemId.Value)
                    : null;

                var series = await unitOfWork.SeriesRepository.GetSeriesByIdAsync(target.LibrarySeriesId.Value,
                    SeriesIncludes.None, cancellationToken);
                if (series is null)
                {
                    await RequireManualMatchAsync(download,
                        "The monitored library series no longer exists.", cancellationToken);
                    continue;
                }

                var library = await unitOfWork.LibraryRepository.GetLibraryForIdAsync(series.LibraryId,
                    LibraryIncludes.Folders | LibraryIncludes.FileTypes, cancellationToken);
                var seriesDirectory = ExistingSeriesDirectory(series.FolderPath, series.LowestFolderPath);
                var folder = library?.Folders
                    .Where(candidate => IsWithin(Path.GetFullPath(candidate.Path), seriesDirectory))
                    .OrderByDescending(candidate => Path.GetFullPath(candidate.Path).Length)
                    .FirstOrDefault();
                var candidate = analysis.Candidates.Single();
                if (library is null || folder is null ||
                    !SupportedFormats(library.LibraryFileTypes.Select(group => group.FileTypeGroup))
                        .Contains(candidate.Format))
                {
                    await RequireManualMatchAsync(download,
                        "Librariann could not determine one compatible library destination.", cancellationToken);
                    continue;
                }

                var relativeDirectory = Path.GetRelativePath(Path.GetFullPath(folder.Path), seriesDirectory);
                await CommitAsync(new CommitImportRequest
                {
                    DownloadId = download.Id,
                    LibraryId = library.Id,
                    FolderId = folder.Id,
                    CandidateRelativePath = candidate.RelativePath,
                    DestinationSubdirectory = relativeDirectory == "." ? string.Empty : relativeDirectory,
                    DestinationBaseName = AutomaticBaseName(wanted, candidate.FileName),
                }, cancellationToken);

                if (wanted is not null)
                {
                    wanted.Status = WantedItemStatus.Owned;
                    wanted.LibrarySeriesId = series.Id;
                    wanted.LastSearchSummary = "Imported automatically into the monitored library series.";
                    await unitOfWork.CommitAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Automatic import could not complete acquisition download {DownloadId}",
                    download.Id);
                if (download.Status is AcquisitionDownloadStatus.ImportPending or AcquisitionDownloadStatus.Importing)
                    await RequireManualMatchAsync(download,
                        "Automatic import failed. Review the candidate and destination manually.", CancellationToken.None);
            }
        }
    }

    /// <summary>
    /// Connects completed manual imports to the series created or updated by the subsequent library scan.
    /// Keeping this as a retryable job also repairs imports when a scan was temporarily unavailable.
    /// </summary>
    [DisableConcurrentExecution(timeoutInSeconds: 15 * 60)]
    public async Task ReconcileImportedSeriesAsync(CancellationToken cancellationToken = default)
    {
        var downloads = await unitOfWork.AcquisitionDownloadRepository
            .GetPendingSeriesReconciliationAsync(cancellationToken);
        foreach (var download in downloads)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var series = await unitOfWork.SeriesRepository.GetSeriesThatContainsLowestFolderPathAsync(
                download.ImportedPath, SeriesIncludes.None, cancellationToken);
            if (series is null) continue;

            download.ImportedSeriesId = series.Id;
        }

        if (downloads.Any(download => download.ImportedSeriesId.HasValue))
            await unitOfWork.CommitAsync(cancellationToken);

        // This also recovers known-series imports if their original Hangfire continuation was interrupted.
        foreach (var download in await unitOfWork.AcquisitionDownloadRepository
                     .GetPendingMetadataRefreshAsync(cancellationToken))
        {
            await QueueImportedSeriesMetadataRefreshAsync(download.Id, cancellationToken);
        }
    }

    /// <summary>
    /// Queues the normal scanner-owned metadata/cover refresh after an import scan. External provider candidates
    /// are not auto-applied here because edition selection must remain explicit and provenance-aware.
    /// </summary>
    public async Task QueueImportedSeriesMetadataRefreshAsync(int downloadId,
        CancellationToken cancellationToken = default)
    {
        var download = await unitOfWork.AcquisitionDownloadRepository.GetAsync(downloadId, cancellationToken);
        if (download is null || download.Status != AcquisitionDownloadStatus.Imported ||
            !download.ImportedSeriesId.HasValue || download.MetadataRefreshQueuedAtUtc.HasValue) return;

        var series = await unitOfWork.SeriesRepository.GetSeriesByIdAsync(download.ImportedSeriesId.Value,
            SeriesIncludes.None, cancellationToken);
        if (series is null) return;

        await taskScheduler.RefreshSeriesMetadata(series.LibraryId, series.Id, true, true);
        download.MetadataRefreshQueuedAtUtc = DateTime.UtcNow;
        await unitOfWork.CommitAsync(cancellationToken);
    }

    public async Task<CommitImportResult> CommitAsync(CommitImportRequest request, CancellationToken cancellationToken = default)
    {
        var download = await unitOfWork.AcquisitionDownloadRepository.GetAsync(request.DownloadId, cancellationToken)
                       ?? throw new LibrariannException("acquisition-download-does-not-exist");
        if (download.Status is not (AcquisitionDownloadStatus.ImportPending or AcquisitionDownloadStatus.NeedsManualMatch))
            throw new LibrariannException("acquisition-download-is-not-ready-for-import");

        var provider = await unitOfWork.IntegrationProviderRepository.GetAsync(download.IntegrationProviderConfigurationId, cancellationToken)
                       ?? throw new LibrariannException("download-client-does-not-exist");
        var library = await unitOfWork.LibraryRepository.GetLibraryForIdAsync(request.LibraryId,
                          LibraryIncludes.Folders | LibraryIncludes.FileTypes, cancellationToken)
                      ?? throw new LibrariannException("library-does-not-exist");
        var folder = library.Folders.SingleOrDefault(candidate => candidate.Id == request.FolderId)
                     ?? throw new LibrariannException("library-folder-does-not-exist");

        var source = ResolveSource(provider.LocalPath, download.OutputPath, request.CandidateRelativePath);
        var sourceInfo = new FileInfo(source);
        if (!sourceInfo.Exists || (sourceInfo.Attributes & FileAttributes.ReparsePoint) != 0)
            throw new LibrariannException("import-candidate-is-not-a-safe-file");
        var format = GetFormat(sourceInfo.Extension);
        if (format == AcquisitionMediaFormat.Unknown ||
            !SupportedFormats(library.LibraryFileTypes.Select(group => group.FileTypeGroup)).Contains(format))
            throw new LibrariannException("import-format-is-not-supported-by-library");

        var targetRoot = Path.GetFullPath(folder.Path);
        if (!Directory.Exists(targetRoot)) throw new LibrariannException("library-folder-is-not-accessible");
        var targetDirectory = ResolveTargetDirectory(targetRoot, request.DestinationSubdirectory);
        EnsureNoChildReparsePoints(targetRoot, targetDirectory);
        var baseName = ValidatedBaseName(request.DestinationBaseName, sourceInfo.Name);
        var targetPath = Path.Combine(targetDirectory, baseName + sourceInfo.Extension.ToLowerInvariant());
        if (!IsWithin(targetRoot, targetPath) || File.Exists(targetPath) || Directory.Exists(targetPath))
            throw new LibrariannException("import-destination-already-exists-or-is-invalid");

        download.Status = AcquisitionDownloadStatus.Importing;
        download.ErrorMessage = string.Empty;
        await unitOfWork.CommitAsync(cancellationToken);

        string? temporaryPath = null;
        var placed = false;
        try
        {
            Directory.CreateDirectory(targetDirectory);
            EnsureNoChildReparsePoints(targetRoot, targetDirectory);
            temporaryPath = Path.Combine(targetDirectory, $".librariann-import-{Guid.NewGuid():N}.tmp");
            await CopyAndValidateAsync(sourceInfo, temporaryPath, cancellationToken);
            File.Move(temporaryPath, targetPath, false);
            temporaryPath = null;
            placed = true;

            download.ImportedPath = targetPath;
            download.ImportedSeriesId = await ResolveKnownSeriesIdAsync(download, cancellationToken);
            download.ImportedAtUtc = DateTime.UtcNow;
            download.Status = AcquisitionDownloadStatus.Imported;
            download.ErrorMessage = string.Empty;
            await unitOfWork.CommitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            DeleteTemporary(temporaryPath);
            await MarkCommitFailureAsync(download, placed, CancellationToken.None);
            throw;
        }
        catch (Exception exception)
        {
            DeleteTemporary(temporaryPath);
            logger.LogError(exception, "Unable to import acquisition download {DownloadId}", download.Id);
            await MarkCommitFailureAsync(download, placed, CancellationToken.None);
            throw new LibrariannException(placed
                ? "import-file-was-placed-but-finalization-failed"
                : "import-copy-failed", exception);
        }

        try
        {
            await taskScheduler.ScanImportedMedia(library.Id, download.ImportedSeriesId, download.Id);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Imported download {DownloadId}, but could not enqueue library {LibraryId} scan",
                download.Id, library.Id);
        }

        return new CommitImportResult(download.Id, Path.GetFileName(targetPath), library.Id, library.Name);
    }

    private async Task<int?> ResolveKnownSeriesIdAsync(AcquisitionDownload download,
        CancellationToken cancellationToken)
    {
        if (download.WantedItemId.HasValue && download.MonitoringTargetId.HasValue)
        {
            var wanted = (await unitOfWork.MonitoringRepository.GetWantedAsync(
                    download.MonitoringTargetId.Value, cancellationToken))
                .SingleOrDefault(item => item.Id == download.WantedItemId.Value);
            if (wanted?.LibrarySeriesId is not null) return wanted.LibrarySeriesId;
        }

        if (!download.MonitoringTargetId.HasValue) return null;
        var target = await unitOfWork.MonitoringRepository.GetAsync(download.MonitoringTargetId.Value,
            cancellationToken);
        return target?.LibrarySeriesId;
    }

    private async Task<ImportAnalysisResult> MarkAsync(AcquisitionDownload download,
        IReadOnlyCollection<ImportCandidate> candidates, bool manual, string message, CancellationToken cancellationToken)
    {
        download.Status = manual ? AcquisitionDownloadStatus.NeedsManualMatch : AcquisitionDownloadStatus.ImportPending;
        download.ErrorMessage = manual ? message : string.Empty;
        await unitOfWork.CommitAsync(cancellationToken);
        return new ImportAnalysisResult(download.Id, candidates, manual, message);
    }

    private async Task RequireManualMatchAsync(AcquisitionDownload download, string message,
        CancellationToken cancellationToken)
    {
        download.Status = AcquisitionDownloadStatus.NeedsManualMatch;
        download.ErrorMessage = message;
        await unitOfWork.CommitAsync(cancellationToken);
    }

    private static string ExistingSeriesDirectory(string? folderPath, string? lowestFolderPath)
    {
        var value = !string.IsNullOrWhiteSpace(lowestFolderPath) && Directory.Exists(lowestFolderPath)
            ? lowestFolderPath
            : folderPath;
        if (string.IsNullOrWhiteSpace(value) || !Directory.Exists(value))
            throw new DirectoryNotFoundException("The monitored series directory is unavailable.");
        return Path.GetFullPath(value);
    }

    private static IReadOnlyCollection<ImportCandidate> Discover(string output, string allowedRoot, CancellationToken cancellationToken)
    {
        if (File.Exists(output))
        {
            var candidate = Candidate(new FileInfo(output), allowedRoot);
            return candidate is null ? [] : [candidate];
        }
        if (!Directory.Exists(output)) return [];

        var rootDirectory = new DirectoryInfo(output);
        if ((rootDirectory.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Reparse-point roots are not allowed.");
        var found = new List<ImportCandidate>();
        var pending = new Stack<DirectoryInfo>();
        pending.Push(rootDirectory);
        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var directory = pending.Pop();
            foreach (var file in directory.EnumerateFiles())
            {
                if ((file.Attributes & FileAttributes.ReparsePoint) != 0) continue;
                var candidate = Candidate(file, allowedRoot);
                if (candidate is not null) found.Add(candidate);
                if (found.Count > MaximumCandidates) throw new IOException("Candidate limit exceeded.");
            }
            foreach (var child in directory.EnumerateDirectories())
            {
                if ((child.Attributes & FileAttributes.ReparsePoint) == 0 && IsWithin(allowedRoot, child.FullName)) pending.Push(child);
            }
        }
        return found.OrderBy(candidate => candidate.RelativePath, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static ImportCandidate? Candidate(FileInfo file, string allowedRoot)
    {
        if (!IsWithin(allowedRoot, file.FullName)) return null;
        var format = Path.GetExtension(file.Name).ToLowerInvariant() switch
        {
            ".epub" => AcquisitionMediaFormat.Epub,
            ".azw3" => AcquisitionMediaFormat.Azw3,
            ".mobi" => AcquisitionMediaFormat.Mobi,
            ".pdf" => AcquisitionMediaFormat.Pdf,
            ".cbz" => AcquisitionMediaFormat.Cbz,
            ".cbr" => AcquisitionMediaFormat.Cbr,
            ".cb7" => AcquisitionMediaFormat.Cb7,
            _ => AcquisitionMediaFormat.Unknown,
        };
        return format == AcquisitionMediaFormat.Unknown
            ? null
            : new ImportCandidate(file.Name, Path.GetRelativePath(allowedRoot, file.FullName), format, file.Length);
    }

    private static string ResolveSource(string stagingRoot, string completedPath, string candidateRelativePath)
    {
        if (string.IsNullOrWhiteSpace(stagingRoot) || string.IsNullOrWhiteSpace(completedPath) ||
            string.IsNullOrWhiteSpace(candidateRelativePath) || Path.IsPathRooted(candidateRelativePath))
            throw new LibrariannException("import-candidate-path-is-invalid");
        var root = Path.GetFullPath(stagingRoot);
        var output = Path.GetFullPath(completedPath);
        var source = Path.GetFullPath(Path.Combine(root, candidateRelativePath));
        if (!IsWithin(root, source)) throw new LibrariannException("import-candidate-path-is-invalid");
        if (File.Exists(output))
        {
            if (!PathsEqual(output, source)) throw new LibrariannException("import-candidate-does-not-belong-to-download");
        }
        else if (!IsWithin(output, source))
        {
            throw new LibrariannException("import-candidate-does-not-belong-to-download");
        }
        return source;
    }

    private static string ResolveTargetDirectory(string targetRoot, string subdirectory)
    {
        var relative = subdirectory.Trim();
        if (Path.IsPathRooted(relative)) throw new LibrariannException("import-destination-is-invalid");
        var target = Path.GetFullPath(Path.Combine(targetRoot, relative));
        if (!IsWithin(targetRoot, target)) throw new LibrariannException("import-destination-is-invalid");
        return target;
    }

    private static string ValidatedBaseName(string requested, string sourceFileName)
    {
        var baseName = string.IsNullOrWhiteSpace(requested)
            ? Path.GetFileNameWithoutExtension(sourceFileName)
            : requested.Trim();
        if (baseName is "." or ".." || baseName.EndsWith(' ') || baseName.EndsWith('.') ||
            baseName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || baseName.Contains('/') || baseName.Contains('\\'))
            throw new LibrariannException("import-file-name-is-invalid");
        var deviceName = baseName.Split('.')[0].ToUpperInvariant();
        if (deviceName is "CON" or "PRN" or "AUX" or "NUL" or "COM1" or "COM2" or "COM3" or "COM4" or "COM5" or
            "COM6" or "COM7" or "COM8" or "COM9" or "LPT1" or "LPT2" or "LPT3" or "LPT4" or "LPT5" or "LPT6" or
            "LPT7" or "LPT8" or "LPT9")
            throw new LibrariannException("import-file-name-is-invalid");
        return baseName;
    }

    private static string AutomaticBaseName(WantedItem? wanted, string sourceFileName)
    {
        if (wanted is null || string.IsNullOrWhiteSpace(wanted.Title))
            return Path.GetFileNameWithoutExtension(sourceFileName);

        var value = string.IsNullOrWhiteSpace(wanted.Sequence)
            ? wanted.Title
            : string.IsNullOrWhiteSpace(wanted.Series)
                ? $"{wanted.Sequence} - {wanted.Title}"
                : $"{wanted.Series} {wanted.Sequence} - {wanted.Title}";
        var portable = new string(value.Trim().Select(character =>
            char.IsControl(character) || "<>:\"/\\|?*".Contains(character) ? '-' : character).ToArray());
        while (portable.Contains("--", StringComparison.Ordinal)) portable = portable.Replace("--", "-", StringComparison.Ordinal);
        portable = portable.Trim(' ', '.');
        if (portable.Length > 180) portable = portable[..180].TrimEnd(' ', '.');
        if (string.IsNullOrWhiteSpace(portable)) return Path.GetFileNameWithoutExtension(sourceFileName);

        var deviceName = portable.Split('.')[0].ToUpperInvariant();
        if (deviceName is "CON" or "PRN" or "AUX" or "NUL" or "COM1" or "COM2" or "COM3" or "COM4" or "COM5" or
            "COM6" or "COM7" or "COM8" or "COM9" or "LPT1" or "LPT2" or "LPT3" or "LPT4" or "LPT5" or "LPT6" or
            "LPT7" or "LPT8" or "LPT9")
            portable = "_" + portable;
        return portable;
    }

    private static async Task CopyAndValidateAsync(FileInfo source, string temporaryPath, CancellationToken cancellationToken)
    {
        await using (var input = new FileStream(source.FullName, FileMode.Open, FileAccess.Read, FileShare.Read,
                         CopyBufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan))
        await using (var output = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                         CopyBufferSize, FileOptions.Asynchronous | FileOptions.WriteThrough))
        {
            await input.CopyToAsync(output, CopyBufferSize, cancellationToken);
            await output.FlushAsync(cancellationToken);
            output.Flush(true);
        }
        if (new FileInfo(temporaryPath).Length != source.Length)
            throw new IOException("The copied file failed length validation.");
    }

    private async Task MarkCommitFailureAsync(AcquisitionDownload download, bool placed, CancellationToken cancellationToken)
    {
        download.Status = AcquisitionDownloadStatus.NeedsManualMatch;
        download.ErrorMessage = placed
            ? "The file was placed, but import finalization failed. Verify the destination before retrying."
            : "The media file could not be copied into the library.";
        await unitOfWork.CommitAsync(cancellationToken);
    }

    private static void DeleteTemporary(string? path)
    {
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path)) File.Delete(path);
    }

    private static void EnsureNoChildReparsePoints(string root, string targetDirectory)
    {
        var relative = Path.GetRelativePath(root, targetDirectory);
        var current = Path.GetFullPath(root);
        foreach (var segment in relative.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                     StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            if (Directory.Exists(current) && (new DirectoryInfo(current).Attributes & FileAttributes.ReparsePoint) != 0)
                throw new LibrariannException("import-destination-contains-a-link");
        }
    }

    private static IReadOnlyCollection<AcquisitionMediaFormat> SupportedFormats(IEnumerable<FileTypeGroup> groups)
    {
        var formats = new HashSet<AcquisitionMediaFormat>();
        foreach (var group in groups)
        {
            if (group == FileTypeGroup.Epub) formats.Add(AcquisitionMediaFormat.Epub);
            if (group == FileTypeGroup.Pdf) formats.Add(AcquisitionMediaFormat.Pdf);
            if (group == FileTypeGroup.Archive)
            {
                formats.Add(AcquisitionMediaFormat.Cbz);
                formats.Add(AcquisitionMediaFormat.Cbr);
                formats.Add(AcquisitionMediaFormat.Cb7);
            }
        }
        return formats.OrderBy(format => format).ToArray();
    }

    private static AcquisitionMediaFormat GetFormat(string extension) => extension.ToLowerInvariant() switch
    {
        ".epub" => AcquisitionMediaFormat.Epub,
        ".pdf" => AcquisitionMediaFormat.Pdf,
        ".cbz" => AcquisitionMediaFormat.Cbz,
        ".cbr" => AcquisitionMediaFormat.Cbr,
        ".cb7" => AcquisitionMediaFormat.Cb7,
        _ => AcquisitionMediaFormat.Unknown,
    };

    private static bool PathsEqual(string first, string second) => string.Equals(Path.GetFullPath(first),
        Path.GetFullPath(second), OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    private static bool IsWithin(string root, string candidate)
    {
        var relative = Path.GetRelativePath(Path.GetFullPath(root), Path.GetFullPath(candidate));
        return !Path.IsPathRooted(relative) && relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal);
    }
}

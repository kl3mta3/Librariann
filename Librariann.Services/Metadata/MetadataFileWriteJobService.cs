using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Database;
using Librariann.API.Repositories;
using Librariann.API.Services;
using Librariann.API.Services.Metadata;
using Librariann.Models.DTOs.Metadata;
using Microsoft.Extensions.Logging;

namespace Librariann.Services.Metadata;

public sealed class MetadataFileWriteJobService(
    IUnitOfWork unitOfWork,
    IMetadataFileWriteCoordinator coordinator,
    ITaskScheduler taskScheduler,
    ILogger<MetadataFileWriteJobService> logger) : IMetadataFileWriteJobService
{
    public async Task WriteSeriesFilesAsync(int seriesId, MetadataFileUpdate update,
        CancellationToken cancellationToken = default)
    {
        var settings = await unitOfWork.SettingsRepository.GetSettingsDtoAsync(cancellationToken);
        if (!settings.WriteMetadataToFiles) return;
        var series = await unitOfWork.SeriesRepository.GetSeriesByIdAsync(seriesId, SeriesIncludes.None,
            cancellationToken);
        if (series is null) return;

        var wroteAny = false;
        foreach (var file in await unitOfWork.SeriesRepository.GetFilesForSeriesAsync(seriesId, cancellationToken))
        {
            var extension = Path.GetExtension(file.FilePath);
            if (!extension.Equals(".epub", StringComparison.OrdinalIgnoreCase) &&
                !extension.Equals(".cbz", StringComparison.OrdinalIgnoreCase)) continue;
            try
            {
                await coordinator.WriteAsync(file.FilePath, update, cancellationToken);
                wroteAny = true;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // Continue so one damaged file cannot prevent backups and updates for the rest of the series.
                logger.LogError(exception, "Metadata file write failed for MangaFile {MangaFileId}", file.Id);
            }
        }

        if (wroteAny) await taskScheduler.ScanSeries(series.LibraryId, seriesId, true);
    }
}

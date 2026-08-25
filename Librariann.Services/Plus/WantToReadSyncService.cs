using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;
using Hangfire;
using Librariann.API.Database;
using Librariann.API.Repositories;
using Librariann.API.Services.Plus;
using Librariann.Common;
using Librariann.Common.Extensions;
using Librariann.Models.Entities.Enums.Audit;
using Librariann.Models.DTOs.LibrariannPlus.Audit;
using Librariann.Models.DTOs.LibrariannPlus.Metadata;
using Librariann.Models.Entities.Enums;
using Librariann.Models.Entities.User;
using Microsoft.Extensions.Logging;

namespace Librariann.Services.Plus;

/// <summary>
/// Responsible for syncing Want To Read from upstream providers with Librariann
/// </summary>
public class WantToReadSyncService(
    IUnitOfWork unitOfWork,
    ILogger<WantToReadSyncService> logger,
    ILicenseService licenseService,
    ILibrariannPlusAuditService auditService,
    ILibrariannPlusApiService librariannPlusApiService)
    : IWantToReadSyncService
{
    public async Task Sync(CancellationToken ct = default)
    {
        if (!await licenseService.HasActiveLicense(ct: ct)) return;

        var license = (await unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.LicenseKey, ct)).Value;

        var users = await unitOfWork.UserRepository.GetAllUsersAsync(AppUserIncludes.WantToRead | AppUserIncludes.UserPreferences, ct: ct);
        foreach (var user in users)
        {
            logger.LogInformation("Syncing want to read for user: {UserName}", user.UserName);

            var userScrobbleProviders = user.ScrobbleProviders
                .Where(kv => kv.Value.Settings.WantToReadSync)
                .ToList();

            await auditService.LogAsync(
                LibrariannPlusAuditCategory.Sync,
                LibrariannPlusEventType.SyncStarted,
                AuditStatus.Info,
                payload: new AuditLogWantToReadSyncParamsDto { UserName = user.UserName, Providers = userScrobbleProviders.Select(kv => kv.Key).ToList()},
                userId: user.Id, ct: ct);

            var externalSeries = new List<ExternalSeriesDetailDto>();

            foreach (var kv in userScrobbleProviders)
            {
                var token = kv.Value.AuthenticationToken;
                if (string.IsNullOrEmpty(token))
                {
                    logger.LogDebug("Cannot sync Want To Read for user {UserName} for {Provider} as they do not have a valid token", user.UserName, kv.Key);
                    continue;
                }

                var result = await librariannPlusApiService.GetWantToRead(kv.Key, token, license, ct);
                if (!result.IsSuccess)
                {
                    await auditService.LogAsync(
                        LibrariannPlusAuditCategory.Sync,
                        LibrariannPlusEventType.SyncFailed,
                        AuditStatus.Failure,
                        payload: new AuditLogWantToReadSyncParamsDto { UserName = user.UserName },
                        error: result.ErrorMessage,
                        userId: user.Id, ct: ct);

                    logger.LogError("Failed to retrieve Want To Read for user {UserName} from {Provider}: {Error}", user.UserName, kv.Key, result.ErrorMessage);
                    continue;
                }

                externalSeries.AddRange(result.Data ?? []);
            }

            foreach (var unmatchedSeries in externalSeries)
            {
                var match = await unitOfWork.SeriesRepository.MatchSeriesAsync(unmatchedSeries, ct);
                if (match == null)
                {
                    continue;
                }

                user.WantToRead.Add(new AppUserWantToRead
                {
                    SeriesId = match.Id,
                });

                logger.LogTrace("Added {MatchName} ({Format}) to Want to Read", match.Name, match.Format);
            }

            user.WantToRead = user.WantToRead.DistinctBy(d => d.SeriesId).ToList();

            unitOfWork.UserRepository.Update(user);
            await unitOfWork.CommitAsync(ct);

            await auditService.LogAsync(
                LibrariannPlusAuditCategory.Sync,
                LibrariannPlusEventType.SyncCompleted,
                AuditStatus.Success,
                payload: new AuditLogWantToReadSyncCompletedParamsDto
                {
                    UserName = user.UserName,
                    SeriesMatched = user.WantToRead.Count,
                    Providers = userScrobbleProviders.Select(kv => kv.Key).ToList()
                },
                userId: user.Id, ct: ct);

            RecurringJob.TriggerJob(TaskScheduler.RemoveFromWantToReadTaskId);
        }

    }
}

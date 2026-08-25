using System.IO.Abstractions;
using Librariann.API.Services;
using Librariann.API.Services.Acquisition;
using Librariann.API.Services.Helpers;
using Librariann.API.Services.Metadata;
using Librariann.API.Services.Plus;
using Librariann.API.Services.Reading;
using Librariann.API.Services.ReadingLists;
using Librariann.API.Services.Scanner;
using Librariann.API.Services.SignalR;
using Librariann.Models.Entities.Enums;
using Librariann.Services.Helpers;
using Librariann.Services.Acquisition;
using Librariann.Services.HostedServices;
using Librariann.Services.Metadata;
using Librariann.Services.Metadata.Providers;
using Librariann.Services.Plus;
using Librariann.Services.Plus.ScrobbleService;
using Librariann.Services.Reading;
using Librariann.Services.ReadingLists;
using Librariann.Services.Scanner;
using Librariann.Services.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace Librariann.Services.Extensions;

public static class ApplicationServiceExtensions
{

    public static void AddLibrariannServices(this IServiceCollection services)
    {
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<ICacheHelper, CacheHelper>();

        services.AddScoped<IStatsService, StatsService>();
        services.AddScoped<ITaskScheduler, TaskScheduler>();
        services.AddScoped<ICacheService, CacheService>();
        services.AddScoped<IArchiveService, ArchiveService>();
        services.AddScoped<IAudiobookMetadataService, AudiobookMetadataService>();
        services.AddScoped<IBackupService, BackupService>();
        services.AddScoped<ICleanupService, CleanupService>();
        services.AddScoped<IBookService, BookService>();
        services.AddScoped<IVersionUpdaterService, VersionUpdaterService>();
        services.AddScoped<IDownloadService, DownloadService>();
        services.AddScoped<IReaderService, ReaderService>();
        services.AddScoped<IReadingItemService, ReadingItemService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IBookmarkService, BookmarkService>();
        services.AddScoped<ISeriesService, SeriesService>();
        services.AddScoped<IReadingListService, ReadingListService>();
        services.AddScoped<IDeviceService, DeviceService>();
        services.AddScoped<IStatisticService, StatisticService>();
        services.AddScoped<IMediaErrorService, MediaErrorService>();
        services.AddScoped<IMediaConversionService, MediaConversionService>();
        services.AddScoped<IStreamService, StreamService>();
        services.AddScoped<IRatingService, RatingService>();
        services.AddScoped<IPersonService, PersonService>();
        services.AddScoped<IReadingProfileService, ReadingProfileService>();
        services.AddScoped<IKoreaderService, KoreaderService>();
        services.AddScoped<IFontService, FontService>();
        services.AddScoped<IAnnotationService, AnnotationService>();
        services.AddScoped<IOpdsService, OpdsService>();
        services.AddScoped<IOAuthService, OAuthService>();

        services.AddScoped<IUrlValidationService, UrlValidationService>();

        services.AddScoped<ICblExportService, CblExportService>();
        services.AddScoped<ICblGithubService, CblGithubService>();
        services.AddScoped<ICblImportService, CblImportService>();

        services.AddScoped<IScannerService, ScannerService>();
        services.AddScoped<IProcessSeries, ProcessSeries>();
        services.AddScoped<IMetadataService, MetadataService>();
        services.AddScoped<IWordCountAnalyzerService, WordCountAnalyzerService>();
        services.AddScoped<ILibraryWatcher, LibraryWatcher>();
        services.AddScoped<ITachiyomiService, TachiyomiService>();
        services.AddScoped<ICollectionTagService, CollectionTagService>();

        services.AddScoped<IFileSystem, FileSystem>();
        services.AddScoped<IDirectoryService, DirectoryService>();
        services.AddScoped<IEventHub, EventHub>();
        services.AddScoped<IPresenceTracker, PresenceTracker>();
        services.AddScoped<IImageService, ImageService>();
        services.AddScoped<ICoverDbService, CoverDbService>();

        services.AddScoped<ILocalizationService, LocalizationService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IAuthKeyService, AuthKeyService>();
        services.AddSingleton<ICredentialProtectionService, CredentialProtectionService>();
        services.AddSingleton<IExternalSourceLaunchTokenStore, ExternalSourceLaunchTokenStore>();
        services.AddScoped<IReleaseEvaluationService, ReleaseEvaluationService>();
        services.AddScoped<IIntegrationEndpointValidator, IntegrationEndpointValidator>();
        services.AddScoped<IIntegrationProviderService, IntegrationProviderService>();
        services.AddScoped<IIntegrationProviderTestService, IntegrationProviderTestService>();
        services.AddSingleton<IIntegrationHttpClientFactory, IntegrationHttpClientFactory>();
        services.AddSingleton<IReleaseTokenStore, ReleaseTokenStore>();
        services.AddScoped<IDownloadClientFactory, DownloadClientFactory>();
        services.AddScoped<IInteractiveSearchService, InteractiveSearchService>();
        services.AddScoped<IReleaseGrabService, ReleaseGrabService>();
        services.AddScoped<IAcquisitionQueueService, AcquisitionQueueService>();
        services.AddScoped<IAcquisitionImportService, AcquisitionImportService>();
        services.AddScoped<IQualityProfileService, QualityProfileService>();
        services.AddScoped<IMonitoringService, MonitoringService>();
        services.AddScoped<IMonitoringJobService, MonitoringJobService>();
        services.AddScoped<IMonitoringCatalogService, MonitoringCatalogService>();
        services.AddScoped<IMetadataProviderFactory, MetadataProviderFactory>();
        services.AddSingleton<IMetadataApplyTokenStore, MetadataApplyTokenStore>();
        services.AddScoped<IMetadataLookupService, MetadataLookupService>();
        services.AddScoped<IMetadataApplicationService, MetadataApplicationService>();
        services.AddScoped<IMetadataProvenanceService, MetadataProvenanceService>();
        // Scoped, not Singleton: it depends on the Scoped IUnitOfWork (to read the contact-email setting) and
        // creates a fresh HttpClient/SocketsHttpHandler per call anyway, so there's no pooling benefit to lose.
        services.AddScoped<ITrustedMetadataHttpClientFactory, TrustedMetadataHttpClientFactory>();
        services.AddHttpClient();
        services.AddScoped<IKokoroTtsService, KokoroTtsService>();
        services.AddScoped<IKokoroReleaseService, KokoroReleaseService>();
        // Singleton - has to remember a live Process handle across requests. Same IServiceScopeFactory pattern
        // as TtsRequestQueueService for reaching the Scoped IUnitOfWork; no hosted-service registration needed
        // here since there's no background loop to run, just a shutdown hook registered in its constructor.
        services.AddSingleton<IKokoroProcessService, KokoroProcessService>();
        // Singleton for the same reason as the process service - install progress has to outlive the request
        // that kicked off a 350MB+ download.
        services.AddSingleton<IKokoroInstallService, KokoroInstallService>();
        // Same Singleton reasoning as IKokoroInstallService - a separate service (own download, own progress
        // state) since it installs an unrelated tool (ffmpeg) from an unrelated source (BtbN/FFmpeg-Builds).
        services.AddSingleton<IFfmpegInstallService, FfmpegInstallService>();
        // Singleton + hosted service pointing at the same instance, same pattern as ActiveUserTrackerService
        // below - see TtsRequestQueueService's doc comment for why it can't just be Scoped like IKokoroTtsService.
        services.AddSingleton<TtsRequestQueueService>();
        services.AddSingleton<ITtsRequestQueueService>(sp => sp.GetRequiredService<TtsRequestQueueService>());
        services.AddHostedService(sp => sp.GetRequiredService<TtsRequestQueueService>());
        services.AddScoped<IAuthorMetadataService, OpenLibraryAuthorMetadataService>();
        services.AddScoped<IAuthorMetadataRefreshService, AuthorMetadataRefreshService>();
        services.AddScoped<IMetadataFileWriter, EpubMetadataFileWriter>();
        services.AddScoped<IMetadataFileWriter, CbzMetadataFileWriter>();
        services.AddScoped<IMetadataFileWriteCoordinator, MetadataFileWriteCoordinator>();
        services.AddScoped<IMetadataFileWriteJobService, MetadataFileWriteJobService>();

        services.AddScoped<ILibrariannPlusApiService, LibrariannPlusApiService>();
        services.AddKeyedScoped<IScrobbleProviderService, MangabakaScrobbleProviderService>(ScrobbleProvider.MangaBaka);
        services.AddKeyedScoped<IScrobbleProviderService, AniListScrobbleProviderService>(ScrobbleProvider.AniList);
        services.AddKeyedScoped<IScrobbleProviderService, MyAnimeListScrobbleProviderService>(ScrobbleProvider.Mal);
        services.AddKeyedScoped<IScrobbleProviderService, HardcoverScrobbleProviderService>(ScrobbleProvider.Hardcover);
        services.AddScoped<IScrobbleRuleService, ScrobbleRuleService>();
        services.AddScoped<IScrobblingService, ScrobblingService>();
        services.AddScoped<ILicenseService, LicenseService>();
        services.AddScoped<IExternalMetadataService, ExternalMetadataService>();
        services.AddScoped<ISmartCollectionSyncService, SmartCollectionSyncService>();
        services.AddScoped<IWantToReadSyncService, WantToReadSyncService>();
        services.AddScoped<ILibrariannPlusAuditService, LibrariannPlusAuditService>();
        services.AddScoped<ILibrariannPlusProviderHealthService, LibrariannPlusProviderHealthService>();

        services.AddScoped<IOidcService, OidcService>();


        services.AddScoped<IReadingHistoryService, ReadingHistoryService>();
        services.AddScoped<IClientDeviceService, ClientDeviceService>();
        services.AddScoped<IDeviceTrackingService, DeviceTrackingService>();


        services.AddScoped<IFileCacheService, FileCacheService>();
        services.AddSingleton<IReadingSessionService, ReadingSessionService>();
        services.AddSingleton<IEntityNamingService, EntityNamingService>();
        services.AddSingleton<ActiveUserTrackerService>(); // This is required for the below lines. It allows IHostedService.StopAsync() to be called on shutdown
        services.AddSingleton<IActiveUserTrackerService>(sp => sp.GetRequiredService<ActiveUserTrackerService>());
        services.AddHostedService(sp => sp.GetRequiredService<ActiveUserTrackerService>());

        services.AddHostedService<StartupTasksHostedService>();
    }

}

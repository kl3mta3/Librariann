using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Database;
using Librariann.API.Repositories;
using Librariann.API.Services;
using Librariann.Database.Repositories;
using Librariann.Models.Entities.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Librariann.Database;


public class UnitOfWork : IUnitOfWork
{
    private readonly DataContext _context;

    public UnitOfWork(DataContext context, UserManager<AppUser> userManager,
        ICredentialProtectionService credentialProtectionService)
    {
        _context = context;

        SeriesRepository = new SeriesRepository(_context);
        UserRepository = new UserRepository(_context, userManager);
        LibraryRepository = new LibraryRepository(_context);
        VolumeRepository = new VolumeRepository(_context);
        SettingsRepository = new SettingsRepository(_context, credentialProtectionService);
        AppUserProgressRepository = new AppUserProgressRepository(_context);
        CollectionTagRepository = new CollectionTagRepository(_context);
        ChapterRepository = new ChapterRepository(_context);
        ReadingListRepository = new ReadingListRepository(_context);
        SeriesMetadataRepository = new SeriesMetadataRepository(_context);
        PersonRepository = new PersonRepository(_context);
        GenreRepository = new GenreRepository(_context);
        TagRepository = new TagRepository(_context);
        MangaFileRepository = new MangaFileRepository(_context);
        DeviceRepository = new DeviceRepository(_context);
        MediaErrorRepository = new MediaErrorRepository(_context);
        ScrobbleRepository = new ScrobbleRepository(_context);
        UserTableOfContentRepository = new UserTableOfContentRepository(_context);
        AppUserSmartFilterRepository = new AppUserSmartFilterRepository(_context);
        AppUserExternalSourceRepository = new AppUserExternalSourceRepository(_context,
            credentialProtectionService);
        ExternalSeriesMetadataRepository = new ExternalSeriesMetadataRepository(_context);
        EmailHistoryRepository = new EmailHistoryRepository(_context);
        AppUserReadingProfileRepository = new AppUserReadingProfileRepository(_context);
        AnnotationRepository = new AnnotationRepository(_context);
        EpubFontRepository = new EpubFontRepository(_context);
        ReadingSessionRepository = new ReadingSessionRepository(_context);
        ClientDeviceRepository = new ClientDeviceRepository(_context);
        RemapRuleRepository = new ReadingListRemapRuleRepository(_context);
        LibrariannPlusAuditRepository = new LibrariannPlusAuditRepository(_context);
        IntegrationProviderRepository = new IntegrationProviderRepository(_context);
        AcquisitionDownloadRepository = new AcquisitionDownloadRepository(_context);
        MetadataFieldProvenanceRepository = new MetadataFieldProvenanceRepository(_context);
        QualityProfileRepository = new QualityProfileRepository(_context);
        MonitoringRepository = new MonitoringRepository(_context);
        InviteRequestRepository = new InviteRequestRepository(_context);
    }

    /// <summary>
    /// This is here for Scanner only. Don't use otherwise.
    /// </summary>
    public IDataContext DataContext => _context;
    public ISeriesRepository SeriesRepository { get; }
    public IUserRepository UserRepository { get; }
    public ILibraryRepository LibraryRepository { get; }
    public IVolumeRepository VolumeRepository { get; }
    public ISettingsRepository SettingsRepository { get; }
    public IAppUserProgressRepository AppUserProgressRepository { get; }
    public ICollectionTagRepository CollectionTagRepository { get; }
    public IChapterRepository ChapterRepository { get; }
    public IReadingListRepository ReadingListRepository { get; }
    public ISeriesMetadataRepository SeriesMetadataRepository { get; }
    public IPersonRepository PersonRepository { get; }
    public IGenreRepository GenreRepository { get; }
    public ITagRepository TagRepository { get; }
    public IMangaFileRepository MangaFileRepository { get; }
    public IDeviceRepository DeviceRepository { get; }
    public IMediaErrorRepository MediaErrorRepository { get; }
    public IScrobbleRepository ScrobbleRepository { get; }
    public IUserTableOfContentRepository UserTableOfContentRepository { get; }
    public IAppUserSmartFilterRepository AppUserSmartFilterRepository { get; }
    public IAppUserExternalSourceRepository AppUserExternalSourceRepository { get; }
    public IExternalSeriesMetadataRepository ExternalSeriesMetadataRepository { get; }
    public IEmailHistoryRepository EmailHistoryRepository { get; }
    public IAppUserReadingProfileRepository AppUserReadingProfileRepository { get; }
    public IAnnotationRepository AnnotationRepository { get; }
    public IEpubFontRepository EpubFontRepository { get;  }
    public IReadingSessionRepository ReadingSessionRepository { get;  }
    public IClientDeviceRepository ClientDeviceRepository { get; }
    public IReadingListRemapRuleRepository RemapRuleRepository { get; }
    public ILibrariannPlusAuditRepository LibrariannPlusAuditRepository { get; }
    public IIntegrationProviderRepository IntegrationProviderRepository { get; }
    public IAcquisitionDownloadRepository AcquisitionDownloadRepository { get; }
    public IMetadataFieldProvenanceRepository MetadataFieldProvenanceRepository { get; }
    public IQualityProfileRepository QualityProfileRepository { get; }
    public IMonitoringRepository MonitoringRepository { get; }
    public IInviteRequestRepository InviteRequestRepository { get; }

    /// <summary>
    /// Commits pending changes inside an IMMEDIATE SQLite transaction so writer contention
    /// waits on the writer lock (via busy_timeout) instead of failing with SQLITE_BUSY_SNAPSHOT.
    /// </summary>
    public async Task<bool> CommitAsync(CancellationToken ct = default)
    {
        await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var result = await _context.SaveChangesAsync(ct) > 0;
        await tx.CommitAsync(ct);
        return result;
    }

    /// <summary>
    /// Is the DB Context aware of Changes in loaded entities
    /// </summary>
    /// <returns></returns>
    public bool HasChanges()
    {
        return _context.ChangeTracker.HasChanges();
    }

    /// <summary>
    /// Rollback transaction
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<bool> RollbackAsync(CancellationToken ct = default)
    {
        try
        {
            await _context.Database.RollbackTransactionAsync(ct);
        }
        catch (Exception)
        {
            // Swallow exception (this might be used in places where a transaction isn't setup)
        }

        return true;
    }
}

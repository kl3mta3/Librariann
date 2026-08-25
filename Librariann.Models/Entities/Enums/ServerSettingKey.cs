using System;
using System.ComponentModel;

namespace Librariann.Models.Entities.Enums;

/// <summary>
/// 15 is blocked as it was EnableSwaggerUi, which is no longer used
/// </summary>
public enum ServerSettingKey
{
    /// <summary>
    /// Cron format for how often full library scans are performed.
    /// </summary>
    [Description("TaskScan")]
    TaskScan = 0,
    /// <summary>
    /// Where files are cached. Not currently used.
    /// </summary>
    [Description("CacheDirectory")]
    CacheDirectory = 1,
    /// <summary>
    /// Cron format for how often backups are taken.
    /// </summary>
    [Description("TaskBackup")]
    TaskBackup = 2,
    /// <summary>
    /// Logging level for Server. Not managed in DB. Managed in appsettings.json and synced to DB.
    /// </summary>
    [Description("LoggingLevel")]
    LoggingLevel = 3,
    /// <summary>
    /// Port server listens on. Not managed in DB. Managed in appsettings.json and synced to DB.
    /// </summary>
    [Description("Port")]
    Port = 4,
    /// <summary>
    /// Where the backups are stored.
    /// </summary>
    [Description("BackupDirectory")]
    BackupDirectory = 5,
    /// <summary>
    /// Is OPDS enabled for the server
    /// </summary>
    [Description("EnableOpds")]
    EnableOpds = 7,
    // Removed option on 8 in v0.9.0
    /// <summary>
    /// Base Url for the server. Not Implemented.
    /// </summary>
    [Description("BaseUrl")]
    BaseUrl = 9,
    /// <summary>
    /// Represents this installation of Librariann. Is tied to Stat reporting but has no information about user or files.
    /// </summary>
    [Description("InstallId")]
    InstallId = 10,
    /// <summary>
    /// Represents the version the software is running.
    /// </summary>
    /// <remarks>This will be updated on Startup to the latest release. Provides ability to detect if certain migrations need to be run.</remarks>
    [Description("InstallVersion")]
    InstallVersion = 11,
    /// <summary>
    /// Location of where bookmarks are stored
    /// </summary>
    [Description("BookmarkDirectory")]
    BookmarkDirectory = 12,
    // Removed option on 13 in v0.9.0
    // Removed option on 14 in v0.9.0
    /// <summary>
    /// Total Number of Backups to maintain before cleaning. Default 30, min 1.
    /// </summary>
    [Description("TotalBackups")]
    TotalBackups = 16,
    /// <summary>
    /// If Librariann should watch the library folders and process changes
    /// </summary>
    [Description("EnableFolderWatching")]
    EnableFolderWatching = 17,
    /// <summary>
    /// Total number of days worth of logs to keep
    /// </summary>
    [Description("TotalLogs")]
    TotalLogs = 18,
    // Removed option on 19 in v0.9.0
    /// <summary>
    /// The Host name (ie Reverse proxy domain name) for the server. Used for email link generation
    /// </summary>
    [Description("HostName")]
    HostName = 20,
    /// <summary>
    /// Ip addresses the server listens on. Not managed in DB. Managed in appsettings.json and synced to DB.
    /// </summary>
    [Description("IpAddresses")]
    IpAddresses = 21,
    /// <summary>
    /// Encode all media as PNG/WebP/AVIF/etc.
    /// </summary>
    /// <remarks>As of v0.7.3 this replaced ConvertCoverToWebP and ConvertBookmarkToWebP</remarks>
    [Description("EncodeMediaAs")]
    EncodeMediaAs = 22,
    /// <summary>
    /// A Librariann+ Subscription license key
    /// </summary>
    [Description("LicenseKey")]
    LicenseKey = 23,
    /// <summary>
    /// The size in MB for Caching API data
    /// </summary>
    [Description("Cache")]
    CacheSize = 24,
    /// <summary>
    /// How many Days since today in the past for reading progress, should content be considered for On Deck, before it gets removed automatically
    /// </summary>
    [Description("OnDeckProgressDays")]
    OnDeckProgressDays = 25,
    /// <summary>
    /// How many Days since today in the past for chapter updates, should content be considered for On Deck, before it gets removed automatically
    /// </summary>
    [Description("OnDeckUpdateDays")]
    OnDeckUpdateDays = 26,
    /// <summary>
    /// The size of the cover image thumbnail. Defaults to <see cref="CoverImageSize"/>.Default
    /// </summary>
    [Description("CoverImageSize")]
    CoverImageSize = 27,
    #region EmailSettings
    /// <summary>
    /// The address of the emailer host
    /// </summary>
    [Description("EmailSenderAddress")]
    EmailSenderAddress = 28,
    /// <summary>
    /// What the email name should be
    /// </summary>
    [Description("EmailSenderDisplayName")]
    EmailSenderDisplayName = 29,
    [Description("EmailAuthUserName")]
    EmailAuthUserName = 30,
    [Description("EmailAuthPassword")]
    EmailAuthPassword = 31,
    [Description("EmailHost")]
    EmailHost = 32,
    [Description("EmailPort")]
    EmailPort = 33,
    [Description("EmailEnableSsl")]
    EmailEnableSsl = 34,
    /// <summary>
    /// Number of bytes that the sender allows to be sent through
    /// </summary>
    [Description("EmailSizeLimit")]
    EmailSizeLimit = 35,
    /// <summary>
    /// Should Librariann use config/templates for Email templates or the default ones
    /// </summary>
    [Description("EmailCustomizedTemplates")]
    EmailCustomizedTemplates = 36,
    #endregion
    /// <summary>
    /// When the cleanup task should run - Critical to keeping Librariann working
    /// </summary>
    [Description("TaskCleanup")]
    TaskCleanup = 37,
    /// <summary>
    /// The Date Librariann was first installed
    /// </summary>
    [Description("FirstInstallDate")]
    FirstInstallDate = 38,
    /// <summary>
    /// The Version of Librariann on the first run
    /// </summary>
    [Description("FirstInstallVersion")]
    FirstInstallVersion = 39,
    /// <summary>
    /// A Json object of type <see cref="Librariann.Models.DTOs.Settings.OidcConfigDto"/>
    /// </summary>
    [Description("OidcConfiguration")]
    OidcConfiguration = 40,
    /// <summary>
    /// The resolution to render PDFs as when delivering them as images.
    /// </summary>
    [Description("PdfRenderResolution")]
    PdfRenderResolution = 41,
    /// <summary>
    /// When the CBL Sync task should run
    /// </summary>
    [Description("TaskCblSync")]
    TaskCblSync = 43,
    /// <summary>
    /// Allows validated Librariann metadata updates to be written back into supported library files.
    /// Disabled by default because it mutates user media, even though every write creates a backup.
    /// </summary>
    [Description("WriteMetadataToFiles")]
    WriteMetadataToFiles = 44,
    /// <summary>
    /// One-time marker for <see cref="Librariann.Database.Seed.SeedDefaultMetadataProviders"/>. Once "true",
    /// the seed never re-adds the default Open Library metadata provider, so an admin who deliberately
    /// deletes it doesn't see it reappear on the next restart.
    /// </summary>
    [Description("DefaultMetadataProviderSeeded")]
    DefaultMetadataProviderSeeded = 45,
    /// <summary>
    /// Path to the ffprobe/ffmpeg executable, used to read audiobook metadata (duration, embedded M4B chapter
    /// markers) at scan time. Audiobooks are streamed as their original file, never transcoded.
    /// </summary>
    [Description("FfmpegPath")]
    FfmpegPath = 46,
    /// <summary>
    /// Optional contact email sent in the User-Agent to free metadata providers (currently Open Library) that
    /// grant a higher rate limit to "identified" clients. No account/API key involved - just a header
    /// convention. Empty by default (unidentified, lower rate limit).
    /// </summary>
    [Description("MetadataProviderContactEmail")]
    MetadataProviderContactEmail = 47,
    /// <summary>
    /// Base URL of a self-hosted Kokoro TTS server (e.g. http://localhost:8880) the backend forwards text
    /// chunks to for synthesis when a user selects Kokoro as their TTS provider. Empty by default (Kokoro
    /// disabled - book reader TTS falls back to the browser's own SpeechSynthesis). See
    /// docs/kokoro-tts-integration.md for the request/response contract a Kokoro server must implement.
    /// </summary>
    [Description("KokoroEndpointUrl")]
    KokoroEndpointUrl = 48,
    /// <summary>
    /// Folder containing a Librariann-Kokoro-Server install (the folder holding LKS.Server.exe on Windows /
    /// LKS.Server on Linux), so Librariann can launch and supervise it as a child process. Empty by default -
    /// leaving it unset means "I'm pointing KokoroEndpointUrl at a Kokoro server I run/manage myself", and
    /// Librariann never attempts to start/stop anything.
    /// </summary>
    [Description("KokoroExecutablePath")]
    KokoroExecutablePath = 49,
    /// <summary>
    /// Whether to launch the managed Kokoro process with GPU (DirectML) synthesis enabled. Passed as an
    /// environment variable override (Kokoro__UseGpu) when Librariann starts the process - has no effect on a
    /// Kokoro server the admin points at manually rather than lets Librariann manage.
    /// </summary>
    [Description("KokoroUseGpu")]
    KokoroUseGpu = 50,
    /// <summary>
    /// Whether Librariann should keep the managed Kokoro install's ffmpeg path (its appsettings.json
    /// Ffmpeg:Path) in sync with Librariann's own FfmpegPath setting - written on install and whenever
    /// FfmpegPath changes. Defaults to true (one ffmpeg path to manage instead of two); an admin who wants
    /// Kokoro to use a different ffmpeg than Librariann can turn this off. Only affects the config file on disk
    /// - an already-running Kokoro process needs a restart to pick up a change either way.
    /// </summary>
    [Description("KokoroSyncFfmpegPath")]
    KokoroSyncFfmpegPath = 51,
    /// <summary>
    /// Shows a "Request an Invite" link on the login screen. Visitors who use it submit an email/name, landing
    /// in a pending-approval queue reviewable from Settings > Users - they do not get an account until an
    /// admin approves them (or AutoAcceptInviteRequests is on).
    /// </summary>
    [Description("ShowRequestInviteLink")]
    ShowRequestInviteLink = 52,
    /// <summary>
    /// When a request-an-invite submission comes in, immediately create and email the invite instead of
    /// queuing it for admin approval. Requires email to be configured - the whole point is the invite has to
    /// actually reach the requester, since nobody reviewed the request first.
    /// </summary>
    [Description("AutoAcceptInviteRequests")]
    AutoAcceptInviteRequests = 53,
    /// <summary>
    /// Server-wide default roles/libraries/age-restriction, stored as a DefaultInvitePermissionsDto JSON blob.
    /// Pre-fills the invite modal and is used as-is for approved/auto-accepted invite requests, which have no
    /// per-request permission picker.
    /// </summary>
    [Description("DefaultInvitePermissions")]
    DefaultInvitePermissions = 54
}

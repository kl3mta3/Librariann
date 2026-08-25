using System;
using Librariann.Models.Entities.Enums;
using System.ComponentModel.DataAnnotations;
using Librariann.Models.DTOs.Account;

namespace Librariann.Models.DTOs.Settings;
#nullable enable

public sealed record ServerSettingDto
{

    public string CacheDirectory { get; set; } = default!;
    public string TaskScan { get; set; } = default!;
    public string TaskBackup { get; set; } = default!;
    public string TaskCleanup { get; set; } = default!;
    public string TaskCblSync { get; set; } = default!;
    /// <summary>
    /// Logging level for server. Managed in appsettings.json.
    /// </summary>
    public string LoggingLevel { get; set; } = default!;
    /// <summary>
    /// Port the server listens on. Managed in appsettings.json.
    /// </summary>
    public int Port { get; set; }
    /// <summary>
    /// Comma separated list of ip addresses the server listens on. Managed in appsettings.json
    /// </summary>
    public string IpAddresses { get; set; }
    /// <summary>
    /// Enables OPDS connections to be made to the server.
    /// </summary>
    public bool EnableOpds { get; set; }
    /// <summary>
    /// Base Url for the librariann. Requires restart to take effect.
    /// </summary>
    public string BaseUrl { get; set; } = default!;
    /// <summary>
    /// Where Bookmarks are stored.
    /// </summary>
    /// <remarks>If null or empty string, will default back to default install setting aka <see cref="DirectoryService.BookmarkDirectory"/></remarks>
    public string BookmarksDirectory { get; set; } = default!;
    public string InstallVersion { get; set; } = default!;
    /// <summary>
    /// Represents a unique Id to this Librariann installation. Only used in Stats to identify unique installs.
    /// </summary>

    public string InstallId { get; set; } = default!;
    /// <summary>
    /// The format that should be used when saving media for Librariann
    /// </summary>
    /// <example>This includes things like: Covers, Bookmarks, Favicons</example>
    [EnumDataType(typeof(EncodeFormat))]
    public EncodeFormat EncodeMediaAs { get; set; }

    /// <summary>
    /// The amount of Backups before cleanup
    /// </summary>
    /// <remarks>Value should be between 1 and 30</remarks>
    public int TotalBackups { get; set; } = 30;
    /// <summary>
    /// If Librariann should watch the library folders and process changes
    /// </summary>
    public bool EnableFolderWatching { get; set; } = true;
    /// <summary>
    /// Write resolved metadata into supported media files using backup, validation, and atomic replacement.
    /// </summary>
    public bool WriteMetadataToFiles { get; set; }
    /// <summary>
    /// Total number of days worth of logs to keep at a given time.
    /// </summary>
    /// <remarks>Value should be between 1 and 30</remarks>
    public int TotalLogs { get; set; }
    /// <summary>
    /// The Host name (ie Reverse proxy domain name) for the server
    /// </summary>
    public string HostName { get; set; }
    /// <summary>
    /// The size in MB for Caching API data
    /// </summary>
    public long CacheSize { get; set; }
    /// <summary>
    /// How many Days since today in the past for reading progress, should content be considered for On Deck, before it gets removed automatically
    /// </summary>
    public int OnDeckProgressDays { get; set; }
    /// <summary>
    /// How many Days since today in the past for chapter updates, should content be considered for On Deck, before it gets removed automatically
    /// </summary>
    public int OnDeckUpdateDays { get; set; }
    /// <summary>
    /// How large the cover images should be
    /// </summary>
    [EnumDataType(typeof(CoverImageSize))]
    public CoverImageSize CoverImageSize { get; set; }
    /// <summary>
    /// How large rendered PDF images should be
    /// </summary>
    [EnumDataType(typeof(PdfRenderResolution))]
    public PdfRenderResolution PdfRenderResolution { get; set; }
    /// <summary>
    /// SMTP Configuration
    /// </summary>
    public SmtpConfigDto SmtpConfig { get; set; }
    /// <summary>
    /// OIDC Configuration
    /// </summary>
    public OidcConfigDto OidcConfig { get; set; }

    /// <summary>
    /// The Date Librariann was first installed
    /// </summary>
    public DateTime? FirstInstallDate { get; set; }
    /// <summary>
    /// The Version of Librariann on the first run
    /// </summary>
    public string? FirstInstallVersion { get; set; }
    /// <summary>
    /// Path to the ffprobe/ffmpeg executable, used to read audiobook metadata (duration, embedded M4B chapter
    /// markers) at scan time. Audiobooks are streamed as their original file - not transcoded - so this is only
    /// used for metadata reads, never for producing output. Defaults to "ffmpeg", resolved via PATH.
    /// </summary>
    public string FfmpegPath { get; set; } = "ffmpeg";
    /// <summary>
    /// Optional contact email sent in the User-Agent to free metadata providers (currently Open Library) that
    /// grant a higher rate limit (3 req/s vs 1 req/s) to "identified" clients. No account or API key involved.
    /// </summary>
    public string MetadataProviderContactEmail { get; set; } = string.Empty;
    /// <summary>
    /// Base URL of a self-hosted Kokoro TTS server. Empty disables Kokoro as a TTS option in the book reader.
    /// </summary>
    public string KokoroEndpointUrl { get; set; } = string.Empty;
    /// <summary>
    /// Folder containing a Librariann-Kokoro-Server install, so Librariann can start/stop it as a supervised
    /// child process. Empty means Librariann doesn't manage any Kokoro process - KokoroEndpointUrl is expected
    /// to point at one the admin runs themselves.
    /// </summary>
    public string KokoroExecutablePath { get; set; } = string.Empty;
    /// <summary>Whether to launch the managed Kokoro process with GPU (DirectML) synthesis enabled.</summary>
    public bool KokoroUseGpu { get; set; }
    /// <summary>Whether to keep the managed Kokoro install's ffmpeg path in sync with FfmpegPath. Defaults to true.</summary>
    public bool KokoroSyncFfmpegPath { get; set; } = true;
    /// <summary>
    /// Shows a "Request an Invite" link on the login screen.
    /// </summary>
    public bool ShowRequestInviteLink { get; set; }
    /// <summary>
    /// Immediately creates and emails an invite for a request-an-invite submission, instead of queuing it for
    /// admin approval. Requires email to be configured.
    /// </summary>
    public bool AutoAcceptInviteRequests { get; set; }
    /// <summary>
    /// Server-wide default roles/libraries/age-restriction applied to new invites.
    /// </summary>
    public DefaultInvitePermissionsDto DefaultInvitePermissions { get; set; } = new();

    /// <summary>
    /// Are at least some basics filled in
    /// </summary>
    /// <returns></returns>
    public bool IsEmailSetup()
    {
        return !string.IsNullOrEmpty(SmtpConfig.Host)
               && !string.IsNullOrEmpty(SmtpConfig.SenderAddress)
               && !string.IsNullOrEmpty(HostName);
    }

    /// <summary>
    /// Are at least some basics filled in, but not hostname as not required for Send to Device
    /// </summary>
    /// <returns></returns>
    public bool IsEmailSetupForSendToDevice()
    {
        return !string.IsNullOrEmpty(SmtpConfig.Host)
               && !string.IsNullOrEmpty(SmtpConfig.SenderAddress);
    }
}

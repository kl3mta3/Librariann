import {EncodeFormat} from "./encode-format";
import {CoverImageSize} from "./cover-image-size";
import {SmtpConfig} from "./smtp-config";
import {PdfRenderResolution} from "./pdf-render-resolution";
import {OidcConfig} from "./oidc-config";

export interface ServerSettings {
  cacheDirectory: string;
  taskScan: string;
  taskBackup: string;
  taskCleanup: string;
  taskCblSync: string;
  loggingLevel: string;
  port: number;
  ipAddresses: string;
  enableOpds: boolean;
  baseUrl: string;
  bookmarksDirectory: string;
  emailServiceUrl: string;
  encodeMediaAs: EncodeFormat;
  totalBackups: number;
  totalLogs: number;
  enableFolderWatching: boolean;
  writeMetadataToFiles: boolean;
  hostName: string;
  cacheSize: number;
  onDeckProgressDays: number;
  onDeckUpdateDays: number;
  coverImageSize: CoverImageSize;
  pdfRenderResolution: PdfRenderResolution;
  smtpConfig: SmtpConfig;
  oidcConfig: OidcConfig;
  installId: string;
  installVersion: string;
  /**
   * Path to the ffprobe/ffmpeg executable, used to read audiobook metadata (duration, embedded M4B chapter
   * markers) at scan time. Audiobooks are streamed as their original file, never transcoded.
   */
  ffmpegPath: string;
  /**
   * Optional contact email sent in the User-Agent to free metadata providers (currently Open Library) that
   * grant a higher rate limit (3 req/s vs 1 req/s) to "identified" clients. No account or API key involved.
   */
  metadataProviderContactEmail: string;
  /**
   * Base URL of a self-hosted Kokoro TTS server. Empty disables Kokoro as a TTS option in the book reader.
   */
  kokoroEndpointUrl: string;
  /** Folder containing a Librariann-Kokoro-Server install, so Librariann can start/stop it as a supervised child process. */
  kokoroExecutablePath: string;
  /** Whether to launch the managed Kokoro process with GPU (DirectML) synthesis enabled. */
  kokoroUseGpu: boolean;
  /** Whether to keep the managed Kokoro install's ffmpeg path in sync with FfmpegPath. Defaults to true. */
  kokoroSyncFfmpegPath: boolean;
}

import {HttpClient, httpResource} from '@angular/common/http';
import {inject, Injectable} from '@angular/core';
import {map, of} from 'rxjs';
import {environment} from 'src/environments/environment';
import {TextResonse} from '../_types/text-response';
import {ServerSettings} from './_models/server-settings';
import {MetadataSettings} from "./_models/metadata-settings";
import {MetadataMappingsExport} from "./manage-metadata-mappings/manage-metadata-mappings.component";
import {FieldMappingsImportResult, ImportSettings} from "../_models/import-field-mappings";
import {AuthorityValidationResult, OidcPublicConfig} from "./_models/oidc-config";
import {RunMetadataMappingsRequest} from "../_models/metadata/run-metadata-mappings-request";

/**
 * Used only for the Test Email Service call
 */
export interface EmailTestResult {
  successful: boolean;
  errorMessage: string;
  emailAddress: string;
}

/**
 * Result of pinging the configured Kokoro TTS server - backs the "Check Status" button in Settings -> Media.
 */
export interface KokoroStatus {
  isConfigured: boolean;
  isReachable: boolean;
  modelPrecision?: string;
  gpuActive?: boolean;
  gpuRequested?: boolean;
  defaultVoice?: string;
  voiceCount?: number;
  version?: string;
  uptimeSeconds?: number;
}

/**
 * Latest github.com/kl3mta3/Librariann-Kokoro-Server release - backs the "Check for Updates" button in
 * Settings -> Media. Informational only - does not download/install anything.
 */
export interface KokoroLatestRelease {
  success: boolean;
  errorMessage?: string;
  tagName?: string;
  name?: string;
  htmlUrl?: string;
  publishedAtUtc?: string;
  assetName?: string;
  assetDownloadUrl?: string;
  assetSizeBytes?: number;
}

/** Status of the Kokoro process Librariann itself started - never reports on one the admin runs manually. */
export interface KokoroProcessStatus {
  isManaged: boolean;
  isRunning: boolean;
  processId?: number;
  error?: string;
  isInstalled: boolean;
}

/** Progress of an in-flight or just-finished Kokoro download/install - poll while inProgress is true. */
export interface KokoroInstallStatus {
  inProgress: boolean;
  bytesDownloaded: number;
  totalBytes: number;
  success?: boolean | null;
  error?: string;
}

/** Progress of an in-flight or just-finished ffmpeg download/install - poll while inProgress is true. */
export interface FfmpegInstallStatus {
  inProgress: boolean;
  bytesDownloaded: number;
  totalBytes: number;
  success?: boolean | null;
  error?: string;
}

@Injectable({
  providedIn: 'root'
})
export class SettingsService {
  private http = inject(HttpClient);


  baseUrl = environment.apiUrl;

  getServerSettings() {
    return this.http.get<ServerSettings>(this.baseUrl + 'settings');
  }

  getPublicOidcConfig() {
    return this.http.get<OidcPublicConfig>(this.baseUrl + "settings/oidc");
  }

  getMetadataSettings() {
    return this.http.get<MetadataSettings>(this.baseUrl + 'settings/metadata-settings');
  }
  updateMetadataSettings(model: MetadataSettings) {
    return this.http.post<MetadataSettings>(this.baseUrl + 'settings/metadata-settings', model);
  }

  runMetadataMappings(request: RunMetadataMappingsRequest) {
    return this.http.post(this.baseUrl + 'settings/run-metadata-mappings', request);
  }

  importFieldMappings(data: MetadataMappingsExport, settings: ImportSettings) {
    const body = {
      data: data,
      settings: settings,
    }
    return this.http.post<FieldMappingsImportResult>(this.baseUrl + 'settings/import-field-mappings', body);
  }

  updateServerSettings(model: ServerSettings) {
    return this.http.post<ServerSettings>(this.baseUrl + 'settings', model);
  }

  resetServerSettings() {
    return this.http.post<ServerSettings>(this.baseUrl + 'settings/reset', {});
  }

  resetIPAddressesSettings() {
    return this.http.post<ServerSettings>(this.baseUrl + 'settings/reset-ip-addresses', {});
  }

  resetBaseUrl() {
    return this.http.post<ServerSettings>(this.baseUrl + 'settings/reset-base-url', {});
  }

  testEmailServerSettings() {
    return this.http.post<EmailTestResult>(this.baseUrl + 'settings/test-email-url', {});
  }

  testKokoroConnection() {
    return this.http.get<KokoroStatus>(this.baseUrl + 'settings/kokoro-status');
  }

  getKokoroLatestRelease() {
    return this.http.get<KokoroLatestRelease>(this.baseUrl + 'settings/kokoro-latest-release');
  }

  getKokoroProcessStatus() {
    return this.http.get<KokoroProcessStatus>(this.baseUrl + 'settings/kokoro-process-status');
  }

  startKokoroProcess() {
    return this.http.post<KokoroProcessStatus>(this.baseUrl + 'settings/kokoro-start', {});
  }

  stopKokoroProcess() {
    return this.http.post<KokoroProcessStatus>(this.baseUrl + 'settings/kokoro-stop', {});
  }

  startKokoroInstall() {
    return this.http.post<KokoroInstallStatus>(this.baseUrl + 'settings/kokoro-install', {});
  }

  getKokoroInstallStatus() {
    return this.http.get<KokoroInstallStatus>(this.baseUrl + 'settings/kokoro-install-status');
  }

  startFfmpegInstall() {
    return this.http.post<FfmpegInstallStatus>(this.baseUrl + 'settings/ffmpeg-install', {});
  }

  getFfmpegInstallStatus() {
    return this.http.get<FfmpegInstallStatus>(this.baseUrl + 'settings/ffmpeg-install-status');
  }

  isEmailSetup() {
    return this.http.get<string>(this.baseUrl + 'settings/is-email-setup', TextResonse).pipe(map(d => d == "true"));
  }

  getTaskFrequencies() {
    return this.http.get<string[]>(this.baseUrl + 'settings/task-frequencies');
  }

  getLoggingLevels() {
    return this.http.get<string[]>(this.baseUrl + 'settings/log-levels');
  }

  getLibraryTypes() {
    return this.http.get<string[]>(this.baseUrl + 'settings/library-types');
  }

  getOpdsEnabledResource() {
    return httpResource<boolean>(() => this.baseUrl + 'settings/opds-enabled').asReadonly();
  }

  clearExternalIds() {
    return this.http.post(this.baseUrl + 'settings/reset-external-ids', {})
  }

  isValidCronExpression(val: string) {
    if (val === '' || val === undefined || val === null) return of(false);
    return this.http.get<string>(this.baseUrl + 'settings/is-valid-cron?cronExpression=' + val, TextResonse).pipe(map(d => d === 'true'));
  }

  ifValidAuthority(authority: string) {
    if (authority === '' || authority === undefined || authority === null) return of(AuthorityValidationResult.NotApplicable);

    return this.http.post<string>(this.baseUrl + 'settings/is-valid-authority', {authority}, TextResonse).pipe(map(r => parseInt(r) as AuthorityValidationResult));
  }
}

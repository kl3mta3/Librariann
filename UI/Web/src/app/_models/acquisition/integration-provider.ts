export enum IntegrationProviderCategory {
  Indexer = 1,
  DownloadClient = 2,
  Metadata = 3,
  Tts = 4,
  Notification = 5,
  Authentication = 6,
}

export enum DownloadClientKind {
  QBittorrent = 1,
  Sabnzbd = 2,
  UTorrent = 3,
  Transmission = 10,
  Deluge = 11,
  NzbGet = 12,
  RTorrent = 13,
}

export enum IndexerProtocol {
  Torznab = 1,
  Newznab = 2,
}

export interface IntegrationProvider {
  id: number;
  name: string;
  category: IntegrationProviderCategory;
  providerType: string;
  baseUrl: string;
  allowPrivateNetwork: boolean;
  isEnabled: boolean;
  downloadCategory: string;
  remotePath: string;
  localPath: string;
  tags: string[];
  downloadClientKind?: DownloadClientKind;
  indexerProtocol?: IndexerProtocol;
  hasUsername: boolean;
  hasPassword: boolean;
  hasApiKey: boolean;
}

export interface UpsertIntegrationProvider extends Omit<IntegrationProvider, 'hasUsername' | 'hasPassword' | 'hasApiKey'> {
  username?: string;
  password?: string;
  apiKey?: string;
  clearUsername: boolean;
  clearPassword: boolean;
  clearApiKey: boolean;
}

export enum AcquisitionMediaFormat {
  Unknown = 0,
  Epub = 1,
  Azw3 = 2,
  Mobi = 3,
  Pdf = 4,
  Cbz = 10,
  Cbr = 11,
  Cb7 = 12,
}

export enum DownloadProtocol {
  Torrent = 1,
  Usenet = 2,
}

export interface ReleaseCandidate {
  providerKey: string;
  providerReleaseId: string;
  title: string;
  author: string;
  edition: string;
  language: string;
  format: AcquisitionMediaFormat;
  protocol: DownloadProtocol;
  sizeBytes: number;
  publishedAt: string;
  seeders?: number;
  peers?: number;
  isRetail: boolean;
  detailsUri?: string;
}

export interface ReleaseRejection {
  code: number;
  message: string;
}

export interface ReleaseDecision {
  release: ReleaseCandidate;
  score: number;
  rejections: ReleaseRejection[];
  isApproved: boolean;
  grabToken?: string;
}

export interface InteractiveSearchResponse {
  results: ReleaseDecision[];
  providerFailures: {providerKey: string; providerName: string; message: string}[];
}

export interface DownloadClientOption {
  id: number;
  name: string;
  kind: DownloadClientKind;
  protocol: DownloadProtocol;
}

export interface GrabReleaseResponse {
  externalId: string;
  downloadClientName: string;
  releaseTitle: string;
}

export enum AcquisitionDownloadStatus {
  Queued = 1,
  Downloading = 2,
  Completed = 3,
  ImportPending = 4,
  Importing = 5,
  NeedsManualMatch = 6,
  Imported = 7,
  Failed = 8,
  Removed = 9,
}

export interface AcquisitionDownload {
  id: number;
  requestedByUserId: number;
  downloadClientId: number;
  downloadClientName: string;
  externalId: string;
  releaseTitle: string;
  format: AcquisitionMediaFormat;
  protocol: DownloadProtocol;
  status: AcquisitionDownloadStatus;
  progress: number;
  outputPath: string;
  importedPath: string;
  importedSeriesId?: number;
  errorMessage: string;
  createdAtUtc: string;
  lastPolledAtUtc?: string;
  completedAtUtc?: string;
  importedAtUtc?: string;
  metadataRefreshQueuedAtUtc?: string;
  externalRemovedAtUtc?: string;
}

export interface ImportCandidate {
  fileName: string;
  relativePath: string;
  format: AcquisitionMediaFormat;
  sizeBytes: number;
}

export interface ImportAnalysisResult {
  downloadId: number;
  candidates: ImportCandidate[];
  needsManualMatch: boolean;
  message: string;
}

export interface ImportDestinationOption {
  libraryId: number;
  libraryName: string;
  folderId: number;
  folderPath: string;
  supportedFormats: AcquisitionMediaFormat[];
}

export interface CommitImportRequest {
  downloadId: number;
  libraryId: number;
  folderId: number;
  candidateRelativePath: string;
  destinationSubdirectory: string;
  destinationBaseName: string;
}

export interface CommitImportResult {
  downloadId: number;
  fileName: string;
  libraryId: number;
  libraryName: string;
}

export interface ProviderTestResult {
  isSuccess: boolean;
  message: string;
  elapsed: string;
}

export interface QualityProfile {
  id: number;
  name: string;
  mediaType: number;
  language: string;
  upgradeAllowed: boolean;
  preferRetail: boolean;
  cutoffFormat: AcquisitionMediaFormat;
  minimumSizeBytes?: number;
  maximumSizeBytes?: number;
  formatScores: Partial<Record<AcquisitionMediaFormat, number>>;
}

export interface UpsertQualityProfile extends Omit<QualityProfile, 'formatScores'> {
  formatScores: Record<number, number>;
}

import {LibrariannMediaType} from '../metadata/librariann-metadata';

export enum MonitoringTargetKind {
  Book = 1,
  Series = 2,
  Author = 3,
}

export enum MonitoringSearchStatus {
  CandidateFound = 1,
  NoApprovedCandidate = 2,
  ProviderFailure = 3,
}

export enum WantedItemStatus {
  Missing = 1,
  Owned = 2,
  Downloading = 3,
  Ignored = 4,
}

export interface MonitoringTarget {
  id: number;
  createdByUserId: number;
  kind: MonitoringTargetKind;
  mediaType: LibrariannMediaType;
  librarySeriesId?: number;
  qualityProfileId: number;
  title: string;
  author: string;
  isbn: string;
  language: string;
  externalProviderKey: string;
  externalItemId: string;
  monitorMissing: boolean;
  monitorFuture: boolean;
  automaticGrabEnabled: boolean;
  downloadClientId?: number;
  minimumAutomaticGrabScore: number;
  lastAutomaticGrabAtUtc?: string;
  isEnabled: boolean;
  searchIntervalHours: number;
  lastSearchAtUtc?: string;
  nextSearchAtUtc: string;
  lastSearchSummary: string;
  lastCatalogSyncAtUtc?: string;
  catalogSummary: string;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface WantedItem {
  id: number;
  monitoringTargetId: number;
  providerKey: string;
  externalItemId: string;
  title: string;
  author: string;
  series: string;
  sequence: string;
  publicationYear?: number;
  status: WantedItemStatus;
  librarySeriesId?: number;
  firstSeenAtUtc: string;
  lastSeenAtUtc: string;
  lastSearchAtUtc?: string;
  nextSearchAtUtc: string;
  lastSearchSummary: string;
}

export interface MissingSeriesItem {
  wantedItemId: number;
  monitoringTargetId: number;
  sourceSeriesId: number;
  libraryId: number;
  sourceSeriesTitle: string;
  missingTitle: string;
  author: string;
  series: string;
  sequence: string;
  publicationYear?: number;
}

export type UpsertMonitoringTarget = Omit<MonitoringTarget,
  'createdByUserId' | 'lastSearchAtUtc' | 'nextSearchAtUtc' | 'lastSearchSummary' |
  'lastAutomaticGrabAtUtc' | 'lastCatalogSyncAtUtc' | 'catalogSummary' | 'createdAtUtc' | 'updatedAtUtc'>;

export interface MonitoringSearchRun {
  id: number;
  monitoringTargetId: number;
  wantedItemId?: number;
  status: MonitoringSearchStatus;
  query: string;
  resultCount: number;
  approvedCount: number;
  bestReleaseTitle: string;
  bestReleaseScore?: number;
  summary: string;
  wasGrabbed: boolean;
  grabSummary: string;
  decisionSnapshotJson: string;
  startedAtUtc: string;
  completedAtUtc: string;
}

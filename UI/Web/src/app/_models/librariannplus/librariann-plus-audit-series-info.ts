import {LibrariannPlusAuditEntry} from './librariann-plus-audit-entry';
import {MetadataProvider} from "./metadata-provider.enum";

export interface LibrariannPlusAuditSeriesInfo {
  seriesId: number;
  libraryId: number;
  seriesName: string;
  isMatched: boolean;
  mangaBakaId?: number;
  aniListId?: number;
  malId?: number;
  hardcoverId?: number;
  metronId?: number;
  comicVineId?: string;
  cbrId?: number;
  metadataProvider?: MetadataProvider;
  nextRefreshUtc: string | null;
  lastRefreshedUtc: string | null;
  recentEvents: LibrariannPlusAuditEntry[];
  isStandAlone: boolean;
}

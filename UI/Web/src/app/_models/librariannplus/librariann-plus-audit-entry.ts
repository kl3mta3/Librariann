import {LibrariannPlusAuditCategory} from './librariann-plus-audit-category.enum';
import {LibrariannPlusEventType} from './librariann-plus-event-type.enum';
import {AuditStatus} from './audit-status.enum';
import {AuditSubjectType} from './audit-subject-type.enum';
import {LibrariannPlusScrobbleDetails} from './librariann-plus-scrobble-details';
import {MetadataFieldChange} from './metadata-field-change';
import {LibrariannPlusAuditMatchDetails} from './librariann-plus-audit-match-details';
import {LibrariannPlusAuditSyncDetails} from './librariann-plus-audit-sync-details';
import {LibrariannPlusAuditMetadataExtras} from './librariann-plus-audit-metadata-extras';
import {LibrariannPlusSystemDetail} from "./librariann-plus-system-detail";


export interface LibrariannPlusAuditEntry {
  id: number;
  createdUtc: string;
  category: LibrariannPlusAuditCategory;
  eventType: LibrariannPlusEventType;
  status: AuditStatus;
  seriesId: number | null;
  libraryId: number | null;
  seriesName: string | null;
  subjectType: AuditSubjectType;
  subjectId: number | null;
  userId: number | null;
  username: string | null;
  diff: MetadataFieldChange[] | null;
  errorMessage: string | null;
  scrobbleErrorId: number | null;
  scrobbleDetails: LibrariannPlusScrobbleDetails | null;
  matchDetails: LibrariannPlusAuditMatchDetails | null;
  syncDetails: LibrariannPlusAuditSyncDetails | null;
  metadataExtras: LibrariannPlusAuditMetadataExtras | null;
  systemDetails: LibrariannPlusSystemDetail | null;
  canRetry: boolean;
}

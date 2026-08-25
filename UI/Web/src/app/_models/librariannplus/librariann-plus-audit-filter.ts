import {LibrariannPlusAuditCategory} from './librariann-plus-audit-category.enum';
import {AuditStatus} from './audit-status.enum';
import {AuditSubjectType} from './audit-subject-type.enum';
import {ScrobbleProvider} from "../../_services/scrobbling.service";

export interface LibrariannPlusAuditFilter {
  category?: LibrariannPlusAuditCategory | null;
  status?: AuditStatus | null;
  subjectType?: AuditSubjectType | null;
  provider?: ScrobbleProvider | null;
  userId?: number | null;
  seriesId?: number | null;
  fromUtc?: string | null;
  toUtc?: string | null;
  search?: string | null;
}

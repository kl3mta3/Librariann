import {MetadataFetchTrigger} from "./metadata-fetch-trigger.enum";

export interface LibrariannPlusAuditMetadataExtras {
  coverUrl: string | null;
  issueNumber: string | null;
  volumeNumber: string | null;
  personName: string | null;
  aliasAdded: string | null;
  fetchTrigger: MetadataFetchTrigger | null;
}

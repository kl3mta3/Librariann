import {ScrobbleProvider} from '../../_services/scrobbling.service';

export enum LibrariannPlusProviderHealthStatus {
  Unknown = 0,
  Operational = 1,
  Degraded = 2,
  Down = 3,
}

export interface LibrariannPlusProviderIncident {
  startedAtUtc: string;
  endedAtUtc: string | null;
  type: number;
}

export interface LibrariannPlusProviderHealthSnapshot {
  provider: ScrobbleProvider;
  avgLatencyMs: number;
  status: LibrariannPlusProviderHealthStatus;
  lastIncident: LibrariannPlusProviderIncident | null;
}

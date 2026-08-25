export interface LibrariannPlusAuditStats {
  events24H: number;
  failures24H: number;
  unresolvedMatchFailures: number;
  matchedSeriesCount: number;
  totalEligibleSeriesCount: number;
  scrobbleQueueCount: number;
  /** Matched but needs refresh (automatic) */
  staleMatchesCount: number;
  /** Failed to match **/
  blacklistedSeriesCount: number;
}

export interface LibrariannPlusMyAuditStats {
  events24H: number;
  failures24H: number;
  scrobbleQueueCount: number;
}

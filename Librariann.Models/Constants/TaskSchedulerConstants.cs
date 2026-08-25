namespace Librariann.Models.Constants;

public static class TaskSchedulerConstants
{
    public const string ScanQueue = "scan";
    public const string DefaultQueue = "default";
    public const string RemoveFromWantToReadTaskId = "remove-from-want-to-read";
    public const string UpdateYearlyStatsTaskId = "update-yearly-stats";
    public const string SyncThemesTaskId = "sync-themes";
    public const string CheckForUpdateId = "check-updates";
    public const string CleanupDbTaskId = "cleanup-db";
    public const string CleanupTaskId = "cleanup";
    public const string TaskCblSyncId = "sync-cbl";
    public const string BackupTaskId = "backup";
    public const string ScanLibrariesTaskId = "scan-libraries";
    public const string CheckScrobblingTokensId = "librariann+-check-scrobbling-tokens";
    public const string ProcessScrobblingEventsId = "librariann+-process-scrobbling-events";
    public const string ProcessProcessedScrobblingEventsId = "librariann+-process-processed-scrobbling-events";
    public const string LicenseCheckId = "license-check";
    public const string LibrariannPlusDataRefreshId = "librariann+-data-refresh";
    public const string LibrariannPlusStackSyncId = "librariann+-stack-sync";
    public const string LibrariannPlusWantToReadSyncId = "librariann+-want-to-read-sync";
    public const string ReadingHistoryAggregationId = "reading-history-aggregation";
    public const string AuthKeyExpirationId = "auth-key-expiration";
    public const string EnsureSideNavId = "ensure-sidenav";
    public const string FlushUserActiveTaskId = "flush-user-active";
    public const string PurgeLibrariannPlusAuditLogsId = "librariann+-purge-audit-logs";
    public const string CreateReadStatusTransitionRuleEventsId = "librariann+-create-read-status-transition-rule-events";
    public const string RefreshConnectedTokensId = "librariann+-refresh-connected-tokens";
    public const string AcquisitionDownloadPollId = "librariann-acquisition-download-poll";
    public const string AcquisitionAutomaticImportId = "librariann-acquisition-automatic-import";
    public const string AcquisitionImportReconciliationId = "librariann-acquisition-import-reconciliation";
    public const string MonitoringSearchId = "librariann-monitoring-search";
    public const string MonitoringCatalogSyncId = "librariann-monitoring-catalog-sync";
}

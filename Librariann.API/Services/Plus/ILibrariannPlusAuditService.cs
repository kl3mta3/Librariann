using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs.LibrariannPlus;
using Librariann.Models.Entities.Enums.Audit;
using Librariann.Models.Entities.History;
using Librariann.Models.Entities.Scrobble;

namespace Librariann.API.Services.Plus;

public interface ILibrariannPlusAuditService
{
    Task LogAsync(
        LibrariannPlusAuditCategory category,
        LibrariannPlusEventType eventType,
        AuditStatus status,
        AuditSubjectType subjectType = AuditSubjectType.Global,
        int? seriesId = null,
        int? subjectId = null,
        object? payload = null,
        string? error = null,
        int? userId = null,
        ScrobbleError? scrobbleError = null,
        CancellationToken ct = default);

    /// <summary>
    /// Logs an audit log only if the same audit log hasn't been logged in the last 24h
    /// Audit logs are the same if their <see cref="LibrariannPlusAuditLog.Category"/>, <see cref="LibrariannPlusAuditLog.EventType"/>,
    /// <see cref="LibrariannPlusAuditLog.Status"/>, <see cref="LibrariannPlusAuditLog.SubjectType"/>, <see cref="LibrariannPlusAuditLog.ErrorMessage"/>,
    /// and the <see cref="idSelector"/> are the same.
    /// </summary>
    Task LogTemperedAsync(
        Expression<Func<LibrariannPlusAuditLog, bool>> idSelector,
        LibrariannPlusAuditCategory category,
        LibrariannPlusEventType eventType,
        AuditStatus status,
        AuditSubjectType subjectType = AuditSubjectType.Global,
        int? seriesId = null,
        int? subjectId = null,
        object? payload = null,
        string? error = null,
        int? userId = null,
        CancellationToken ct = default);

    Task LogMatchAsync(LibrariannPlusEventType type, int seriesId, object payload,
        AuditStatus status = AuditStatus.Success, string? error = null, CancellationToken ct = default);

    Task LogMetadataAsync(int seriesId, IList<MetadataFieldChangeDto> changes, CancellationToken ct = default);

    Task LogChapterMetadataAsync(int chapterId, int seriesId, IList<MetadataFieldChangeDto> changes,
        CancellationToken ct = default);

    Task LogPersonAsync(LibrariannPlusEventType type, int personId, object payload,
        AuditStatus status = AuditStatus.Success, CancellationToken ct = default);

    Task LogCollectionAsync(LibrariannPlusEventType type, int collectionId, object payload,
        AuditStatus status = AuditStatus.Success, int? userId = null, CancellationToken ct = default);

    Task LogScrobbleAsync(LibrariannPlusEventType type, int seriesId, AuditLogScrobbleParamsDto details,
        AuditStatus status, string? error = null, int? userId = null, ScrobbleError? scrobbleError = null, CancellationToken ct = default);

    Task LogChapterScrobbleAsync(LibrariannPlusEventType type, int seriesId, int chapterId, AuditLogScrobbleParamsDto details,
        AuditStatus status, string? error = null, int? userId = null, ScrobbleError? scrobbleError = null, CancellationToken ct = default);

    Task PurgeOldLogsAsync(CancellationToken ct = default);
}

using System;
using Librariann.Models.Entities.Enums.Audit;
using Librariann.Models.Entities.Scrobble;
using Librariann.Models.Entities.User;

namespace Librariann.Models.Entities.History;
#nullable enable

/// <summary>
/// Records a durable, queryable log of every significant Librariann+ event:
/// matching, metadata writes, scrobble sends, collection syncs, and people updates.
/// </summary>
public class LibrariannPlusAuditLog
{
    public long Id { get; set; }
    public DateTime CreatedUtc { get; set; }

    public LibrariannPlusAuditCategory Category { get; set; }
    public LibrariannPlusEventType EventType { get; set; }
    public AuditStatus Status { get; set; }

    /// <summary>
    /// Series FK - set for Series, Chapter, and series-contextual events. No cascade delete: logs outlive entities
    /// </summary>
    public int? SeriesId { get; set; }

    public int? ScrobbleErrorId { get; set; }
    public ScrobbleError? ScrobbleError { get; set; }

    /// <summary>
    /// Discriminator describing what SubjectId refers to
    /// </summary>
    public AuditSubjectType SubjectType { get; set; }

    /// <summary>PersonId, CollectionId, or ChapterId depending on SubjectType. Null for Series/Global events</summary>
    public int? SubjectId { get; set; }

    /// <summary>
    /// JSON-serialized event-specific payload.
    /// </summary>
    public string? Payload { get; set; }

    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Scrobble events that failed allow retrying
    /// </summary>
    public bool HasRetried { get; set; }

    /// <summary>
    /// The user who triggered this event. Null for system-initiated events.
    /// No cascade delete: logs outlive users.
    /// </summary>
    public int? UserId { get; set; }
    public AppUser? User { get; set; }
}

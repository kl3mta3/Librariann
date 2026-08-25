using System;
using System.Collections.Generic;
using Librariann.Models.Entities.Enums.Audit;
using System.ComponentModel.DataAnnotations;

namespace Librariann.Models.DTOs.LibrariannPlus;
#nullable enable

public sealed record LibrariannPlusAuditEntryDto
{
    public long Id { get; init; }
    public DateTime CreatedUtc { get; init; }
    [EnumDataType(typeof(LibrariannPlusAuditCategory))]
    public LibrariannPlusAuditCategory Category { get; init; }
    [EnumDataType(typeof(LibrariannPlusEventType))]
    public LibrariannPlusEventType EventType { get; init; }
    [EnumDataType(typeof(AuditStatus))]
    public AuditStatus Status { get; init; }
    public int? SeriesId { get; init; }
    public int? LibraryId { get; init; }
    public string? SeriesName { get; init; }
    [EnumDataType(typeof(AuditSubjectType))]
    public AuditSubjectType SubjectType { get; init; }
    public int? SubjectId { get; init; }
    public int? UserId { get; init; }
    public string? Username { get; init; }
    public IList<MetadataFieldChangeDto>? Diff { get; init; }
    public string? ErrorMessage { get; init; }
    public int? ScrobbleErrorId { get; init; }
    public LibrariannPlusScrobbleDetailsDto? ScrobbleDetails { get; init; }
    public LibrariannPlusAuditMatchDetailsDto? MatchDetails { get; init; }
    public LibrariannPlusAuditSyncDetailsDto? SyncDetails { get; init; }
    public LibrariannPlusAuditMetadataExtrasDto? MetadataExtras { get; init; }
    public LibrariannPlusAuditSystemDetailsDto? SystemDetails { get; init; }
    public bool CanRetry { get; init; }
}

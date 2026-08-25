using System;
using Librariann.Models.Entities.Enums;
using Librariann.Models.Entities.Enums.Audit;
using System.ComponentModel.DataAnnotations;

namespace Librariann.Models.DTOs.LibrariannPlus;
#nullable enable

public sealed record LibrariannPlusAuditFilterDto
{
    [EnumDataType(typeof(LibrariannPlusAuditCategory))]
    public LibrariannPlusAuditCategory? Category { get; init; }
    [EnumDataType(typeof(AuditStatus))]
    public AuditStatus? Status { get; init; }
    [EnumDataType(typeof(AuditSubjectType))]
    public AuditSubjectType? SubjectType { get; init; }
    /// <summary>
    /// When set, forces <see cref="Category"/> to be <see cref="LibrariannPlusAuditCategory.Scrobble"/>
    /// </summary>
    [EnumDataType(typeof(ScrobbleProvider))]
    public ScrobbleProvider? Provider { get; init; }
    public int? UserId { get; init; }
    public int? SeriesId { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public string? Search { get; init; }
}
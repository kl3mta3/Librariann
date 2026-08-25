#nullable enable
using System;
using Librariann.Models.Entities.Enums;
using System.ComponentModel.DataAnnotations;

namespace Librariann.Models.DTOs.LibrariannPlus.Audit;

public sealed record AuditLogSystemTokenRefreshParamsDto
{
    [EnumDataType(typeof(ScrobbleProvider))]
    public ScrobbleProvider Provider { get; init; }
    public DateTime? ValidUntilUtc { get; init; }
}

public sealed record AuditLogSystemProviderInfoSyncParamsDto
{
    [EnumDataType(typeof(ScrobbleProvider))]
    public ScrobbleProvider Provider { get; init; }
    public LibrariannPlusUserInfo? UserInfo { get; init; }
}

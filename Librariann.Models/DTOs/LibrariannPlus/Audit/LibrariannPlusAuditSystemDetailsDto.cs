using System;
using Librariann.Models.DTOs.LibrariannPlus.Audit;
using Librariann.Models.Entities.Enums;
using System.ComponentModel.DataAnnotations;

namespace Librariann.Models.DTOs.LibrariannPlus;

public sealed record LibrariannPlusAuditSystemDetailsDto
{

    [EnumDataType(typeof(ScrobbleProvider))]
    public ScrobbleProvider Provider { get; init; }
    public DateTime? ValidUntilUtc { get; init; }
    public LibrariannPlusUserInfo? UserInfo { get; init; }

    public static LibrariannPlusAuditSystemDetailsDto From(AuditLogSystemTokenRefreshParamsDto dto)
    {
        return new LibrariannPlusAuditSystemDetailsDto
        {
            Provider = dto.Provider,
            ValidUntilUtc = dto.ValidUntilUtc,
        };
    }

    public static LibrariannPlusAuditSystemDetailsDto From(AuditLogSystemProviderInfoSyncParamsDto dto)
    {
        return new LibrariannPlusAuditSystemDetailsDto
        {
            Provider = dto.Provider,
            UserInfo = dto.UserInfo,
        };
    }

}
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs.LibrariannPlus.License;

namespace Librariann.API.Services.Plus;

public interface ILicenseService
{
    Task RemoveLicense(CancellationToken ct = default);
    Task<LibrariannPlusRegisterResultDto> AddLicense(string license, string email, string? discordId, CancellationToken ct = default);
    Task<bool> HasActiveLicense(bool forceCheck = false, CancellationToken ct = default);
    Task<bool> HasActiveSubscription(string? license, CancellationToken ct = default);
    Task<bool> ResetLicense(string license, string email, CancellationToken ct = default);
    Task<LicenseInfoDto?> GetLicenseInfo(bool forceCheck = false, CancellationToken ct = default);
    Task<bool> ResendWelcomeEmail(CancellationToken ct = default);
    Task<LibrariannPlusLicenseUsageDto> GetLicenseUsage(CancellationToken ct = default);
    Task<bool> CancelLicense(CancelLibrariannPlusLicenseDto dto, CancellationToken ct);
    Task<IList<LibrariannPlusProductInfoDto>> GetProducts(CancellationToken ct = default);
    Task<string?> RenewLicense(RenewLibrariannPlusLicenseDto dto, CancellationToken ct);
    Task<bool> ChangeEmail(ChangeEmailOnLicenseDto dto, CancellationToken ct);
    Task LinkDiscord(string discordId, string discordUsername, CancellationToken ct = default);
}

using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs;
using Librariann.Models.DTOs.LibrariannPlus.Metadata;
using Librariann.Models.DTOs.Settings;

namespace Librariann.API.Services;

public interface ISettingsService
{
    Task<MetadataSettingsDto> UpdateMetadataSettings(MetadataSettingsDto dto, CancellationToken ct = default);

    /// <summary>
    /// Update <see cref="MetadataSettings.Whitelist"/>, <see cref="MetadataSettings.Blacklist"/>, <see cref="MetadataSettings.AgeRatingMappings"/>, <see cref="MetadataSettings.FieldMappings"/>
    /// with data from the given dto.
    /// </summary>
    /// <param name="dto"></param>
    /// <param name="settings"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<FieldMappingsImportResultDto> ImportFieldMappings(FieldMappingsDto dto, ImportSettingsDto settings, CancellationToken ct = default);
    Task<ServerSettingDto> UpdateSettings(ServerSettingDto updateSettingsDto, CancellationToken ct = default);

    /// <summary>
    /// One-time/idempotent startup migration that encrypts recoverable credentials inherited in the general settings
    /// table before normal background work begins.
    /// </summary>
    Task ProtectStoredCredentialsAsync(CancellationToken ct = default);

    /// <summary>
    /// Check if the server can reach the authority at the given uri
    /// </summary>
    /// <param name="authority"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<AuthorityValidationResult> IsValidAuthority(string authority, CancellationToken ct = default);
}

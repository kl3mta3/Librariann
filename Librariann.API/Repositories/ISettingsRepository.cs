using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs.LibrariannPlus.Metadata;
using Librariann.Models.DTOs.Settings;
using Librariann.Models.Entities;
using Librariann.Models.Entities.Enums;
using Librariann.Models.Entities.Metadata;
using Librariann.Models.Entities.MetadataMatching;

namespace Librariann.API.Repositories;

public interface ISettingsRepository
{
    void Update(ServerSetting settings);
    void Update(MetadataSettings settings);
    void RemoveRange(List<MetadataFieldMapping> fieldMappings);
    Task<ServerSettingDto> GetSettingsDtoAsync(CancellationToken ct = default);
    Task<ServerSetting> GetSettingAsync(ServerSettingKey key, CancellationToken ct = default);
    Task<IEnumerable<ServerSetting>> GetSettingsAsync(CancellationToken ct = default);
    Task<MetadataSettings> GetMetadataSettings(CancellationToken ct = default);
    Task<MetadataSettingsDto> GetMetadataSettingDto(CancellationToken ct = default);
}

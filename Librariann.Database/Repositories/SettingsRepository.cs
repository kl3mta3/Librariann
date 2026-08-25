using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Repositories;
using Librariann.API.Services;
using Librariann.Models.DTOs.LibrariannPlus.Metadata;
using Librariann.Models.DTOs.Settings;
using Librariann.Models.Entities;
using Librariann.Models.Entities.Enums;
using Librariann.Models.Entities.Metadata;
using Librariann.Models.Mapping;
using Librariann.Models.Entities.MetadataMatching;
using Microsoft.EntityFrameworkCore;

namespace Librariann.Database.Repositories;


public class SettingsRepository(DataContext context,
    ICredentialProtectionService credentialProtectionService) : ISettingsRepository
{
    public void Update(ServerSetting settings)
    {
        context.Entry(settings).State = EntityState.Modified;
    }

    public void Update(MetadataSettings settings)
    {
        context.Entry(settings).State = EntityState.Modified;
    }

    public void RemoveRange(List<MetadataFieldMapping> fieldMappings)
    {
        context.MetadataFieldMapping.RemoveRange(fieldMappings);
    }


    public async Task<MetadataSettings> GetMetadataSettings(CancellationToken ct = default)
    {
        return await context.MetadataSettings
            .Include(m => m.FieldMappings)
            .FirstAsync(ct);
    }

    public async Task<MetadataSettingsDto> GetMetadataSettingDto(CancellationToken ct = default)
    {
        var settings = await context.MetadataSettings
            .Include(m => m.FieldMappings)
            .FirstAsync(ct);

        return settings.ToMetadataSettingsDto();
    }

    public async Task<ServerSettingDto> GetSettingsDtoAsync(CancellationToken ct = default)
    {
        var settings = await context.ServerSetting
            .Select(x => x)
            .AsNoTracking()
            .ToListAsync(ct);
        UnprotectCredentialValues(settings);
        return settings.ToServerSettingDto();
    }

    private void UnprotectCredentialValues(IEnumerable<ServerSetting> settings)
    {
        foreach (var setting in settings)
        {
            if (setting.Key == ServerSettingKey.EmailAuthPassword &&
                credentialProtectionService.IsProtected(setting.Value))
            {
                setting.Value = credentialProtectionService.Unprotect(setting.Value,
                    ServerSettingCredentialScopes.SmtpPassword);
                continue;
            }

            if (setting.Key != ServerSettingKey.OidcConfiguration) continue;
            var config = JsonSerializer.Deserialize<OidcConfigDto>(setting.Value);
            if (config == null || !credentialProtectionService.IsProtected(config.Secret)) continue;

            config.Secret = credentialProtectionService.Unprotect(config.Secret,
                ServerSettingCredentialScopes.OidcClientSecret);
            setting.Value = JsonSerializer.Serialize(config);
        }
    }

    public Task<ServerSetting> GetSettingAsync(ServerSettingKey key, CancellationToken ct = default)
    {
        return context.ServerSetting.SingleOrDefaultAsync(x => x.Key == key, ct)!;
    }

    public async Task<IEnumerable<ServerSetting>> GetSettingsAsync(CancellationToken ct = default)
    {
        return await context.ServerSetting.ToListAsync(ct);
    }
}

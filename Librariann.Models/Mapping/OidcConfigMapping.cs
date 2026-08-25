using Librariann.Models.DTOs.Settings;

namespace Librariann.Models.Mapping;

/// <summary>Explicit replacement for <c>CreateMap&lt;OidcConfigDto, OidcPublicConfigDto&gt;()</c>.</summary>
public static class OidcConfigMapping
{
    public static OidcPublicConfigDto ToOidcPublicConfigDto(this OidcConfigDto dto) => new()
    {
        AutoLogin = dto.AutoLogin,
        DisablePasswordAuthentication = dto.DisablePasswordAuthentication,
        ProviderName = dto.ProviderName,
        Enabled = dto.Enabled,
    };
}

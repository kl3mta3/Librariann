using Librariann.Models.DTOs.LibrariannPlus.Scrobble;
using Librariann.Models.Entities.User;

namespace Librariann.Models.Mapping;

/// <summary>
/// Explicit replacement for <c>CreateMap&lt;AppUserScrobbleProvider, ScrobbleProviderDto&gt;()</c>. Matches
/// AutoMapper's original flat/convention behavior exactly, including that
/// <see cref="ScrobbleProviderDto.HasRunScrobbleEventGeneration"/> has no matching source property and stays at
/// its default (false).
/// </summary>
public static class ScrobbleProviderMapping
{
    public static ScrobbleProviderDto ToScrobbleProviderDto(this AppUserScrobbleProvider p) => new()
    {
        Provider = p.Provider,
        UserName = p.UserName,
        AuthenticationToken = p.AuthenticationToken,
        RefreshToken = p.RefreshToken,
        ValidUntilUtc = p.ValidUntilUtc,
        LastSyncedUtc = p.LastSyncedUtc,
        ScrobbleEventGenerationRan = p.ScrobbleEventGenerationRan,
        Settings = p.Settings,
    };
}

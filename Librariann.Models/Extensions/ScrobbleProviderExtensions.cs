using System;
using Librariann.Models.DTOs.LibrariannPlus.OAuth;
using Librariann.Models.Entities.Enums;

namespace Librariann.Models.Extensions;

public static class ScrobbleProviderExtensions
{

    extension(ScrobbleProvider scrobbleProvider)
    {
        public OAuthUpstream? ToOAuthUpstream() => scrobbleProvider switch
        {
            ScrobbleProvider.Librariann => null,
            ScrobbleProvider.AniList => OAuthUpstream.AniList,
            ScrobbleProvider.Mal => OAuthUpstream.MyAnimeList,
            ScrobbleProvider.Cbr => null,
            ScrobbleProvider.Hardcover => null,
            ScrobbleProvider.MangaBaka => OAuthUpstream.MangaBaka,
            _ => throw new ArgumentOutOfRangeException(nameof(scrobbleProvider), scrobbleProvider, null)
        };

        public bool SupportsOAuthTokenRefresh() => scrobbleProvider switch
        {
            ScrobbleProvider.Mal or ScrobbleProvider.MangaBaka => true,
            _ => false
        };
    }
}

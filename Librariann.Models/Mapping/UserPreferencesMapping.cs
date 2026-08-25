using Librariann.Models.DTOs;
using Librariann.Models.Entities.User;

namespace Librariann.Models.Mapping;

/// <summary>Explicit replacement for <c>CreateMap&lt;AppUserPreferences, UserPreferencesDto&gt;()</c>.</summary>
public static class UserPreferencesMapping
{
    public static UserPreferencesDto ToUserPreferencesDto(this AppUserPreferences p) => new()
    {
        ThemeMode = p.ThemeMode,
        GlobalPageLayoutMode = p.GlobalPageLayoutMode,
        BlurUnreadSummaries = p.BlurUnreadSummaries,
        PromptForDownloadSize = p.PromptForDownloadSize,
        NoTransitions = p.NoTransitions,
        CollapseSeriesRelationships = p.CollapseSeriesRelationships,
        Locale = p.Locale,
        ColorScapeEnabled = p.ColorScapeEnabled,
        DataSaver = p.DataSaver,
        PromptForRereadsAfter = p.PromptForRereadsAfter,
        IgnoredGenreIds = p.IgnoredGenreIds,
        FavoriteGenreIds = p.FavoriteGenreIds,
        CustomKeyBinds = p.CustomKeyBinds,
        AniListScrobblingEnabled = p.AniListScrobblingEnabled,
        WantToReadSync = p.WantToReadSync,
        BookReaderHighlightSlots = p.BookReaderHighlightSlots,
        SocialPreferences = p.SocialPreferences,
        OpdsPreferences = p.OpdsPreferences,
    };
}

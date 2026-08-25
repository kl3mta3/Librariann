namespace Librariann.Models.Entities.Enums.Theme;

/// <summary>
/// Replaces the old downloadable/uploadable SiteTheme system (Kavita's community-CSS-theme catalog, removed
/// entirely - those targeted Kavita's own DOM/branding and wouldn't render correctly under Librariann anyway).
/// A simple mode selector is the foundation for the planned BookOrbit-style Appearance panel (background/accent
/// color pickers) - System/Light/Dark is exactly its own first control. No UI to change this exists yet as part
/// of this removal; every account defaults to Dark until that panel is built.
/// </summary>
public enum ThemeMode
{
    Dark = 0,
    Light = 1,
    System = 2,
}

/**
 * Replaces the old downloadable/uploadable SiteTheme system (removed - see the backend ThemeMode enum's doc
 * comment for why). Foundation for the planned BookOrbit-style Appearance panel; no UI to change this exists
 * yet as part of this removal.
 */
export enum ThemeMode {
  Dark = 0,
  Light = 1,
  System = 2,
}

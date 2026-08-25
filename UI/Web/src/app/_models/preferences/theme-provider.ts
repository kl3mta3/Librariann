/**
 * Where something (a book reading theme, a font) came from - a built-in system default vs. something the user
 * customized/uploaded. Previously lived in site-theme.ts alongside the now-removed downloadable SiteTheme
 * system; kept here since book themes and fonts both still use it independently of that.
 */
export enum ThemeProvider {
  System = 1,
  Custom = 2,
}

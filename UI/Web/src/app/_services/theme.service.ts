import {DOCUMENT} from '@angular/common';
import {HttpClient} from '@angular/common/http';
import {computed, effect, inject, Injectable, Renderer2, RendererFactory2} from '@angular/core';
import {ReplaySubject, tap} from 'rxjs';
import {environment} from 'src/environments/environment';
import {ThemeMode} from '../_models/preferences/theme-mode';
import {ColorscapeService} from "./colorscape.service";
import {ColorScape} from "../_models/theme/colorscape";
import {AccountService} from "./account.service";

/**
 * Site-wide theming. The old downloadable/uploadable SiteTheme system (browse/download/upload a CSS file from
 * Kavita's community theme repo, inject it as a <style> tag) has been removed entirely - those files targeted
 * Kavita's own DOM/branding and wouldn't render correctly under Librariann anyway, and the app's actual default
 * appearance never really depended on it (it's plain CSS in the compiled bundle, not something toggled by a
 * body class). ThemeMode (Dark/Light/System) replaces it as the foundation for the planned BookOrbit-style
 * Appearance panel (background/accent color pickers) - no UI to change it exists yet as part of this removal.
 */
@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private document = inject<Document>(DOCUMENT);
  private httpClient = inject(HttpClient);
  private accountService = inject(AccountService);

  private readonly colorTransitionService = inject(ColorscapeService);

  /** Default book reading theme selector name - unrelated to the removed SiteTheme system, this is the book
   * reader's own (much simpler) built-in dark/light/sepia page theming. */
  public defaultBookTheme: string = 'Dark';

  private darkModeSource = new ReplaySubject<boolean>(1);
  public isDarkMode$ = this.darkModeSource.asObservable();

  /** The active user's theme mode, defaulting to Dark when logged out (public confirmation pages etc.) or unset. */
  public readonly themeMode = computed(() => this.accountService.currentUser()?.preferences?.themeMode ?? ThemeMode.Dark);
  /** Lowercase name form ('dark'/'light'/'system') - e.g. for registering an ECharts theme by name. Always a
   * truthy string, unlike the raw enum (ThemeMode.Dark === 0, which is falsy - callers checking "has a theme
   * loaded yet" against the raw enum would incorrectly treat Dark mode as "not loaded"). */
  public readonly themeName = computed(() => ThemeMode[this.themeMode()].toLowerCase());
  /** Chart color palette CSS custom properties - recomputed whenever themeMode changes (the dependency read
   * below exists purely to trigger that, same as the old SiteTheme-driven version did). */
  public readonly chartsColourPalette = computed(() => {
    this.themeMode();
    return this.loadChartColours();
  });

  private renderer: Renderer2;
  private baseUrl = environment.apiUrl;

  constructor() {
    const rendererFactory = inject(RendererFactory2);
    this.renderer = rendererFactory.createRenderer(null, null);

    effect(() => this.applyThemeMode(this.themeMode()));
  }

  getColorScheme() {
    return getComputedStyle(this.document.body).getPropertyValue('--color-scheme').trim();
  }

  /**
   * --theme-color from theme. Updates the meta tag
   * @returns
   */
  getThemeColor() {
    return getComputedStyle(this.document.body).getPropertyValue('--theme-color').trim();
  }

  /**
   * --msapplication-TileColor from theme. Updates the meta tag
   * @returns
   */
  getTileColor() {
    return getComputedStyle(this.document.body).getPropertyValue('--title-color').trim();
  }

  getCssVariable(variable: string) {
    return getComputedStyle(this.document.body).getPropertyValue(variable).trim();
  }

  isDarkTheme() {
    return this.getColorScheme().toLowerCase() === 'dark';
  }

  /**
   * Used by readers to clear any global theme state before applying their own book-specific theming, so the
   * two don't visually conflict. A genuine no-op now that there's no global stylesheet/body-class system to
   * clear - kept as a method (rather than removing every call site) since readers still legitimately want "make
   * sure nothing but my own book theme is affecting this page" as a concept, even if there's currently nothing
   * here that needs undoing.
   */
  clearThemes() {
    // Intentionally empty - see doc comment above.
  }

  /**
   * Applies Dark/Light/System as a data-theme attribute on <html> - not yet backed by any real light-mode
   * stylesheet (the app has only ever shipped a dark appearance), so Light/System currently look identical to
   * Dark. This is deliberately the hook the planned Appearance panel builds on, not a finished feature.
   */
  private applyThemeMode(mode: ThemeMode) {
    const html = this.document.documentElement;
    if (mode === ThemeMode.Light) {
      this.renderer.setAttribute(html, 'data-theme', 'light');
    } else if (mode === ThemeMode.System) {
      this.renderer.removeAttribute(html, 'data-theme');
    } else {
      this.renderer.setAttribute(html, 'data-theme', 'dark');
    }
    this.darkModeSource.next(this.isDarkTheme());
  }

  /**
   * Sets the book theme on the body tag so css variable overrides can take place
   * @param selector brtheme- prefixed string
   */
  setBookTheme(selector: string) {
    this.unsetBookThemes();
    this.renderer.addClass(this.document.querySelector('body'), selector);
  }

  clearBookTheme() {
    this.unsetBookThemes();
  }

  /**
   * Set's the background color from a single primary color.
   * @param primaryColor
   * @param complementaryColor
   */
  setColorScape(primaryColor: string, complementaryColor: string | null = null) {
    this.colorTransitionService.setColorScape(primaryColor, complementaryColor);
  }

  /**
   * Trigger a request to get the colors for a given entity and apply them
   * @param entity
   * @param id
   */
  refreshColorScape(entity: 'series' | 'volume' | 'chapter' | 'person', id: number) {
    return this.httpClient.get<ColorScape>(`${this.baseUrl}colorscape/${entity}?id=${id}`).pipe(tap((cs) => {
      this.setColorScape(cs.primary || '', cs.secondary);
    }));
  }

  private unsetBookThemes() {
    Array.from(this.document.body.classList).filter(cls => cls.startsWith('brtheme-')).forEach(c => this.document.body.classList.remove(c));
  }

  private loadChartColours() {
     return [
       '--charts-palette1',
       '--charts-palette2',
       '--charts-palette3',
       '--charts-palette4',
       '--charts-palette5',
       '--charts-palette6',
       '--charts-palette7',
     ].map(ccsVarName => this.getCssVariable(ccsVarName))
  }
}

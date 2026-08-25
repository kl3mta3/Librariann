import {PageLayoutMode} from '../page-layout-mode';
import {ThemeMode} from './theme-mode';
import {HighlightSlot} from "../../book-reader/_models/annotations/highlight-slot";
import {AgeRating} from "../metadata/age-rating";
import {KeyCode} from "../../_services/key-bind.service";

export interface Preferences {

  // Global
  themeMode: ThemeMode;
  globalPageLayoutMode: PageLayoutMode;
  blurUnreadSummaries: boolean;
  promptForDownloadSize: boolean;
  noTransitions: boolean;
  collapseSeriesRelationships: boolean;
  locale: string;
  bookReaderHighlightSlots: HighlightSlot[];
  colorScapeEnabled: boolean;
  dataSaver: boolean;
  promptForRereadsAfter: number;
  ignoredGenreIds: number[];
  favoriteGenreIds: number[];
  customKeyBinds: Partial<Record<KeyBindTarget, KeyBind[]>>;

  // Librariann+
  aniListScrobblingEnabled: boolean;
  wantToReadSync: boolean;

  // Social
  socialPreferences: SocialPreferences;

  opdsPreferences: OpdsPreferences;
}

export interface SocialPreferences {
  shareReviews: boolean;
  shareAnnotations: boolean;
  viewOtherAnnotations: boolean;
  socialLibraries: number[];
  socialMaxAgeRating: AgeRating;
  socialIncludeUnknowns: boolean;
  shareProfile: boolean;
}

export interface OpdsPreferences {
  embedProgressIndicator: boolean;
  includeContinueFrom: boolean;
}

export interface KeyBind {
  meta?: boolean;
  control?: boolean;
  alt?: boolean;
  shift?: boolean;
  controllerSequence?: readonly string[];
  key: KeyCode;
}

export enum KeyBindTarget {
  NavigateToSettings = 'NavigateToSettings',
  OpenSearch = 'OpenSearch',
  NavigateToScrobbling = 'NavigateToScrobbling',

  ToggleFullScreen = 'ToggleFullScreen',
  BookmarkPage = 'BookmarkPage',
  OpenHelp = 'OpenHelp',
  GoTo = "GoTo",
  ToggleMenu = 'ToggleMenu',
  PageLeft = 'PageLeft',
  PageRight = 'PageRight',
  Escape = 'Escape',
  PageUp = 'PageUp',
  PageDown = 'PageDown',
  OffsetDoublePage = 'OffsetDoublePage',
  NextChapter = 'NextChapter',
  PreviousChapter = 'PreviousChapter',
  FirstPage = 'FirstPage',
  LastPage = 'LastPage',
  NavigateHome = 'NavigateHome',
}

export interface OpdsPreferences {
  embedProgressIndicator: boolean;
  includeContinueFrom: boolean;
}


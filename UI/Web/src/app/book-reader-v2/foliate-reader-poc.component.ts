import {ChangeDetectionStrategy, Component, DestroyRef, ElementRef, OnInit, computed, effect, inject, signal, viewChild} from '@angular/core';
import {ActivatedRoute} from '@angular/router';
import {Subject, debounceTime, firstValueFrom} from 'rxjs';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {ReaderService} from '../_services/reader.service';
import {AnnotationService} from '../_services/annotation.service';
import {Annotation} from '../book-reader/_models/annotations/annotation';
import {EpubReaderMenuService} from '../_services/epub-reader-menu.service';
import {EpubReaderSettingsService, ReaderSettingUpdate} from '../_services/epub-reader-settings.service';
import {TtsService} from '../_services/tts.service';
import {TtsControlsComponent} from '../book-reader/_components/tts-controls/tts-controls.component';
import {ThemeService} from '../_services/theme.service';
import {NavService} from '../_services/nav.service';
import {DOCUMENT} from '@angular/common';
import {ReadingProfile} from '../_models/preferences/reading-profiles';
import {BookPageLayoutMode} from '../_models/readers/book-page-layout-mode';
import {WritingStyle} from '../_models/preferences/writing-style';
import {PageStyle} from '../book-reader/_components/reader-settings/reader-settings.component';
import {BookTheme} from '../_models/preferences/book-theme';
import {buildRangeForAnnotation} from './annotation-cfi.util';
import {DrawerService} from '../_services/drawer.service';
import {TocDrawerComponent} from './toc-drawer.component';

const TOOLBAR_AUTO_HIDE_MS = 3000;

// eslint-disable-next-line @typescript-eslint/no-explicit-any
const dynamicImport: (specifier: string) => Promise<any> = new Function('specifier', 'return import(specifier)') as any;

const PROGRESS_SAVE_DEBOUNCE_MS = 2000;

interface FoliateRelocateDetail {
  section: {current: number; total: number};
  fraction: number;
  cfi: string;
}

/** foliate-js's own parsed table of contents shape (epub.js) - {label, href, subitems} nested tree. */
export interface FoliateTocItem {
  label: string;
  href: string;
  subitems?: FoliateTocItem[] | null;
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
type FoliateView = HTMLElement & {
  open: (b: File) => Promise<void>;
  goTo: (target: string | number) => Promise<void>;
  /** Jumps to an overall book position by fraction (0-1) - backs the draggable/seekable progress bar. */
  goToFraction: (fraction: number) => Promise<void>;
  close: () => void;
  addAnnotation: (annotation: {value: string; [key: string]: unknown}, remove?: boolean) => Promise<unknown>;
  /**
   * The ONLY correct way to build a resolvable CFI for a Range within a given section. `CFI.fromRange(range)`
   * alone (epubcfi.js's low-level function) only encodes the path *within* that section's own document - it
   * has no idea which spine item that is. getCFI() prefixes the section's own package-document CFI (via
   * `book.sections[index].cfi`) and joins them with the `!` indirection marker foliate-js's own resolution
   * (`resolveCFI`/`book.resolveCFI`) requires to find the right spine item again later. Calling
   * `CFI.fromRange()` directly (an earlier version of this code did) produces a CFI that *looks* valid and
   * round-trips fine as long as you already know which section it's in, but silently fails to resolve via
   * `view.addAnnotation()`/`view.goTo()` - caught live via the `draw-annotation`/highlight-rendering path.
   */
  getCFI: (sectionIndex: number, range: Range) => string;
  /** Parsed directly by foliate-js from the EPUB's own nav document/NCX - the real table of contents. */
  book?: {
    toc?: FoliateTocItem[];
    sections?: unknown[];
    /** Synchronous - resolves a TOC/link href to its spine index, for computing chapter-marker positions. */
    resolveHref?: (href: string) => {index: number} | null;
  };
  renderer?: {
    next: () => void; prev: () => void; setAttribute: (k: string, v: string) => void;
    /** Injects a stylesheet into every section's own document - the only way to style book text/links/images,
     * which live inside foliate-js's iframe-rendered sections and can't be reached by the host page's CSS. */
    setStyles: (css: string) => void;
  };
};

interface DrawAnnotationDetail {
  draw: (
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    fn: (rects: DOMRectList, options?: Record<string, unknown>) => SVGElement, options?: Record<string, unknown>,
  ) => void;
  annotation: {value: string; selectedSlotIndex?: number};
}

/**
 * The book reader, built stage by stage on the foliate-js engine (see the migration plan) as a replacement for
 * the old column-pagination reader, which has been removed now that this reached full feature parity
 * (pagination, progress, annotations, settings, TTS). Serves the `series/:seriesId/book/:chapterId` route.
 *
 * Stage 0/1 (done): vendor foliate-js, open/paginate a real EPUB fetched from this app's backend.
 * Stage 2 (done): progress via CFI - resume from and save to the existing ProgressDto.BookScrollId string
 * field. Old progress rows (pre-migration) hold a descoped XPath there, not a CFI - goTo() on a non-CFI string
 * throws, so we detect the format and fall back to the spine index (ProgressDto.pageNum, which already means
 * the same thing - a spine index - for both the old and new reader) instead of crashing.
 * Stage 3 (this revision): existing annotations are anchored by XPath/EndingXPath/SelectedText (resolved
 * server-side against HtmlAgilityPack-parsed HTML by the old reader). The new reader needs CFI instead. On each
 * section `load`, any of that chapter's annotations targeting this section that don't have a `cfi` yet get one
 * computed via `annotation-cfi.util.ts` (ports the old reader's resolution logic to the DOM/XPath APIs) +
 * foliate-js's own `CFI.fromRange()`, then PATCHed to the backend - lazy, incremental, non-destructive
 * (XPath/EndingXPath are untouched, kept as the source of truth for this).
 *
 * Highlights are drawn using foliate-js's own built-in annotation/overlay mechanism (`view.addAnnotation()` +
 * the `draw-annotation` event it emits, backed by `overlayer.js`) rather than a hand-rolled overlay - no
 * annotation storage/hit-testing code needed here, just a draw callback. Colors are CSS custom properties
 * (`--librariann-highlight-slot-N`, kept in sync with the user's slot-color preferences by `AnnotationService`),
 * not literal values baked in per highlight - a slot-color change repaints every already-drawn highlight of
 * that slot instantly with zero re-render calls, since the browser resolves the var() at paint time. This is
 * deliberately the same pattern the eventual site-wide theme system (accent colors, surfaces, etc.) should use.
 */
@Component({
  selector: 'app-foliate-reader-poc',
  standalone: true,
  imports: [TtsControlsComponent],
  // EpubReaderSettingsService is deliberately NOT providedIn: 'root' - each reader instance gets its own (same
  // as the current reader), since its state (current reading profile, form, signals) is scoped to one open book.
  providers: [EpubReaderSettingsService],
  // NOTE: this toolbar is functional but not yet visually polished to match the app's real theme/action-bar
  // styling the way the old reader's was (proper theming, etc.) - a known, tracked remaining gap, not an
  // oversight. See the migration plan's "still not done" notes.
  template: `
    <div class="poc-container" #container (mousemove)="onActivity()" (wheel)="onActivity()" (touchstart)="onActivity()"></div>

    <!-- Deliberately OUTSIDE the auto-hiding top bar: app-tts-controls manages its own toggle+panel lifecycle,
         and should only ever close via its own X button, never because the header happened to auto-hide from
         inactivity while the user is sitting still listening. See onActivity()'s doc comment. -->
    <div class="poc-tts-anchor">
      <app-tts-controls [contentRoot]="ttsContentRoot"></app-tts-controls>
    </div>

    <div class="poc-bar poc-top-bar" [class.poc-bar-hidden]="!barVisible()" (mouseenter)="onBarEnter()" (mouseleave)="onBarLeave()">
      <div class="poc-bar-group">
        <button type="button" class="btn-icon" (click)="openToc()" title="Table of Contents">
          <i class="fa-regular fa-rectangle-list" aria-hidden="true"></i>
        </button>
        <button type="button" class="btn-icon" (click)="viewAnnotationsList()" title="Annotations">
          <i class="fa-solid fa-highlighter" aria-hidden="true"></i>
        </button>
        <button type="button" class="btn-icon" (click)="openSettings()" title="Settings">
          <i class="fa-solid fa-gear" aria-hidden="true"></i>
        </button>
        <select class="poc-tts-provider" (change)="setTtsProvider($any($event.target).value)" title="TTS Provider">
          <option value="browser">Device TTS</option>
          <option value="kokoro">Kokoro</option>
        </select>
        <span class="poc-status">{{ status() }}</span>
        <button type="button" class="btn-icon poc-close" (click)="closeReader()" title="Close">
          <i class="fa fa-times-circle" aria-hidden="true"></i>
        </button>
      </div>
    </div>

    <div class="poc-bar poc-bottom-bar" [class.poc-bar-hidden]="!barVisible()" (mouseenter)="onBarEnter()" (mouseleave)="onBarLeave()">
      <div class="poc-bar-group">
        <button type="button" class="btn-icon" (click)="prev()" title="Previous">
          <i class="fa fa-angle-left" aria-hidden="true"></i>
        </button>
        <span class="poc-progress-label">{{ progressLabel() }}</span>
        <button type="button" class="btn-icon" (click)="next()" title="Next">
          <i class="fa fa-angle-right" aria-hidden="true"></i>
        </button>
      </div>
      <div class="poc-progress-track" (pointerdown)="onProgressPointerDown($event)" (pointermove)="onProgressPointerMove($event)">
        @for (marker of chapterMarkers(); track marker) {
          <div class="poc-progress-tick" [style.left.%]="marker"></div>
        }
        <div class="poc-progress-fill" [style.width.%]="progressPercent()"></div>
        <div class="poc-progress-handle" [style.left.%]="progressPercent()"></div>
      </div>
    </div>
  `,
  styles: [`
    :host { position: fixed; inset: 0; background: #1b1b1b; color: #eee; z-index: 2000; }
    .poc-container {
      position: absolute; inset: 0;
      background-color: color-mix(in srgb, var(--reader-background-color, #1b1b1b) var(--reader-background-opacity, 100%), transparent);
    }
    .poc-container foliate-view { display: block; width: 100%; height: 100%; }

    .poc-tts-anchor { position: absolute; top: 0.5rem; right: 1rem; z-index: 6; }

    .poc-bar {
      position: absolute; left: 0; right: 0; z-index: 5;
      display: flex; justify-content: center; align-items: center;
      background: color-mix(in srgb, #111 85%, transparent);
      transition: opacity 0.2s ease, transform 0.2s ease;
      opacity: 1; pointer-events: auto;
    }
    .poc-bar-hidden { opacity: 0; pointer-events: none; }
    .poc-top-bar { top: 0; padding: 0.5rem 1rem; }
    .poc-top-bar.poc-bar-hidden { transform: translateY(-100%); }
    .poc-bottom-bar { bottom: 0; flex-direction: column; gap: 0.25rem; padding: 0.4rem 1rem 0.6rem; }
    .poc-bottom-bar.poc-bar-hidden { transform: translateY(100%); }

    .poc-bar-group { display: flex; align-items: center; gap: 0.75rem; }
    .btn-icon { cursor: pointer; background: none; border: none; color: inherit; font-size: 1rem; padding: 0.25rem 0.4rem; }
    .btn-icon:hover { opacity: 0.75; }
    .poc-close { margin-left: 0.5rem; }
    .poc-tts-provider { background: #222; color: inherit; border: 1px solid #444; border-radius: 4px; padding: 0.15rem 0.3rem; }
    .poc-status { font-size: 0.8rem; opacity: 0.7; margin-left: 0.5rem; }
    .poc-progress-label { font-size: 0.8rem; opacity: 0.85; min-width: 8rem; text-align: center; }
    .poc-progress-track {
      position: relative; width: min(400px, 80vw); height: 12px; padding: 4.5px 0;
      cursor: pointer; touch-action: none; box-sizing: content-box;
    }
    .poc-progress-track::before {
      content: ''; position: absolute; left: 0; right: 0; top: 4.5px; height: 3px;
      background: rgba(255,255,255,0.15); border-radius: 2px;
    }
    .poc-progress-tick { position: absolute; top: 4.5px; width: 2px; height: 3px; background: rgba(255,255,255,0.5); }
    .poc-progress-fill { position: absolute; left: 0; top: 4.5px; height: 3px; background: #e0a020; border-radius: 2px; }
    .poc-progress-handle {
      position: absolute; top: 50%; width: 10px; height: 10px; margin-left: -5px;
      background: #e0a020; border-radius: 50%; transform: translateY(-50%);
    }

    :host.immersive .poc-bar, :host.immersive .poc-tts-anchor { display: none; }
  `],
  host: {'[class.immersive]': 'readerSettingsService.immersiveMode()'},
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FoliateReaderPocComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly readerService = inject(ReaderService);
  private readonly annotationService = inject(AnnotationService);
  private readonly epubMenuService = inject(EpubReaderMenuService);
  private readonly drawerService = inject(DrawerService);
  private readonly themeService = inject(ThemeService);
  private readonly navService = inject(NavService);
  private readonly document = inject(DOCUMENT);
  protected readonly readerSettingsService = inject(EpubReaderSettingsService);
  protected readonly tts = inject(TtsService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly container = viewChild.required<ElementRef<HTMLDivElement>>('container');

  protected chapterId = 0;
  protected status = signal('loading...');

  // Toolbar auto-hide: visible by default, hides itself TOOLBAR_AUTO_HIDE_MS after the last mousemove/wheel/touch
  // over the reading area (see onActivity()) - matches typical fullscreen-reader/video-player UX rather than
  // requiring an explicit click to toggle, the way the old reader's action bar worked.
  protected readonly barVisible = signal(true);
  private hideBarTimer: ReturnType<typeof setTimeout> | null = null;

  protected readonly relocateInfo = signal<FoliateRelocateDetail | null>(null);
  protected readonly progressPercent = computed(() => Math.round((this.relocateInfo()?.fraction ?? 0) * 100));
  protected readonly progressLabel = computed(() => {
    const loc = this.relocateInfo();
    if (!loc) return '';
    return `Section ${loc.section.current + 1} of ${loc.section.total} · ${this.progressPercent()}%`;
  });
  /** Top-level TOC entries' positions along the overall book, as percentages - drawn as tick marks on the
   * progress track. Approximate (section-index / total-sections), not byte-weighted, but accurate enough to be
   * useful as a visual chapter guide. Populated once after the book opens - see load(). */
  protected readonly chapterMarkers = signal<number[]>([]);

  private libraryId = 0;
  private seriesId = 0;
  private volumeId = 0;
  private view: FoliateView | null = null;
  private readonly progressSave$ = new Subject<void>();
  private annotationsNeedingCfi: Annotation[] = [];
  private annotationsBySection = new Map<number, Annotation[]>();
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  private overlayerModule: any = null;
  private currentSectionDoc: Document | null = null;

  constructor() {
    // Fired by AnnotationService.createAnnotation/updateAnnotation/deleteAnnotation regardless of which UI
    // triggered them - the drawer opened below doesn't hand back the saved annotation directly, so reacting to
    // this (already-existing, decoupled) event is simpler than threading a callback through it.
    effect(() => {
      const event = this.annotationService.events();
      if (!event || event.annotation.chapterId !== this.chapterId) return;
      this.handleAnnotationEvent(event.type, event.annotation);
    });
  }

  ngOnInit(): void {
    const chapterId = this.route.snapshot.paramMap.get('chapterId');
    const libraryId = this.route.snapshot.paramMap.get('libraryId') ?? this.route.parent?.snapshot.paramMap.get('libraryId');
    const seriesId = this.route.snapshot.paramMap.get('seriesId') ?? this.route.parent?.snapshot.paramMap.get('seriesId');
    if (!chapterId || !libraryId || !seriesId) { this.status.set('missing route params'); return; }

    this.chapterId = parseInt(chapterId, 10);
    this.libraryId = parseInt(libraryId, 10);
    this.seriesId = parseInt(seriesId, 10);

    // Same nav/theme handling every reader in this app does on entry (see e.g. pdf-reader.component.ts) -
    // restored on destroy below. The reader itself renders as a fixed full-viewport overlay regardless (see
    // :host's position/z-index in the component styles), so this mostly matters for state other components
    // read (keybind scoping, wake lock elsewhere), not for anything visually uncovered by that overlay.
    this.navService.hideNavBar();
    this.navService.hideSideNav();
    this.themeService.clearThemes();
    this.document.body.classList.add('librariann-reader-route');

    this.progressSave$.pipe(
      debounceTime(PROGRESS_SAVE_DEBOUNCE_MS),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe(() => this.saveProgress());

    // Same settings service the current reader uses, unmodified - it's reader-agnostic (signals + a
    // settingUpdates$ stream), the current reader is just its only subscriber today. See handleSettingUpdate().
    this.readerSettingsService.settingUpdates$.pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe(update => this.handleSettingUpdate(update));

    this.load().catch(err => {
      console.error('[FoliateReaderPoc] failed to load', err);
      this.status.set('error: ' + (err?.message ?? String(err)));
    });

    this.destroyRef.onDestroy(() => {
      this.saveProgress();
      this.tts.stop();
      this.view?.close?.();
      if (this.hideBarTimer) clearTimeout(this.hideBarTimer);
      this.navService.showNavBar();
      this.navService.showSideNav();
      this.document.body.classList.remove('librariann-reader-route');
    });
  }

  private async load(): Promise<void> {
    this.status.set('fetching chapter info...');
    const readingProfile = this.route.snapshot.data['readingProfile'] as ReadingProfile;
    const [info, progress, annotations] = await Promise.all([
      firstValueFrom(this.readerService.getChapterInfo(this.chapterId)),
      firstValueFrom(this.readerService.getProgress(this.chapterId)),
      firstValueFrom(this.annotationService.getAllAnnotations(this.chapterId)),
      this.readerSettingsService.initialize(this.libraryId, this.seriesId, readingProfile),
    ]);
    this.volumeId = info?.volumeId ?? 0;
    // Only the ones that still need the one-time XPath->CFI backfill (see class doc comment, Stage 3).
    this.annotationsNeedingCfi = annotations.filter(a => !a.cfi && a.xPath);
    // All of them, keyed by section, for drawing - includes ones just backfilled above once they get a cfi.
    for (const annotation of annotations) {
      const list = this.annotationsBySection.get(annotation.pageNumber) ?? [];
      list.push(annotation);
      this.annotationsBySection.set(annotation.pageNumber, list);
    }

    this.status.set('fetching epub...');
    const url = this.readerService.getBookFileUrl(this.chapterId);
    const resp = await fetch(url);
    if (!resp.ok) throw new Error(`book-file fetch failed: ${resp.status}`);
    // foliate-js's makeBook() destructures {name, type} off the file to detect CBZ/FB2/MOBI (e.g.
    // name.endsWith('.cbz')) before falling through to EPUB - a plain Blob has no .name and throws there, so
    // wrap it as a File (which does) rather than passing the Blob directly.
    const blob = new File([await resp.blob()], `chapter-${this.chapterId}.epub`, {type: 'application/epub+zip'});

    this.status.set('loading foliate-js...');
    // Loaded by URL at runtime, not bundled - see UI/Web/src/assets/foliate-js/VENDORED.md for why. The
    // Function-constructor indirection keeps both TypeScript's module resolution and esbuild's static
    // dynamic-import bundling from ever seeing this specifier, so it stays a genuine browser-resolved import
    // against the real file (needed so view.js's own internal relative imports, e.g. './epub.js', keep working).
    await dynamicImport('/assets/foliate-js/view.js');
    // Same module instance view.js itself uses internally (ES modules are singleton-cached per URL) - we just
    // need our own reference to Overlayer.highlight as the draw preset for rendering (see the class doc
    // comment). CFI generation goes through view.getCFI(), not a direct epubcfi.js import - see its own doc
    // comment on the FoliateView type above for why.
    this.overlayerModule = await dynamicImport('/assets/foliate-js/overlayer.js');

    this.status.set('opening book...');
    const view = document.createElement('foliate-view') as unknown as FoliateView;
    this.container().nativeElement.appendChild(view);
    this.view = view;

    view.addEventListener('relocate', (e: Event) => {
      this.relocateInfo.set((e as CustomEvent<FoliateRelocateDetail>).detail);
      this.progressSave$.next();
    });
    view.addEventListener('load', (e: Event) => {
      const {doc, index} = (e as CustomEvent<{doc: Document; index: number}>).detail;
      this.currentSectionDoc = doc;
      this.backfillAnnotationCfis(doc, index);
      this.watchForNewSelections(doc, index);
      this.watchForTapToPaginate(doc);
      // view.js dispatches `load` from *inside* the renderer's own `view.load()` promise chain, before it
      // continues on to dispatch `create-overlayer` and attach the section's Overlayer instance
      // (paginator.js's #display(): `await view.load(...)` triggers `load` internally, then a couple more
      // microtask hops later fires `create-overlayer`). Calling view.addAnnotation() synchronously here loses
      // that race - it resolves "successfully" (view.js's own addAnnotation swallows a missing overlayer as a
      // silent no-op, not an error) but draws nothing, since #getOverlayer(index) finds nothing attached yet.
      // A macrotask reliably runs after that microtask chain drains, unlike a Promise.resolve().then() hop.
      setTimeout(() => this.drawAnnotationsForSection(index), 0);
    });
    // Emitted by foliate-js per annotation once view.addAnnotation() resolves its CFI to a real Range - we
    // decide how it's actually drawn/styled, foliate-js just handles the CFI resolution + overlay bookkeeping.
    view.addEventListener('draw-annotation', (e: Event) => {
      const {draw, annotation} = (e as CustomEvent<DrawAnnotationDetail>).detail;
      const slot = annotation.selectedSlotIndex ?? 0;
      draw(this.overlayerModule.Overlayer.highlight, {color: `var(--librariann-highlight-slot-${slot})`});
    });
    // Click on an existing highlight - reuses the same drawer/service the current reader's own click-to-view
    // flow uses (EpubReaderMenuService.openViewAnnotationDrawer); nothing reader-specific about that drawer.
    view.addEventListener('show-annotation', (e: Event) => {
      const {value: cfi} = (e as CustomEvent<{value: string; index: number; range: Range}>).detail;
      const annotation = [...this.annotationsBySection.values()].flat().find(a => a.cfi === cfi);
      if (!annotation) return;
      this.epubMenuService.openViewAnnotationDrawer(annotation, false, () => {});
    });

    await view.open(blob);
    // Settings arrived (via settingUpdates$) before the view existed to apply them to - readerSettingsService's
    // initialize() above already set every signal, so apply the current snapshot of all of them once now,
    // then let the subscription set up in ngOnInit() handle every change from here on.
    this.applyLayoutMode(this.readerSettingsService.layoutMode());
    this.applyBookStyles();
    this.computeChapterMarkers();

    // Resume position. Old progress rows (saved by the current reader) hold a descoped XPath string in
    // bookScrollId, not a CFI - goTo() on that would throw trying to parse it as one, so only trust it as a
    // CFI when it actually looks like one, otherwise fall back to the spine index (pageNum), which has always
    // meant "spine item index" for books in this app's progress model - true for both readers.
    const bookScrollId = progress?.bookScrollId;
    const target = bookScrollId?.startsWith('epubcfi(') ? bookScrollId : (progress?.pageNum ?? 0);
    try {
      await view.goTo(target);
    } catch (err) {
      console.warn('[FoliateReaderPoc] could not resume at saved position, opening from start', err);
      await view.goTo(0);
    }
    this.status.set('opened');
    // Starts the auto-hide countdown now that the toolbar has something real to show (progress/chapter info) -
    // without this it would stay visible indefinitely until the first mousemove, since the timer is otherwise
    // only (re)armed by onActivity().
    this.onActivity();
  }

  /**
   * One-time backfill: for annotations targeting this now-rendered section that don't have a CFI yet, resolve
   * their stored XPath/EndingXPath/SelectedText against the real section document and PATCH the computed CFI.
   * Best-effort - an annotation whose anchor can't be resolved (e.g. an XPath that no longer matches exactly)
   * is silently left for a later attempt, matching the old reader's own catch-and-swallow behavior in
   * AnnotationHelper.cs rather than surfacing an error the user can't act on.
   */
  private backfillAnnotationCfis(doc: Document, sectionIndex: number): void {
    const pending = this.annotationsNeedingCfi.filter(a => a.pageNumber === sectionIndex);
    if (pending.length === 0 || !this.view) return;

    for (const annotation of pending) {
      try {
        const range = buildRangeForAnnotation(doc, annotation.xPath, annotation.endingXPath, annotation.selectedText ?? '');
        if (!range) {
          console.warn(`[FoliateReaderPoc] could not build a range for annotation ${annotation.id} (xpath didn't resolve/text not found)`);
          continue;
        }

        // getCFI(), not a raw CFI.fromRange() - see the FoliateView type's doc comment on getCFI for why that
        // distinction is the difference between a CFI that resolves later and one that silently doesn't.
        const cfi = this.view.getCFI(sectionIndex, range);
        if (!cfi) continue;

        this.annotationService.setAnnotationCfi(annotation.id, cfi).subscribe({
          next: () => {
            annotation.cfi = cfi;
            console.log(`[FoliateReaderPoc] backfilled CFI for annotation ${annotation.id}: ${cfi}`);
            // Section is already loaded (that's why the backfill could run at all) - draw it now rather than
            // waiting for a `load` event that won't fire again for this section on its own.
            this.view?.addAnnotation({value: cfi, selectedSlotIndex: annotation.selectedSlotIndex});
          },
          error: err => console.warn(`[FoliateReaderPoc] failed to save backfilled CFI for annotation ${annotation.id}`, err),
        });
      } catch (err) {
        console.warn(`[FoliateReaderPoc] could not resolve annotation ${annotation.id} for CFI backfill`, err);
      }
    }

    this.annotationsNeedingCfi = this.annotationsNeedingCfi.filter(a => a.pageNumber !== sectionIndex);
  }

  /** Draws every already-CFI'd annotation targeting this section - see the class doc comment for the mechanism. */
  private drawAnnotationsForSection(sectionIndex: number): void {
    const annotations = this.annotationsBySection.get(sectionIndex) ?? [];
    for (const annotation of annotations) {
      if (!annotation.cfi) continue; // Not backfilled yet - backfillAnnotationCfis() draws it once it is.
      this.view?.addAnnotation({value: annotation.cfi, selectedSlotIndex: annotation.selectedSlotIndex});
    }
  }

  /**
   * Opens the existing create-annotation drawer (EpubReaderMenuService.openCreateAnnotationDrawer - the same
   * one the current reader uses) on a fresh text selection within this section. Unlike the current reader,
   * there's no intermediate floating Bookmark/Annotate toolbar yet - releasing a selection goes straight to the
   * create drawer. That toolbar is a separate, later UX polish item, not required for the underlying mechanism
   * (a v2-created annotation carries a real `cfi` and an empty `xPath` - the reverse of a pre-migration one -
   * both fields just get stored as given, see AnnotationService.CreateAnnotation).
   */
  private watchForNewSelections(doc: Document, sectionIndex: number): void {
    doc.addEventListener('mouseup', () => {
      const selection = doc.getSelection();
      const selectedText = selection?.toString() ?? '';
      if (!selection || selection.isCollapsed || selectedText.trim().length === 0 || !this.view) return;

      const range = selection.getRangeAt(0);
      const cfi = this.view.getCFI(sectionIndex, range);
      selection.removeAllRanges();

      const draft = {
        id: 0,
        xPath: '',
        endingXPath: '',
        cfi,
        selectedText,
        comment: '',
        containsSpoiler: false,
        pageNumber: sectionIndex,
        selectedSlotIndex: 0,
        chapterTitle: '',
        highlightCount: selectedText.length,
        ownerUserId: 0,
        ownerUsername: '',
        createdUtc: '',
        lastModifiedUtc: '',
        context: range.commonAncestorContainer.textContent ?? selectedText,
        chapterId: this.chapterId,
        libraryId: this.libraryId,
        volumeId: this.volumeId,
        seriesId: this.seriesId,
      } as Annotation;

      this.epubMenuService.openCreateAnnotationDrawer(draft, () => {});
    });
  }

  /**
   * Tap/click-to-paginate - the old reader's `clickToPaginate` setting, never ported to this reader at all
   * (a genuine gap, not a regression). Has to be attached directly to each section's own document, not the host
   * page's `.poc-container` - a click inside an iframe never bubbles out to the parent document's listeners, so
   * a handler on the outer container would simply never fire for clicks on the actual book content.
   *
   * Guarded against two real conflicts rather than pagination on every click:
   * - A genuine text selection (a real drag, not a tap) shouldn't page - `watchForNewSelections()`'s own
   *   `mouseup` handler already only acts on a non-collapsed selection, so the two don't fight over the same
   *   gesture, but this still needs its own check since `click` fires regardless of selection state.
   * - A click on a link or an existing highlight (an SVG overlay rect, drawn by overlayer.js) should keep its
   *   own behavior (following the link / firing foliate-js's own `show-annotation` event) instead of also
   *   paging underneath it.
   */
  private watchForTapToPaginate(doc: Document): void {
    doc.addEventListener('click', (e: MouseEvent) => {
      if (!this.readerSettingsService.clickToPaginate()) return;
      if (!doc.getSelection()?.isCollapsed) return;
      if ((e.target as Element | null)?.closest('a, svg')) return;

      const width = doc.defaultView?.innerWidth ?? 0;
      if (width === 0) return;
      if (e.clientX < width / 3) this.prev();
      else if (e.clientX > (width * 2) / 3) this.next();
    });
  }

  /** Keeps drawn highlights in sync with create/edit/delete regardless of which UI triggered them. */
  private handleAnnotationEvent(type: 'create' | 'edit' | 'delete', annotation: Annotation): void {
    for (const [section, list] of this.annotationsBySection) {
      this.annotationsBySection.set(section, list.filter(a => a.id !== annotation.id));
    }
    if (annotation.cfi && type !== 'delete') {
      this.view?.addAnnotation({value: annotation.cfi, selectedSlotIndex: annotation.selectedSlotIndex});
    } else if (annotation.cfi && type === 'delete') {
      this.view?.addAnnotation({value: annotation.cfi}, true);
    }
    if (type !== 'delete') {
      const list = this.annotationsBySection.get(annotation.pageNumber) ?? [];
      list.push(annotation);
      this.annotationsBySection.set(annotation.pageNumber, list);
    }
  }

  private saveProgress(): void {
    const loc = this.relocateInfo();
    if (!loc) return;
    this.readerService.saveProgress(this.libraryId, this.seriesId, this.volumeId, this.chapterId, loc.section.current, loc.cfi).subscribe();
  }

  /**
   * Reuses the current reader's own settings drawer (EpubSettingDrawerComponent via EpubReaderMenuService) -
   * it already only talks to EpubReaderSettingsService's signals/form, nothing reader-specific.
   */
  protected openSettings(): void {
    const profile = this.readerSettingsService.getCurrentReadingProfile();
    if (!profile) {
      // Shouldn't happen once load() has finished (initialize() sets this synchronously) - logged rather than
      // silently no-op'd so a real regression here is visible instead of just "the button does nothing".
      console.warn('[FoliateReaderPoc] openSettings() called before a reading profile was available');
      return;
    }
    this.epubMenuService.openSettingsDrawer(this.chapterId, this.seriesId, this.libraryId, profile, this.readerSettingsService);
  }

  /**
   * Table of contents - NOT EpubReaderMenuService.openViewTocDrawer() (that one is built around the old reader's
   * spine-index + XPath-anchor chapter model, which doesn't apply here). Reads foliate-js's own already-parsed
   * TOC directly and navigates via view.goTo(href), which the book's real nav document/NCX hrefs resolve
   * against natively - see TocDrawerComponent's doc comment.
   */
  protected openToc(): void {
    const toc = this.view?.book?.toc ?? [];
    const ref = this.drawerService.open(TocDrawerComponent, {position: 'end'});
    ref.setInput('toc', toc);
    ref.componentInstance.select.subscribe((href: string) => {
      this.view?.goTo(href);
    });
  }

  /** Reuses the current reader's own "browse all annotations" drawer - nothing reader-specific about it. */
  protected viewAnnotationsList(): void {
    this.epubMenuService.openViewAnnotationsDrawer((annotation: Annotation) => {
      if (annotation.cfi) this.view?.goTo(annotation.cfi);
    });
  }

  /** Reuses the same close/navigate-back logic every reader in this app uses (ReaderService.closeReader). */
  protected closeReader(): void {
    this.readerService.closeReader(this.libraryId, this.seriesId, this.chapterId);
  }

  /**
   * Top-level TOC entries' approximate position along the whole book (section-index / total-sections) - drawn
   * as tick marks on the progress track. book.resolveHref() is synchronous (a plain lookup against the already-
   * parsed spine/manifest), so this is a one-time, cheap computation done once right after the book opens.
   */
  private computeChapterMarkers(): void {
    const book = this.view?.book;
    const totalSections = book?.sections?.length ?? 0;
    if (!book?.toc || totalSections === 0) return;

    const markers = book.toc
      .map(item => book.resolveHref?.(item.href)?.index)
      .filter((index): index is number => typeof index === 'number')
      .map(index => (index / totalSections) * 100);
    this.chapterMarkers.set(markers);
  }

  /** Click-to-seek: jump straight to the clicked position on the progress track. */
  protected onProgressPointerDown(event: PointerEvent): void {
    const track = event.currentTarget as HTMLElement;
    track.setPointerCapture(event.pointerId);
    this.seekToPointerX(track, event.clientX);
  }

  /** Drag-to-seek: keep following the pointer while the primary button/touch stays down. */
  protected onProgressPointerMove(event: PointerEvent): void {
    if (event.buttons !== 1) return;
    this.seekToPointerX(event.currentTarget as HTMLElement, event.clientX);
  }

  private seekToPointerX(track: HTMLElement, clientX: number): void {
    const rect = track.getBoundingClientRect();
    const fraction = Math.min(1, Math.max(0, (clientX - rect.left) / rect.width));
    this.view?.goToFraction(fraction);
  }

  /**
   * Resets the toolbar auto-hide timer - bound to mousemove/wheel/touchstart on the reading area. Shows the bar
   * immediately on any activity, then hides it again TOOLBAR_AUTO_HIDE_MS after the last one, the same idiom
   * fullscreen video players use rather than requiring an explicit click to toggle (the old reader's behavior).
   */
  protected onActivity(): void {
    this.barVisible.set(true);
    this.scheduleHide();
  }

  /**
   * While the pointer is actually over a bar (even sitting still - a stationary mouse fires no mousemove events
   * at all, so onActivity() alone would let the countdown started by the *last* movement expire underneath a
   * cursor that never left), cancel any pending hide entirely rather than just postponing it. The countdown
   * only starts for real once the pointer leaves - see onBarLeave().
   */
  protected onBarEnter(): void {
    this.barVisible.set(true);
    if (this.hideBarTimer) {
      clearTimeout(this.hideBarTimer);
      this.hideBarTimer = null;
    }
  }

  protected onBarLeave(): void {
    this.scheduleHide();
  }

  private scheduleHide(): void {
    if (this.hideBarTimer) clearTimeout(this.hideBarTimer);
    this.hideBarTimer = setTimeout(() => this.barVisible.set(false), TOOLBAR_AUTO_HIDE_MS);
  }

  /**
   * Translates EpubReaderSettingsService's updates (built for the current column-pagination reader) onto
   * foliate-js's own mechanisms. Book text/theme/typography live inside foliate-js's iframe-rendered sections,
   * unreachable by the host page's CSS - `pageStyle`/`writingStyle`/`theme` all funnel into one combined
   * stylesheet pushed via `renderer.setStyles()` (applyBookStyles()) rather than being handled individually,
   * since a single section stylesheet has to represent all three together anyway. `layoutMode` maps onto the
   * `flow`/`max-column-count` attributes already used for pagination in Stage 1. `immersiveMode` is host-level
   * UI (hides the top bar, see the `[class.immersive]` host binding). `clickToPaginate`/`fullscreen` aren't
   * wired yet - not part of settings parity's core (text rendering/layout), tracked as a follow-up.
   */
  private handleSettingUpdate(update: ReaderSettingUpdate): void {
    switch (update.setting) {
      case 'pageStyle':
      case 'writingStyle':
        this.applyBookStyles();
        break;
      case 'layoutMode':
        this.applyLayoutMode(update.object as BookPageLayoutMode);
        break;
      case 'theme':
        this.themeService.setBookTheme((update.object as BookTheme).selector);
        this.applyBookStyles();
        break;
      case 'appearance':
        this.applyAppearance(update.object as {color: string; opacity: number});
        break;
      // readingDirection: the current reader also does nothing extra for this (see its own
      // handleReaderSettingsUpdate) - direction is expressed through the content's own language/script, not a
      // separate layout switch. clickToPaginate/fullscreen: not wired yet, see doc comment above.
    }
  }

  private applyLayoutMode(mode: BookPageLayoutMode): void {
    const renderer = this.view?.renderer;
    if (!renderer) return;
    switch (mode) {
      case BookPageLayoutMode.Default:
        renderer.setAttribute('flow', 'scrolled');
        break;
      case BookPageLayoutMode.Column1:
        renderer.setAttribute('flow', 'paginated');
        renderer.setAttribute('max-column-count', '1');
        break;
      case BookPageLayoutMode.Column2:
        renderer.setAttribute('flow', 'paginated');
        renderer.setAttribute('max-column-count', '2');
        break;
    }
  }

  /** Combines pageStyle + writingStyle + the active theme into one stylesheet for the current section's document. */
  private applyBookStyles(): void {
    const renderer = this.view?.renderer;
    if (!renderer) return;

    const styles = this.readerSettingsService.pageStyles();
    const isVertical = this.readerSettingsService.writingStyle() === WritingStyle.Vertical;
    const theme = this.readerSettingsService.activeTheme();

    // theme.colorHash is the theme's own representative background color (e.g. #292929 for Dark) - the actual
    // per-element background rules further down in theme.content only ever target *descendants* of
    // `.book-content`/`body`, never the element itself, so body's own background needs setting separately here.
    const rules = [
      `body { ${pageStyleToCss(styles)} ${isVertical ? 'writing-mode: vertical-rl;' : ''} ${theme ? `background-color: ${theme.colorHash} !important;` : ''} }`,
    ];
    // BookDarkTheme-style theme content scopes its actual book-text rules to `.book-content ...` (the current
    // reader's own content wrapper class) - that class doesn't exist inside a foliate-js section's document, so
    // reuse the same rules here by retargeting that selector to `body`, the equivalent scope in this context.
    if (theme) rules.push(theme.content.replace(/\.book-content\b/g, 'body'));

    // TtsService.highlight() adds this class to whichever element is currently being spoken (see tts.service.ts)
    // - but that class has no styling anywhere in the app's own stylesheets, since book text lives inside
    // foliate-js's per-section iframes, a separate document the host page's CSS can't reach at all. Has to be
    // injected here like everything else in this stylesheet. (This gap existed for the old reader too - it
    // shared the same host document as global CSS, so it merely never needed this injection, but the rule
    // itself was never actually defined anywhere either.)
    rules.push('.librariann-tts-speaking { background-color: rgba(255, 165, 0, 0.35) !important; border-radius: 2px; box-shadow: 0 0 0 2px rgba(255, 165, 0, 0.35); }');

    renderer.setStyles(rules.join('\n'));
  }

  private applyAppearance(appearance: {color: string; opacity: number}): void {
    const host = this.container().nativeElement;
    host.style.setProperty('--reader-background-color', appearance.color);
    host.style.setProperty('--reader-background-opacity', `${appearance.opacity}%`);
  }

  // Passed to the reused <app-tts-controls> (see its own contentRoot doc comment) - the current reader's
  // default (`.book-content` in the host document) doesn't apply here, content lives in foliate-js's own
  // iframe-rendered section instead. Bound, not called, so it always reads the *current* section at listen()
  // time regardless of how many pages have turned since <app-tts-controls> was created.
  protected readonly ttsContentRoot = (): HTMLElement | null => this.currentSectionDoc?.body ?? null;

  protected setTtsProvider(id: 'browser' | 'kokoro'): void {
    this.tts.setProvider(id);
  }

  protected next(): void { this.view?.renderer?.next?.(); }
  protected prev(): void { this.view?.renderer?.prev?.(); }
}

/**
 * Turns EpubReaderSettingsService's PageStyle map into plain CSS declarations. `!important` because an EPUB's
 * own embedded stylesheet (loaded before ours - see FoliateView.renderer.setStyles's doc comment) can otherwise
 * out-specificity a bare `body { margin-left: ... }` rule with something like `body { margin: 0 }` of its own.
 */
function pageStyleToCss(styles: PageStyle): string {
  return Object.entries(styles)
    .map(([prop, value]) => `${prop}: ${value} !important;`)
    .join(' ');
}

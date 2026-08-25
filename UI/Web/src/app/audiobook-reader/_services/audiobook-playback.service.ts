import {computed, DestroyRef, inject, Injectable, signal} from '@angular/core';
import {Router} from '@angular/router';
import {debounceTime, map, Subject} from 'rxjs';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {ReaderService} from '../../_services/reader.service';
import {ImageService} from '../../_services/image.service';
import {ChapterInfo} from '../../manga-reader/_models/chapter-info';
import {AudiobookChapterMarker} from '../../_models/readers/audiobook-chapter-marker';
import {MangaFormat} from '../../_models/manga-format';

const SLEEP_TIMER_OPTIONS_MINUTES = [5, 15, 30, 45, 60];
const PROGRESS_SAVE_DEBOUNCE_MS = 5000;
const REWIND_SECONDS = 10;
const FORWARD_SECONDS = 30;

/**
 * Owns the single, app-wide <audio> element for audiobook playback, outside of Angular's component tree, so
 * playback survives navigating away from the reader page - matching Plex's "keep playing behind a mini player
 * until you stop it or start something else" behavior. AudiobookReaderComponent (the full-page view) and
 * MiniPlayerComponent (the persistent bottom bar) are both just UI layers reading/driving this shared state.
 */
@Injectable({providedIn: 'root'})
export class AudiobookPlaybackService {
  private readonly readerService = inject(ReaderService);
  private readonly imageService = inject(ImageService);
  private readonly router = inject(Router);

  private readonly audio = new Audio();

  readonly libraryId = signal<number>(0);
  readonly seriesId = signal<number>(0);
  readonly chapterId = signal<number>(0);
  readonly chapterInfo = signal<ChapterInfo | null>(null);
  readonly chapterMarkers = signal<AudiobookChapterMarker[]>([]);
  readonly isPlaying = signal(false);
  readonly currentTime = signal(0);
  readonly duration = signal(0);
  readonly playbackRate = signal(1);
  readonly volume = signal(1);
  readonly sleepTimerMinutesRemaining = signal<number | null>(null);

  /** Whether any audiobook is currently loaded (playing or paused) - drives mini player visibility. */
  readonly hasActiveSession = computed(() => this.chapterId() > 0);
  readonly progressPercent = computed(() => {
    const d = this.duration();
    return d > 0 ? (this.currentTime() / d) * 100 : 0;
  });
  readonly remainingTime = computed(() => Math.max(0, this.duration() - this.currentTime()));
  // Metadata matching (Fix Match) applies covers at the series level, same place the library grid's thumbnail
  // reads from - a chapter-level cover is a separate, usually-unset thing (only populated if extracted directly
  // from the audio file's own embedded art), so use the series cover here to match what's actually there.
  readonly coverImageUrl = computed(() => this.seriesId() > 0 ? this.imageService.getSeriesCoverImage(this.seriesId()) : '');

  readonly speedOptions = [0.75, 1, 1.25, 1.5, 1.75, 2];
  readonly sleepTimerOptions = SLEEP_TIMER_OPTIONS_MINUTES;
  readonly REWIND_SECONDS = REWIND_SECONDS;
  readonly FORWARD_SECONDS = FORWARD_SECONDS;

  private sleepTimerHandle: ReturnType<typeof setTimeout> | null = null;
  private sleepTimerTickHandle: ReturnType<typeof setInterval> | null = null;
  private readonly progressSave$ = new Subject<void>();
  private hasAppliedInitialResumePosition = false;

  constructor() {
    this.audio.addEventListener('loadedmetadata', () => this.onLoadedMetadata());
    // Some M4B files store their duration atom at the end of the file rather than the start - the browser
    // reports Infinity/NaN on loadedmetadata and only fires durationchange once it actually knows the real
    // value (sometimes more than once). Keep taking the latest one rather than trusting loadedmetadata alone.
    this.audio.addEventListener('durationchange', () => {
      if (isFinite(this.audio.duration) && this.audio.duration > 0) this.duration.set(this.audio.duration);
    });
    this.audio.addEventListener('timeupdate', () => this.onTimeUpdate());
    this.audio.addEventListener('play', () => this.isPlaying.set(true));
    this.audio.addEventListener('pause', () => { this.isPlaying.set(false); this.saveProgress(); });
    this.audio.addEventListener('ended', () => { this.isPlaying.set(false); this.saveProgress(); });
    this.audio.addEventListener('error', () => {
      const err = this.audio.error;
      console.error('[AudiobookPlaybackService] <audio> error', err?.code, err?.message);
    });

    // A DestroyRef in a root-provided service only fires on app teardown (never, in practice, for an SPA) -
    // used here just so the debounce subscription has a documented lifetime rather than living forever unmanaged.
    const destroyRef = inject(DestroyRef);
    this.progressSave$.pipe(
      debounceTime(PROGRESS_SAVE_DEBOUNCE_MS),
      takeUntilDestroyed(destroyRef),
    ).subscribe(() => this.saveProgress());
  }

  /**
   * Loads a chapter and starts playback. If a different chapter is already playing, it's stopped (progress
   * saved) first - matching "starting something else replaces what was playing", same as Plex.
   */
  load(libraryId: number, seriesId: number, chapterId: number): void {
    if (this.chapterId() === chapterId) {
      // Already loaded (e.g. navigating back into the full reader for the item already in the mini player) -
      // just resume/keep playing, don't reload the source and lose position.
      return;
    }

    if (this.hasActiveSession()) {
      this.saveProgress();
    }

    this.libraryId.set(libraryId);
    this.seriesId.set(seriesId);
    this.chapterId.set(chapterId);
    this.chapterInfo.set(null);
    this.chapterMarkers.set([]);
    this.isPlaying.set(false);
    this.currentTime.set(0);
    this.duration.set(0);
    this.hasAppliedInitialResumePosition = false;
    this.clearSleepTimer();

    this.readerService.getChapterInfo(chapterId).subscribe(info => this.chapterInfo.set(info));
    this.readerService.getAudiobookChapterMarkers(chapterId).subscribe(markers => this.chapterMarkers.set(markers ?? []));

    this.audio.src = this.readerService.getAudiobookStreamUrl(chapterId);
    this.audio.playbackRate = this.playbackRate();
    this.audio.volume = this.volume();
    // Do NOT auto-play here: by the time this runs (after route resolution/HTTP calls), the browser no longer
    // considers it a user gesture, so autoplay gets silently blocked. Loading a chapter just cues it up -
    // playback starts only from an explicit user click (togglePlayPause/play), same as before this was
    // refactored into a shared service.
  }

  private onLoadedMetadata(): void {
    this.duration.set(this.audio.duration || 0);
    if (this.hasAppliedInitialResumePosition) return;
    this.hasAppliedInitialResumePosition = true;

    this.readerService.getProgress(this.chapterId()).subscribe(progress => {
      const resumeAt = progress?.playbackPositionSeconds;
      if (resumeAt && resumeAt > 0 && resumeAt < this.audio.duration) {
        this.audio.currentTime = resumeAt;
        this.currentTime.set(resumeAt);
      }
    });
  }

  private onTimeUpdate(): void {
    this.currentTime.set(this.audio.currentTime);
    this.progressSave$.next();
  }

  togglePlayPause(): void {
    if (this.audio.paused) this.play(); else this.pause();
  }

  // These are called directly from click handlers, so they're a real user gesture and autoplay restrictions
  // don't apply - still swallow a rejected play() promise (e.g. a mid-request navigation) rather than letting
  // it surface as an unhandled rejection in the console.
  play(): void { this.audio.play().catch(err => console.warn('[AudiobookPlaybackService] play() failed', err)); }
  pause(): void { this.audio.pause(); }

  seekRelative(deltaSeconds: number): void {
    this.audio.currentTime = Math.min(Math.max(0, this.audio.currentTime + deltaSeconds), this.audio.duration || 0);
  }

  seekTo(seconds: number): void {
    this.audio.currentTime = Math.min(Math.max(0, seconds), this.audio.duration || 0);
  }

  jumpToMarker(marker: AudiobookChapterMarker): void {
    this.seekTo(marker.startSeconds);
  }

  setPlaybackRate(rate: number): void {
    this.audio.playbackRate = rate;
    this.playbackRate.set(rate);
  }

  setVolume(vol: number): void {
    this.audio.volume = vol;
    this.volume.set(vol);
  }

  /** Cross-Chapter navigation (e.g. multi-file audiobooks). Navigates the app to the new chapter's reader route. */
  goToAdjacentChapter(direction: 'next' | 'prev'): void {
    const info = this.chapterInfo();
    if (!info) return;

    this.saveProgress();
    const lookup$ = direction === 'next'
      ? this.readerService.getNextChapter(this.seriesId(), info.volumeId, this.chapterId())
      : this.readerService.getPrevChapter(this.seriesId(), info.volumeId, this.chapterId());

    lookup$.subscribe(newChapterId => {
      if (!newChapterId || newChapterId <= 0 || newChapterId === this.chapterId()) return;
      this.router.navigate(this.readerService.getNavigationArray(this.libraryId(), this.seriesId(), newChapterId, MangaFormat.AUDIO))
        .catch(err => console.error(err));
    });
  }

  startSleepTimer(minutes: number): void {
    this.clearSleepTimer();
    let remaining = minutes * 60;
    this.sleepTimerMinutesRemaining.set(Math.ceil(remaining / 60));

    this.sleepTimerTickHandle = setInterval(() => {
      remaining -= 1;
      this.sleepTimerMinutesRemaining.set(Math.max(0, Math.ceil(remaining / 60)));
    }, 60_000);

    this.sleepTimerHandle = setTimeout(() => {
      this.audio.pause();
      this.clearSleepTimer();
    }, minutes * 60_000);
  }

  clearSleepTimer(): void {
    if (this.sleepTimerHandle) clearTimeout(this.sleepTimerHandle);
    if (this.sleepTimerTickHandle) clearInterval(this.sleepTimerTickHandle);
    this.sleepTimerHandle = null;
    this.sleepTimerTickHandle = null;
    this.sleepTimerMinutesRemaining.set(null);
  }

  /** Navigates to the full-page reader for whatever's currently loaded (used by the mini player). */
  openFullPlayer(): void {
    if (!this.hasActiveSession()) return;
    this.router.navigate(
      this.readerService.getNavigationArray(this.libraryId(), this.seriesId(), this.chapterId(), MangaFormat.AUDIO)
    ).catch(err => console.error(err));
  }

  /** Fully stops playback and clears the session - hides the mini player, matching Plex's stop button. */
  stop(): void {
    this.saveProgress();
    this.audio.pause();
    this.audio.removeAttribute('src');
    this.audio.load();
    this.clearSleepTimer();
    this.libraryId.set(0);
    this.seriesId.set(0);
    this.chapterId.set(0);
    this.chapterInfo.set(null);
    this.chapterMarkers.set([]);
    this.isPlaying.set(false);
    this.currentTime.set(0);
    this.duration.set(0);
  }

  formatTime(totalSeconds: number): string {
    const seconds = Math.max(0, Math.floor(totalSeconds || 0));
    const h = Math.floor(seconds / 3600);
    const m = Math.floor((seconds % 3600) / 60);
    const s = seconds % 60;
    const pad = (n: number) => n.toString().padStart(2, '0');
    return h > 0 ? `${h}:${pad(m)}:${pad(s)}` : `${m}:${pad(s)}`;
  }

  private saveProgress(): void {
    const duration = this.duration();
    const position = this.currentTime();
    const chapterId = this.chapterId();
    if (duration <= 0 || chapterId <= 0) return;

    const percentComplete = Math.round((position / duration) * 100);
    const info = this.chapterInfo();

    this.readerService.saveProgress(
      this.libraryId(), this.seriesId(), info?.volumeId ?? 0, chapterId,
      percentComplete, null, position,
    ).subscribe();
  }
}

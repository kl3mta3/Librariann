import {ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute} from '@angular/router';
import {TranslocoDirective} from '@jsverse/transloco';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {map} from 'rxjs';
import {ReaderService} from '../../../_services/reader.service';
import {AudiobookPlaybackService} from '../../_services/audiobook-playback.service';
import {AudiobookChapterMarker} from '../../../_models/readers/audiobook-chapter-marker';

/**
 * Full-page audiobook player. All actual playback state/logic lives in AudiobookPlaybackService (a root-provided
 * singleton) so it survives navigating away from this page - this component is just a view over it, plus the
 * page-local drawer-open UI state.
 */
@Component({
  selector: 'app-audiobook-reader',
  standalone: true,
  imports: [TranslocoDirective],
  templateUrl: './audiobook-reader.component.html',
  styleUrl: './audiobook-reader.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AudiobookReaderComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly readerService = inject(ReaderService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly playback = inject(AudiobookPlaybackService);

  protected readonly showChaptersDrawer = signal(false);
  protected readonly showSleepDrawer = signal(false);
  protected readonly showSpeedDrawer = signal(false);
  protected readonly showVolumeDrawer = signal(false);

  ngOnInit(): void {
    const libraryId = this.route.snapshot.paramMap.get('libraryId') ?? this.route.parent?.snapshot.paramMap.get('libraryId');
    const seriesId = this.route.snapshot.paramMap.get('seriesId') ?? this.route.parent?.snapshot.paramMap.get('seriesId');
    if (!libraryId || !seriesId) return;

    const libraryIdNum = parseInt(libraryId, 10);
    const seriesIdNum = parseInt(seriesId, 10);

    this.route.paramMap.pipe(
      map(params => params.get('chapterId')),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe(chapterId => {
      if (!chapterId) return;
      this.playback.load(libraryIdNum, seriesIdNum, parseInt(chapterId, 10));
    });
  }

  protected onScrubberClick(event: MouseEvent, track: HTMLElement): void {
    const rect = track.getBoundingClientRect();
    const ratio = Math.min(1, Math.max(0, (event.clientX - rect.left) / rect.width));
    this.playback.seekTo(ratio * this.playback.duration());
  }

  protected jumpToMarker(marker: AudiobookChapterMarker): void {
    this.playback.jumpToMarker(marker);
    this.showChaptersDrawer.set(false);
  }

  protected setPlaybackRate(rate: number): void {
    this.playback.setPlaybackRate(rate);
    this.showSpeedDrawer.set(false);
  }

  protected startSleepTimer(minutes: number): void {
    this.playback.startSleepTimer(minutes);
    this.showSleepDrawer.set(false);
  }

  protected toggleDrawer(drawer: 'chapters' | 'speed' | 'volume' | 'sleep'): void {
    const all = {
      chapters: this.showChaptersDrawer,
      speed: this.showSpeedDrawer,
      volume: this.showVolumeDrawer,
      sleep: this.showSleepDrawer,
    };
    const target = all[drawer];
    const wasOpen = target();
    Object.values(all).forEach(s => s.set(false));
    target.set(!wasOpen);
  }

  protected close(): void {
    // Leaving the full-page reader doesn't stop playback - it keeps going in the mini player, matching Plex.
    // Explicitly stopping is a separate action (the mini player's stop button).
    this.readerService.closeReader(this.playback.libraryId(), this.playback.seriesId(), this.playback.chapterId());
  }
}

import {ChangeDetectionStrategy, Component, computed, inject} from '@angular/core';
import {NavigationEnd, Router} from '@angular/router';
import {filter, map} from 'rxjs';
import {toSignal} from '@angular/core/rxjs-interop';
import {TranslocoDirective} from '@jsverse/transloco';
import {AudiobookPlaybackService} from '../../_services/audiobook-playback.service';

/**
 * Persistent bottom bar shown whenever an audiobook is loaded (playing or paused) and the user has navigated
 * away from that chapter's full-page reader - mirrors Plex's "keep the mini player until you stop it or start
 * something else" behavior. Playback itself lives in AudiobookPlaybackService, not here - this is just a view.
 */
@Component({
  selector: 'app-audiobook-mini-player',
  standalone: true,
  imports: [TranslocoDirective],
  templateUrl: './mini-player.component.html',
  styleUrl: './mini-player.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MiniPlayerComponent {
  private readonly router = inject(Router);
  protected readonly playback = inject(AudiobookPlaybackService);

  private readonly currentUrl = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map(event => event.urlAfterRedirects),
    ),
    {initialValue: this.router.url},
  );

  /** Hidden while the full-page reader for (any) audiobook chapter is open, to avoid duplicate controls. */
  protected readonly isOnFullReader = computed(() => this.currentUrl().includes('/audiobook/'));
  protected readonly visible = computed(() => this.playback.hasActiveSession() && !this.isOnFullReader());

  protected open(): void {
    this.playback.openFullPlayer();
  }

  protected togglePlayPause(event: Event): void {
    event.stopPropagation();
    this.playback.togglePlayPause();
  }

  protected seekRelative(event: Event, seconds: number): void {
    event.stopPropagation();
    this.playback.seekRelative(seconds);
  }

  protected stop(event: Event): void {
    event.stopPropagation();
    this.playback.stop();
  }
}

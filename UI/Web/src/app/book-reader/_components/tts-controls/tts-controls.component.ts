import {DOCUMENT} from '@angular/common';
import {ChangeDetectionStrategy, Component, inject, input, OnDestroy, signal} from '@angular/core';
import {TranslocoDirective} from '@jsverse/transloco';
import {TtsService} from '../../../_services/tts.service';

@Component({
  selector: 'app-tts-controls',
  imports: [TranslocoDirective],
  templateUrl: './tts-controls.component.html',
  styleUrl: './tts-controls.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TtsControlsComponent implements OnDestroy {
  private readonly document = inject(DOCUMENT);
  protected readonly tts = inject(TtsService);
  protected readonly open = signal(false);

  /**
   * Where to read text from. Defaults to the current reader's `.book-content` (unchanged behavior for existing
   * callers); the foliate-js reader passes its current section's own `<iframe>` body instead, since its content
   * doesn't live in the host page's DOM at all. TtsService itself needed no changes for this - it already just
   * takes whatever root `listen()` is given.
   */
  readonly contentRoot = input<() => HTMLElement | null>(() => this.document.querySelector<HTMLElement>('.book-content'));

  listen(): void {
    const content = this.contentRoot()();
    if (content) this.tts.listen(content);
  }

  numericValue(event: Event): number {
    return Number((event.target as HTMLInputElement).value);
  }

  stringValue(event: Event): string {
    return (event.target as HTMLSelectElement).value;
  }

  ngOnDestroy(): void {
    this.tts.stop();
  }
}

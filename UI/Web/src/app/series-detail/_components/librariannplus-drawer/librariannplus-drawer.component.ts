import {ChangeDetectionStrategy, Component, inject, input} from '@angular/core';
import {NgbActiveOffcanvas} from '@ng-bootstrap/ng-bootstrap';
import {TranslocoDirective} from '@jsverse/transloco';
import {LibrariannplusTooltipComponent} from '../librariannplus-tooltip/librariannplus-tooltip.component';
import {OffCanvasResizeComponent, ResizeMode} from '../../../shared/_components/off-canvas-resize/off-canvas-resize.component';
import {BreakpointService} from '../../../_services/breakpoint.service';

@Component({
  selector: 'app-librariannplus-drawer',
  templateUrl: './librariannplus-drawer.component.html',
  styleUrls: ['./librariannplus-drawer.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoDirective, LibrariannplusTooltipComponent, OffCanvasResizeComponent],
})
export class LibrariannplusDrawerComponent {
  private readonly activeOffcanvas = inject(NgbActiveOffcanvas);
  readonly breakpointService = inject(BreakpointService);

  seriesId = input.required<number>();

  protected readonly ResizeMode = ResizeMode;
  protected readonly window = window;

  close() {
    this.activeOffcanvas.close();
  }
}

import {ChangeDetectionStrategy, Component, EventEmitter, Output, inject, input} from '@angular/core';
import {NgTemplateOutlet} from '@angular/common';
import {NgbActiveOffcanvas} from '@ng-bootstrap/ng-bootstrap';
import {TranslocoDirective} from '@jsverse/transloco';
import {FoliateTocItem} from './foliate-reader-poc.component';

/**
 * Table of contents drawer for the foliate-js reader. Deliberately NOT a reuse of the old reader's
 * ViewTocDrawerComponent - that one is built around BookService.getBookChapters()'s server-parsed, spine-index +
 * XPath-anchor model (BookChapterItem[]), which has no equivalent in the CFI/href-based world this reader
 * navigates in. foliate-js already parses the EPUB's own real table of contents (its nav document/NCX) into
 * exactly this {label, href, subitems} tree - reading that directly is both simpler and more accurate than
 * re-deriving something equivalent server-side.
 */
@Component({
  selector: 'app-toc-drawer',
  standalone: true,
  imports: [TranslocoDirective, NgTemplateOutlet],
  template: `
    <ng-container *transloco="let t; prefix: 'view-toc-drawer'">
      <div class="offcanvas-header">
        <h5 class="offcanvas-title">{{t('title')}}</h5>
        <button type="button" class="btn-unstyled ms-auto" [attr.aria-label]="t('close')" (click)="close()">
          <i class="fas fa-times" aria-hidden="true"></i>
        </button>
      </div>
      <div class="offcanvas-body">
        @if (toc().length === 0) {
          <p class="text-muted">{{t('no-chapters')}}</p>
        } @else {
          <ng-container [ngTemplateOutlet]="list" [ngTemplateOutletContext]="{items: toc()}" />
        }

        <ng-template #list let-items="items">
          <ul class="toc-list">
            @for (item of items; track item.href) {
              <li>
                <button type="button" class="btn-unstyled toc-item" (click)="onSelect(item.href)">{{item.label}}</button>
                @if (item.subitems && item.subitems.length > 0) {
                  <ng-container [ngTemplateOutlet]="list" [ngTemplateOutletContext]="{items: item.subitems}" />
                }
              </li>
            }
          </ul>
        </ng-template>
      </div>
    </ng-container>
  `,
  styles: [`
    .toc-list { list-style: none; padding-left: 0.75rem; margin: 0; }
    .toc-list .toc-list { padding-left: 1.25rem; }
    .toc-item { display: block; width: 100%; text-align: left; padding: 0.35rem 0; background: none; border: none; color: inherit; }
    .toc-item:hover { text-decoration: underline; }
  `],
})
export class TocDrawerComponent {
  private readonly activeOffcanvas = inject(NgbActiveOffcanvas);

  readonly toc = input<FoliateTocItem[]>([]);

  @Output() select = new EventEmitter<string>();

  onSelect(href: string): void {
    this.select.emit(href);
    this.activeOffcanvas.close();
  }

  close(): void {
    this.activeOffcanvas.close();
  }
}

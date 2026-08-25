import {ChangeDetectionStrategy, Component, computed, DestroyRef, inject, OnInit, signal} from '@angular/core';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {TranslocoDirective} from '@jsverse/transloco';
import {NgbNav, NgbNavItem, NgbNavLink} from '@ng-bootstrap/ng-bootstrap';
import {LibrariannPlusAuditService} from '../../_services/librariannplus-audit.service';
import {ScrobbleProvider, ScrobblingService} from '../../_services/scrobbling.service';
import {LibrariannPlusAuditEntry} from '../../_models/librariannplus/librariann-plus-audit-entry';
import {LibrariannPlusAuditCategory} from '../../_models/librariannplus/librariann-plus-audit-category.enum';
import {AuditStatus} from '../../_models/librariannplus/audit-status.enum';
import {LibrariannplusTimelineComponent} from '../../_single-module/librariannplus-timeline/librariannplus-timeline.component';
import {
  LibrariannPlusAuditEntryComponent
} from '../../admin/librariann-plus/librariannplus-audit-entry/librariann-plus-audit-entry.component';
import {LibrariannPlusEventType} from "../../_models/librariannplus/librariann-plus-event-type.enum";
import {Tabs} from "../../_models/tabs";
import {TabTitlePipe} from "../../_pipes/tab-title.pipe";
import {Pagination} from '../../_models/pagination';
import {UserScrobbleProvider} from "../../_models/librariannplus/scrobble-providers/user-scrobble-provider";
import {DefaultValuePipe} from "../../_pipes/default-value.pipe";
import {TimeDifferencePipe} from "../../_pipes/time-difference.pipe";
import {UtcToLocalDatePipe} from "../../_pipes/utc-to-locale-date.pipe";
import {LibrariannPlusMyAuditStats} from "../../_models/librariannplus/librariann-plus-audit-stats";

@Component({
  selector: 'app-librariannplus-activity',
  templateUrl: './librariannplus-activity.component.html',
  styleUrls: ['./librariannplus-activity.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoDirective, NgbNav, NgbNavItem, NgbNavLink, LibrariannplusTimelineComponent,
    LibrariannPlusAuditEntryComponent, TabTitlePipe, DefaultValuePipe, TimeDifferencePipe, UtcToLocalDatePipe],
})
export class LibrariannplusActivityComponent implements OnInit {
  private readonly auditService = inject(LibrariannPlusAuditService);
  private readonly scrobblingService = inject(ScrobblingService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly PAGE_SIZE = 50;
  protected readonly tabList = [Tabs.All, Tabs.Scrobbles, Tabs.Failed, Tabs.MyChanges, Tabs.ScrobbleHolds];

  stats = signal<LibrariannPlusMyAuditStats | null>(null);
  nextScrobble = signal<string | null>(null);
  entries = signal<LibrariannPlusAuditEntry[]>([]);
  isLoading = signal(true);
  isLoadingMore = signal(false);
  activeTab = signal<Tabs>(Tabs.All);
  scrobblingProviders = signal<UserScrobbleProvider[]>([]);
  currentPage = signal(0);
  pagination = signal<Pagination | null>(null);

  hasMore = computed(() => {
    const p = this.pagination();
    return p != null && p.currentPage < p.totalPages - 1;
  });

  allCount      = computed(() => this.entries().length);
  scrobbleCount = computed(() => this.entries().filter(e => e.category === LibrariannPlusAuditCategory.Scrobble && ![LibrariannPlusEventType.ScrobbleHoldAdded, LibrariannPlusEventType.ScrobbleHoldRemoved].includes(e.eventType)).length);
  failedCount   = computed(() => this.entries().filter(e => e.status === AuditStatus.Failure).length);
  myChangesCount = computed(() => this.entries().filter(e => e.userId != null).length);
  scrobbleHoldsCount = computed(() => this.entries().filter(e => e.category === LibrariannPlusAuditCategory.Scrobble && [LibrariannPlusEventType.ScrobbleHoldAdded, LibrariannPlusEventType.ScrobbleHoldRemoved].includes(e.eventType)).length);

  filteredEntries = computed(() => {
    const tab = this.activeTab();
    const all = this.entries();
    if (tab === Tabs.Scrobbles) return all.filter(e => e.category === LibrariannPlusAuditCategory.Scrobble && ![LibrariannPlusEventType.ScrobbleHoldAdded, LibrariannPlusEventType.ScrobbleHoldRemoved].includes(e.eventType));
    if (tab === Tabs.Failed)    return all.filter(e => e.status === AuditStatus.Failure);
    if (tab === Tabs.MyChanges) return all.filter(e => e.userId != null);
    if (tab === Tabs.ScrobbleHolds) return all.filter(e => e.category === LibrariannPlusAuditCategory.Scrobble && [LibrariannPlusEventType.ScrobbleHoldAdded, LibrariannPlusEventType.ScrobbleHoldRemoved].includes(e.eventType));
    return all;
  });

  ngOnInit() {
    this.loadData();
    this.loadStats();

    this.scrobblingService.getNextScrobble().subscribe(res => {
      this.nextScrobble.set(res)
    });

    this.scrobblingService.getScrobbleProviders().subscribe(tokens => this.scrobblingProviders.set(tokens));
  }

  private loadStats() {
    this.auditService.getMyStats().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: s => this.stats.set(s),
    });
  }

  loadData(reset = true) {
    if (reset) {
      this.currentPage.set(0);
      this.entries.set([]);
      this.isLoading.set(true);
    } else {
      this.isLoadingMore.set(true);
    }
    this.auditService.getMyActivity({}, this.currentPage(), this.PAGE_SIZE)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: result => {
          this.pagination.set(result.pagination);
          if (reset) {
            this.entries.set(result.result ?? []);
            this.isLoading.set(false);
          } else {
            this.entries.update(prev => [...prev, ...(result.result ?? [])]);
            this.isLoadingMore.set(false);
          }
        },
        error: () => {
          this.isLoading.set(false);
          this.isLoadingMore.set(false);
        },
      });
  }


  loadMore() {
    this.currentPage.update(p => p + 1);
    this.loadData(false);
  }

  countFor(tab: Tabs): number {
    switch (tab) {
      case Tabs.Scrobbles: return this.scrobbleCount();
      case Tabs.Failed: return this.failedCount();
      case Tabs.MyChanges: return this.myChangesCount();
      case Tabs.ScrobbleHolds: return this.scrobbleHoldsCount();
      default: return this.allCount();
    }
  }

  retryScrobbleEvent(event: LibrariannPlusAuditEntry) {
    this.scrobblingService.retryScrobbleEvent(event).subscribe((success) => {
      if (!success) return;
      this.loadData();
    });
  }

  protected readonly ScrobbleProvider = ScrobbleProvider;
  protected readonly Tabs = Tabs;
}

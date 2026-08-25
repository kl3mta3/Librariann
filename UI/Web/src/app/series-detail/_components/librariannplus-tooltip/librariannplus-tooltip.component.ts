import {ChangeDetectionStrategy, Component, computed, inject, input, OnInit, signal} from '@angular/core';
import {NavigationExtras, Router} from '@angular/router';
import {NgbActiveOffcanvas} from '@ng-bootstrap/ng-bootstrap';
import {TranslocoDirective} from '@jsverse/transloco';
import {AccountService} from '../../../_services/account.service';
import {LibrariannPlusAuditService} from '../../../_services/librariannplus-audit.service';
import {LibrariannPlusAuditSeriesInfo} from '../../../_models/librariannplus/librariann-plus-audit-series-info';
import {LibrariannPlusAuditCategory} from '../../../_models/librariannplus/librariann-plus-audit-category.enum';
import {LibrariannPlusEventTypePipe} from '../../../_pipes/librariann-plus-event-type.pipe';
import {LibrariannPlusEventDescriptionPipe} from '../../../_pipes/librariann-plus-event-description.pipe';
import {UtcToLocalTimePipe} from "../../../_pipes/utc-to-local-time.pipe";
import {NULL_DATE} from "../../../_pipes/date-year-range.pipe";
import {
  LibrariannPlusAuditEventTypeIconComponent
} from "../../../shared/_components/librariannplus-event-type-icon/librariann-plus-audit-event-type-icon.component";
import {AuditLogErrorPipe} from "../../../_pipes/audit-log-error.pipe";
import {SettingsTabId} from "../../../sidenav/preference-nav/preference-nav.component";
import {
  ScrobbleProviderImageComponent
} from "../../../shared/_components/scrobble-provider-image/scrobble-provider-image.component";
import {ScrobbleProvider} from "../../../_services/scrobbling.service";
import {ScrobbleProviderNamePipe} from "../../../_pipes/scrobble-provider-name.pipe";
import {UtcToLocalDatePipe} from "../../../_pipes/utc-to-locale-date.pipe";
import {TimeDifferencePipe} from "../../../_pipes/time-difference.pipe";
import {
  ScrobbleProviderTagBadgeComponent
} from "../../../shared/_components/scrobble-provider-tag-badge/scrobble-provider-tag-badge.component";
import {
  MetadataProviderTagBadgeComponent
} from "../../../shared/_components/metadata-provider-tag-badge/metadata-provider-tag-badge.component";

@Component({
  selector: 'app-librariannplus-tooltip',
  templateUrl: './librariannplus-tooltip.component.html',
  styleUrls: ['./librariannplus-tooltip.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoDirective, LibrariannPlusEventTypePipe, LibrariannPlusEventDescriptionPipe, UtcToLocalTimePipe,
    LibrariannPlusAuditEventTypeIconComponent, AuditLogErrorPipe, ScrobbleProviderImageComponent, ScrobbleProviderNamePipe,
    UtcToLocalDatePipe, TimeDifferencePipe, ScrobbleProviderTagBadgeComponent, MetadataProviderTagBadgeComponent],
})
export class LibrariannplusTooltipComponent implements OnInit {
  private readonly auditService = inject(LibrariannPlusAuditService);
  private readonly router = inject(Router);
  protected readonly activeOffcanvas = inject(NgbActiveOffcanvas, { optional: true });
  protected readonly isAdmin = inject(AccountService).hasAdminRole;

  seriesId = input.required<number>();

  seriesInfo = signal<LibrariannPlusAuditSeriesInfo | null>(null);
  categoryFilter = signal<LibrariannPlusAuditCategory | null>(null);
  isLoading = signal(true);

  filteredEvents = computed(() => {
    const info = this.seriesInfo();
    if (!info) return [];
    const f = this.categoryFilter();
    return f === null ? info.recentEvents : info.recentEvents.filter(e => e.category === f);
  });

  displayedEvents = computed(() => this.filteredEvents().slice(0, 5));
  totalCount = computed(() => this.seriesInfo()?.recentEvents.length ?? 0);
  metadataCount = computed(() => this.seriesInfo()?.recentEvents.filter(e => e.category === LibrariannPlusAuditCategory.Metadata).length ?? 0);
  scrobbleCount = computed(() => this.seriesInfo()?.recentEvents.filter(e => e.category === LibrariannPlusAuditCategory.Scrobble).length ?? 0);
  matchCount = computed(() => this.seriesInfo()?.recentEvents.filter(e => e.category === LibrariannPlusAuditCategory.Match).length ?? 0);

  ngOnInit() {
    this.auditService.getSeriesInfo(this.seriesId()).subscribe({
      next: info => { this.seriesInfo.set(info); this.isLoading.set(false); },
      error: ()   => this.isLoading.set(false),
    });
  }

  setFilter(cat: LibrariannPlusAuditCategory | null) {
    this.categoryFilter.set(cat);
  }

  navigateAndClose(commands: unknown[], extras?: NavigationExtras) {
    this.activeOffcanvas?.close();
    this.router.navigate(commands, extras);
  }

  categoryColorClass(category: LibrariannPlusAuditCategory): string {
    switch (category) {
      case LibrariannPlusAuditCategory.Match:
        return 'match';
      case LibrariannPlusAuditCategory.Scrobble:
        return 'scrobble';
      case LibrariannPlusAuditCategory.Sync:
        return 'sync';
      default:
        return 'metadata';
    }
  }

  protected readonly AuditCategory = LibrariannPlusAuditCategory;
  protected readonly NULL_DATE = NULL_DATE;
  protected readonly SettingsTabId = SettingsTabId;
  protected readonly ScrobbleProvider = ScrobbleProvider;
}

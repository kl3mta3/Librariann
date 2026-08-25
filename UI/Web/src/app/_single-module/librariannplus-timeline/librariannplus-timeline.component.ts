import {ChangeDetectionStrategy, Component, computed, input, output, TemplateRef} from '@angular/core';
import {DatePipe, NgTemplateOutlet} from '@angular/common';
import {TranslocoDirective} from '@jsverse/transloco';
import {LibrariannPlusAuditEntry} from '../../_models/librariannplus/librariann-plus-audit-entry';
import {LibrariannPlusAuditCategory} from '../../_models/librariannplus/librariann-plus-audit-category.enum';
import {
  LibrariannPlusAuditEventTypeIconComponent
} from "../../shared/_components/librariannplus-event-type-icon/librariann-plus-audit-event-type-icon.component";
import {EmptyStateComponent} from "../../shared/_components/empty-state/empty-state.component";
import {AuditSubjectType} from "../../_models/librariannplus/audit-subject-type.enum";

interface DayGroup {
  key: string;
  label: 'today' | 'yesterday' | 'date';
  dateStr: string;
  count: number;
  events: LibrariannPlusAuditEntry[];
}

function groupByDay(entries: LibrariannPlusAuditEntry[]): DayGroup[] {
  const now = new Date();
  const todayKey = now.toISOString().slice(0, 10);
  const yesterdayKey = new Date(now.getTime() - 86_400_000).toISOString().slice(0, 10);
  const map = new Map<string, LibrariannPlusAuditEntry[]>();

  for (const entry of entries) {
    const key = entry.createdUtc.slice(0, 10);
    if (!map.has(key)) map.set(key, []);
    map.get(key)!.push(entry);
  }

  return Array.from(map.entries())
    .sort(([a], [b]) => b.localeCompare(a))
    .map(([key, evts]) => ({
      key,
      label: key === todayKey ? 'today' : key === yesterdayKey ? 'yesterday' : 'date',
      dateStr: key,
      count: evts.length,
      events: evts,
    }));
}

@Component({
  selector: 'app-librariannplus-timeline',
  templateUrl: './librariannplus-timeline.component.html',
  styleUrls: ['./librariannplus-timeline.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    TranslocoDirective,
    DatePipe,
    LibrariannPlusAuditEventTypeIconComponent,
    NgTemplateOutlet,
    EmptyStateComponent,
  ],
})
export class LibrariannplusTimelineComponent {
  entries = input.required<LibrariannPlusAuditEntry[]>();
  isLoading = input<boolean>(false);
  hasMore = input<boolean>(false);
  isLoadingMore = input<boolean>(false);
  entryTemplate = input<TemplateRef<{$implicit: LibrariannPlusAuditEntry}>>();

  loadMore = output<void>();

  groupedEntries = computed(() => groupByDay(this.entries()));

  categoryColor(category: LibrariannPlusAuditCategory): string {
    switch (category) {
      case LibrariannPlusAuditCategory.Match:
        return 'var(--audit-log-match-color)';
      case LibrariannPlusAuditCategory.Scrobble:
        return 'var(--audit-log-scrobble-color)';
      case LibrariannPlusAuditCategory.Sync:
        return 'var(--audit-log-sync-color)';
      case LibrariannPlusAuditCategory.System:
        return 'var(--audit-log-system-color)';
      default:
        return 'var(--audit-log-metadata-color)';
    }
  }

  categoryBg(category: LibrariannPlusAuditCategory): string {
    return `color-mix(in srgb, ${this.categoryColor(category)} 12%, transparent)`;
  }
}

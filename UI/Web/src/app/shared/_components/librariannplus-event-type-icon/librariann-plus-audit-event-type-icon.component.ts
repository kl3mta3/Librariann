import {ChangeDetectionStrategy, Component, computed, input} from '@angular/core';
import {LibrariannPlusEventType} from '../../../_models/librariannplus/librariann-plus-event-type.enum';
import {LibrariannPlusAuditCategory} from "../../../_models/librariannplus/librariann-plus-audit-category.enum";

function resolveIcon(type: LibrariannPlusEventType): string {
  switch (type) {
    case LibrariannPlusEventType.SeriesMatched:          return 'fas fa-table-list';
    case LibrariannPlusEventType.SeriesMatchFailed:      return 'fas fa-circle-exclamation';
    case LibrariannPlusEventType.SeriesBlacklisted:      return 'fas fa-circle-xmark';
    case LibrariannPlusEventType.SeriesMatchFixed:       return 'fas fa-eraser';
    case LibrariannPlusEventType.SeriesDontMatchSet:     return 'fas fa-table-cells-row-lock';
    case LibrariannPlusEventType.SeriesMetadataProviderOverrideSet: return 'fas fa-right-left';
    case LibrariannPlusEventType.MetadataFetched:        return 'fas fa-magnifying-glass';
    case LibrariannPlusEventType.MetadataUpdated:        return 'fas fa-database';
    case LibrariannPlusEventType.CoverUpdated:           return 'fas fa-image';
    case LibrariannPlusEventType.ChapterMetadataUpdated: return 'fas fa-database';
    case LibrariannPlusEventType.ChapterCoverUpdated:    return 'fas fa-image';
    case LibrariannPlusEventType.VolumeCoverUpdated:     return 'fas fa-image';
    case LibrariannPlusEventType.PersonCoverUpdated:     return 'fas fa-database';
    case LibrariannPlusEventType.PersonAliasAdded:       return 'fas fa-person-circle-plus';
    case LibrariannPlusEventType.CollectionSynced:       return 'fas fa-folder-open';
    case LibrariannPlusEventType.CollectionItemAdded:    return 'fas fa-folder-plus';
    case LibrariannPlusEventType.ScrobbleEventCreated:   return 'fa-regular fa-bookmark';
    case LibrariannPlusEventType.ScrobbleEventUpdated:   return 'fa-solid fa-bookmark';
    case LibrariannPlusEventType.ScrobbleEventSent:      return 'fas fa-paper-plane';
    case LibrariannPlusEventType.ScrobbleEventFailed:    return 'fas fa-circle-exclamation';
    case LibrariannPlusEventType.ScrobbleRateLimitHit:   return 'fas fa-circle-xmark';
    case LibrariannPlusEventType.ScrobbleEventSkipped:   return 'fas fa-circle-xmark';
    case LibrariannPlusEventType.ScrobbleHoldRemoved:    return 'fas fa-eraser';
    case LibrariannPlusEventType.ScrobbleHoldAdded:      return 'fas fa-table-cells-row-lock';
    case LibrariannPlusEventType.SyncStarted:            return 'fas fa-cloud-arrow-up';
    case LibrariannPlusEventType.SyncCompleted:          return 'fas fa-cloud-arrow-down';
    case LibrariannPlusEventType.SyncFailed:             return 'fas fa-cloud-arrow-down';
    case LibrariannPlusEventType.SystemTokenRefresh:     return 'fas fas-recycle'
    case LibrariannPlusEventType.SystemProviderInfoSync: return 'fas fa-sync'
    default:                                         return 'fas fa-circle-exclamation';
  }
}

function resolveColor(type: LibrariannPlusEventType): string {
  switch (type) {
    case LibrariannPlusEventType.SeriesMatchFailed:
    case LibrariannPlusEventType.ScrobbleEventFailed:
    case LibrariannPlusEventType.SyncFailed:
      return 'var(--error-color)';
    case LibrariannPlusEventType.SeriesBlacklisted:
    case LibrariannPlusEventType.ScrobbleRateLimitHit:
    case LibrariannPlusEventType.ScrobbleEventSkipped:
      return 'var(--warning-color)';
    default:
      return '';
  }
}

function resolveCategory(type: LibrariannPlusAuditCategory): string {
  switch (type) {
    case LibrariannPlusAuditCategory.Match:
      return 'var(--audit-log-match-color)';
    case LibrariannPlusAuditCategory.Metadata:
      return 'var(--audit-log-metadata-color)';
    case LibrariannPlusAuditCategory.Scrobble:
      return 'var(--audit-log-scrobble-color)';
    case LibrariannPlusAuditCategory.Sync:
      return 'var(--audit-log-sync-color)';
    case LibrariannPlusAuditCategory.System:
      return 'var(--audit-log-system-color)';
  }
}

@Component({
  selector: 'app-librariannplus-audit-event-type-icon',
  templateUrl: './librariann-plus-audit-event-type-icon.component.html',
  styleUrl: './librariann-plus-audit-event-type-icon.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LibrariannPlusAuditEventTypeIconComponent {
  type = input.required<LibrariannPlusEventType>();
  /** Category will override colors when there is not an explicit color designation (error/warning) */
  category = input.required<LibrariannPlusAuditCategory>();

  protected readonly iconClass = computed(() => resolveIcon(this.type()));
  protected readonly iconColor = computed(() => {

    const color = resolveColor(this.type());
    const categoryColor = resolveCategory(this.category());

    if (color === '') return categoryColor;

    return color;
  });
}

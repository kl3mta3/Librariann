import {inject, Pipe, PipeTransform} from '@angular/core';
import {LibrariannPlusEventType} from '../_models/librariannplus/librariann-plus-event-type.enum';
import {TranslocoService} from '@jsverse/transloco';

@Pipe({
  name: 'librariannPlusEventType',
  standalone: true
})
export class LibrariannPlusEventTypePipe implements PipeTransform {
  private readonly translocoService = inject(TranslocoService);

  transform(value: LibrariannPlusEventType): string {
    switch (value) {
      case LibrariannPlusEventType.SeriesMatched:
        return this.translocoService.translate('librariann-plus-event-type-pipe.series-matched');
      case LibrariannPlusEventType.SeriesMatchFailed:
        return this.translocoService.translate('librariann-plus-event-type-pipe.series-match-failed');
      case LibrariannPlusEventType.SeriesBlacklisted:
        return this.translocoService.translate('librariann-plus-event-type-pipe.series-blacklisted');
      case LibrariannPlusEventType.SeriesMatchFixed:
        return this.translocoService.translate('librariann-plus-event-type-pipe.series-match-fixed');
      case LibrariannPlusEventType.SeriesDontMatchSet:
        return this.translocoService.translate('librariann-plus-event-type-pipe.series-dont-match-set');
      case LibrariannPlusEventType.SeriesMetadataProviderOverrideSet:
        return this.translocoService.translate('librariann-plus-event-type-pipe.series-metadata-provider-override-set');
      case LibrariannPlusEventType.MetadataFetched:
        return this.translocoService.translate('librariann-plus-event-type-pipe.metadata-fetched');
      case LibrariannPlusEventType.MetadataUpdated:
        return this.translocoService.translate('librariann-plus-event-type-pipe.metadata-updated');
      case LibrariannPlusEventType.CoverUpdated:
        return this.translocoService.translate('librariann-plus-event-type-pipe.cover-updated');
      case LibrariannPlusEventType.ChapterMetadataUpdated:
        return this.translocoService.translate('librariann-plus-event-type-pipe.chapter-metadata-updated');
      case LibrariannPlusEventType.ChapterCoverUpdated:
        return this.translocoService.translate('librariann-plus-event-type-pipe.chapter-cover-updated');
      case LibrariannPlusEventType.VolumeCoverUpdated:
        return this.translocoService.translate('librariann-plus-event-type-pipe.volume-cover-updated');
      case LibrariannPlusEventType.PersonCoverUpdated:
        return this.translocoService.translate('librariann-plus-event-type-pipe.person-cover-updated');
      case LibrariannPlusEventType.PersonAliasAdded:
        return this.translocoService.translate('librariann-plus-event-type-pipe.person-alias-added');
      case LibrariannPlusEventType.CollectionSynced:
        return this.translocoService.translate('librariann-plus-event-type-pipe.collection-synced');
      case LibrariannPlusEventType.CollectionItemAdded:
        return this.translocoService.translate('librariann-plus-event-type-pipe.collection-item-added');
      case LibrariannPlusEventType.ScrobbleEventCreated:
        return this.translocoService.translate('librariann-plus-event-type-pipe.scrobble-created');
      case LibrariannPlusEventType.ScrobbleEventUpdated:
        return this.translocoService.translate('librariann-plus-event-type-pipe.scrobble-updated');
      case LibrariannPlusEventType.ScrobbleEventSent:
        return this.translocoService.translate('librariann-plus-event-type-pipe.scrobble-sent');
      case LibrariannPlusEventType.ScrobbleEventFailed:
        return this.translocoService.translate('librariann-plus-event-type-pipe.scrobble-failed');
      case LibrariannPlusEventType.ScrobbleRateLimitHit:
        return this.translocoService.translate('librariann-plus-event-type-pipe.scrobble-rate-limit');
      case LibrariannPlusEventType.ScrobbleEventSkipped:
        return this.translocoService.translate('librariann-plus-event-type-pipe.scrobble-skipped');
      case LibrariannPlusEventType.ScrobbleHoldAdded:
        return this.translocoService.translate('librariann-plus-event-type-pipe.scrobble-hold-added');
      case LibrariannPlusEventType.ScrobbleHoldRemoved:
        return this.translocoService.translate('librariann-plus-event-type-pipe.scrobble-hold-removed');
      case LibrariannPlusEventType.SyncStarted:
        return this.translocoService.translate('librariann-plus-event-type-pipe.sync-started');
      case LibrariannPlusEventType.SyncCompleted:
        return this.translocoService.translate('librariann-plus-event-type-pipe.sync-completed');
      case LibrariannPlusEventType.SyncFailed:
        return this.translocoService.translate('librariann-plus-event-type-pipe.sync-failed');
      case LibrariannPlusEventType.SystemProviderInfoSync:
        return this.translocoService.translate('librariann-plus-event-type-pipe.system-provider-info-sync');
      case LibrariannPlusEventType.SystemTokenRefresh:
        return this.translocoService.translate('librariann-plus-event-type-pipe.system-token-refresh');
    }
  }
}

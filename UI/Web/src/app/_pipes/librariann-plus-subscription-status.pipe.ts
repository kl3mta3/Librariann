import {inject, Pipe, PipeTransform} from '@angular/core';
import {TranslocoService} from '@jsverse/transloco';
import {LibrariannPlusSubscriptionState} from '../_models/librariannplus/license-info';

@Pipe({
  name: 'librariannPlusSubscriptionStatus',
  standalone: true,
  pure: true
})
export class LibrariannPlusSubscriptionStatusPipe implements PipeTransform {
  private readonly translocoService = inject(TranslocoService);

  transform(state: LibrariannPlusSubscriptionState | null | undefined): string {
    switch (state) {
      case LibrariannPlusSubscriptionState.Active:
        return this.translocoService.translate('librariann-plus-subscription-status-pipe.active');
      case LibrariannPlusSubscriptionState.Cancelling:
        return this.translocoService.translate('librariann-plus-subscription-status-pipe.cancelling');
      case LibrariannPlusSubscriptionState.Paused:
        return this.translocoService.translate('librariann-plus-subscription-status-pipe.paused');
      case LibrariannPlusSubscriptionState.Expired:
        return this.translocoService.translate('librariann-plus-subscription-status-pipe.expired');
      default:
        return '';
    }

  }
}

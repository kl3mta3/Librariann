import {inject, Pipe, PipeTransform} from '@angular/core';
import {TranslocoService} from '@jsverse/transloco';
import {LibrariannPlusBillingInterval} from '../_models/librariannplus/license-info';

@Pipe({
  name: 'librariannPlusBillingInterval',
  standalone: true,
  pure: true
})
export class LibrariannPlusBillingIntervalPipe implements PipeTransform {
  private readonly translocoService = inject(TranslocoService);

  transform(interval: LibrariannPlusBillingInterval | null | undefined, mode: 'adjective' | 'unit' = 'adjective'): string {
    const suffix = mode === 'unit' ? '-unit-label' : '-label';
    switch (interval) {
      case LibrariannPlusBillingInterval.Day:  return this.translocoService.translate('librariann-plus-billing-interval-pipe.day' + suffix);
      case LibrariannPlusBillingInterval.Week: return this.translocoService.translate('librariann-plus-billing-interval-pipe.week' + suffix);
      case LibrariannPlusBillingInterval.Year: return this.translocoService.translate('librariann-plus-billing-interval-pipe.year' + suffix);
      default:                             return this.translocoService.translate('librariann-plus-billing-interval-pipe.month' + suffix);
    }
  }
}

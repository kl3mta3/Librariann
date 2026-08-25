import {inject, Pipe, PipeTransform} from '@angular/core';
import {TranslocoService} from '@jsverse/transloco';
import {LibrariannPlusProviderHealthStatus} from '../_models/librariannplus/librariann-plus-provider-health';

@Pipe({
  name: 'librariannPlusProviderHealthStatus',
  standalone: true,
  pure: true,
})
export class LibrariannPlusProviderHealthStatusPipe implements PipeTransform {
  private readonly translocoService = inject(TranslocoService);

  transform(status: LibrariannPlusProviderHealthStatus | null | undefined): string {
    switch (status) {
      case LibrariannPlusProviderHealthStatus.Operational: return this.translocoService.translate('librariann-plus-provider-health-status-pipe.operational-label');
      case LibrariannPlusProviderHealthStatus.Degraded:    return this.translocoService.translate('librariann-plus-provider-health-status-pipe.degraded-label');
      case LibrariannPlusProviderHealthStatus.Down:        return this.translocoService.translate('librariann-plus-provider-health-status-pipe.down-label');
      default:                                         return this.translocoService.translate('librariann-plus-provider-health-status-pipe.unknown-label');
    }
  }
}

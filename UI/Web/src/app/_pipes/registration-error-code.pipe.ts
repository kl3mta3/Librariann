import {inject, Pipe, PipeTransform} from '@angular/core';
import {TranslocoService} from '@jsverse/transloco';
import {LibrariannPlusRegistrationErrorCode} from '../_models/librariannplus/registration/librariann-plus-registration-error-code';

@Pipe({
  name: 'librariannPlusRegistrationErrorCode',
  standalone: true,
  pure: true
})
export class LibrariannPlusRegistrationErrorCodePipe implements PipeTransform {

  private readonly translocoService = inject(TranslocoService);

  transform(code: LibrariannPlusRegistrationErrorCode | null | undefined): string {
    if (code == null) return '';

    switch (code) {
      case LibrariannPlusRegistrationErrorCode.RegistrationFailed:
        return this.translocoService.translate('librariann-plus-registration-error-code-pipe.registration-failed');
      case LibrariannPlusRegistrationErrorCode.AlreadyRegistered:
        return this.translocoService.translate('librariann-plus-registration-error-code-pipe.already-registered');
      case LibrariannPlusRegistrationErrorCode.SubscriptionInactive:
        return this.translocoService.translate('librariann-plus-registration-error-code-pipe.subscription-inactive');
      case LibrariannPlusRegistrationErrorCode.InternalError:
        return this.translocoService.translate('librariann-plus-registration-error-code-pipe.internal-error');
      default:
        return this.translocoService.translate('librariann-plus-registration-error-code-pipe.internal-error');
    }
  }
}

import {inject} from '@angular/core';
import {CanActivateFn, Router} from '@angular/router';
import {AccountService} from '../_services/account.service';

export const manageMetadataGuard: CanActivateFn = () => {
  const accountService = inject(AccountService);
  return accountService.canManageMetadata() ? true : inject(Router).createUrlTree(['/home']);
};

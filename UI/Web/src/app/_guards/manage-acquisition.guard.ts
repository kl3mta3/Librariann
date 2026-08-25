import {inject} from '@angular/core';
import {CanActivateFn, Router} from '@angular/router';
import {AccountService} from '../_services/account.service';

export const manageAcquisitionGuard: CanActivateFn = () => {
  const accountService = inject(AccountService);
  return accountService.canManageAcquisition() ? true : inject(Router).createUrlTree(['/home']);
};

import {inject} from '@angular/core';
import {CanActivateFn, Router} from '@angular/router';
import {AccountService} from '../_services/account.service';

export const indexerSearchGuard: CanActivateFn = () => {
  const accountService = inject(AccountService);
  return accountService.canSearchIndexers() ? true : inject(Router).createUrlTree(['/home']);
};


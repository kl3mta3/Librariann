import {ChangeDetectionStrategy, Component, inject, OnInit, signal} from '@angular/core';
import {ReactiveFormsModule} from "@angular/forms";
import {AccountService} from "../../../_services/account.service";
import {LicenseService} from "../../../_services/license.service";
import {LicenseDashboardComponent} from "../license-dashboard/license-dashboard.component";
import {LibrariannPlusUpsellComponent} from "../librariann-plus-upsell/librariann-plus-upsell.component";
import {
  LibrariannPlusConnectProvidersComponent
} from "../librariann-plus-connect-providers/librariann-plus-connect-providers.component";
import {LoadingComponent} from "../../../shared/loading/loading.component";

export enum LibrariannPlusRegistrationStep {
  Upsell = 0,
  ConnectProviders = 1
}

@Component({
    selector: 'app-license',
    templateUrl: './license.component.html',
    styleUrls: ['./license.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, LicenseDashboardComponent, LibrariannPlusUpsellComponent, LibrariannPlusConnectProvidersComponent, LoadingComponent]
})
export class LicenseComponent implements OnInit {

  protected readonly accountService = inject(AccountService);
  protected readonly licenseService = inject(LicenseService);

  activeStep = signal<LibrariannPlusRegistrationStep>(LibrariannPlusRegistrationStep.Upsell);

  isChecking = signal<boolean>(true);

  protected readonly LibrariannPlusRegistrationStep = LibrariannPlusRegistrationStep;


  ngOnInit(): void {
    this.loadLicenseInfo();
  }

  loadLicenseInfo(forceCheck = false) {
    this.isChecking.set(true);
    this.licenseService.getLicenseInfo(forceCheck).subscribe({
      next: () => this.isChecking.set(false),
      error: () => this.isChecking.set(false),
    });
  }
}

import {ChangeDetectionStrategy, Component, output} from '@angular/core';
import {TranslocoDirective} from '@jsverse/transloco';
import {ScrobbleProvider} from '../../../_services/scrobbling.service';
import {
  ScrobbleProviderImageComponent
} from '../../../shared/_components/scrobble-provider-image/scrobble-provider-image.component';
import {ScrobbleProviderNamePipe} from '../../../_pipes/scrobble-provider-name.pipe';
import {environment} from "../../../../environments/environment";
import {RegisterLicenseKeyComponent} from "../register-license-key/register-license-key.component";
import {LibrariannPlusRegistrationStep} from "../license/license.component";

@Component({
  selector: 'app-librariann-plus-upsell',
  imports: [
    TranslocoDirective,
    ScrobbleProviderImageComponent,
    ScrobbleProviderNamePipe,
    RegisterLicenseKeyComponent,
  ],
  templateUrl: './librariann-plus-upsell.component.html',
  styleUrl: './librariann-plus-upsell.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LibrariannPlusUpsellComponent {

  stepChanged = output<LibrariannPlusRegistrationStep>();

  handleSaved(isSubActive: boolean) {
    // TODO: Prompt the user to inform them then move to Cancelled state
    if (!isSubActive) return;

    // TODO: Move to Connect Provider page
    this.stepChanged.emit(LibrariannPlusRegistrationStep.ConnectProviders);
  }



  protected readonly ScrobbleProvider = ScrobbleProvider;
  protected readonly providers = [
    ScrobbleProvider.AniList,
    ScrobbleProvider.Mal,
    ScrobbleProvider.Hardcover,
    ScrobbleProvider.MangaBaka,
  ];
  protected readonly environment = environment;
}

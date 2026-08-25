import {ChangeDetectionStrategy, Component, inject, output, signal} from '@angular/core';
import {translate, TranslocoDirective} from "@jsverse/transloco";
import {
  ScrobbleAccountCardComponent
} from "../../../user-settings/scrobble-account-card/scrobble-account-card.component";
import {ScrobblingService} from "../../../_services/scrobbling.service";
import {BannerComponent} from "../../../shared/_components/banner/banner.component";
import {ToastrService} from "ngx-toastr";
import {UserScrobbleProvider} from "../../../_models/librariannplus/scrobble-providers/user-scrobble-provider";
import {tap} from "rxjs";

@Component({
  selector: 'app-librariann-plus-connect-providers',
  imports: [
    TranslocoDirective,
    ScrobbleAccountCardComponent,
    BannerComponent
  ],
  templateUrl: './librariann-plus-connect-providers.component.html',
  styleUrl: './librariann-plus-connect-providers.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LibrariannPlusConnectProvidersComponent {

  private readonly scrobblingService = inject(ScrobblingService);
  private readonly toastr = inject(ToastrService);

  done = output();

  scrobblingProviders = signal<UserScrobbleProvider[]>([]);

  constructor() {
    this.scrobblingService.getScrobbleProviders().subscribe(tokens => this.scrobblingProviders.set(tokens));
  }

  backfillAndRedirect() {
    this.scrobblingService.triggerScrobbleEventGenerationForAllValid().pipe(
      tap(hasRan => {
        if (hasRan) {
          this.toastr.info(translate('toasts.scrobble-gen-init'));
        }

        this.done.emit()
      }),
    ).subscribe();
  }

  skip() {
    this.done.emit();
  }
}

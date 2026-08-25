import {ChangeDetectionStrategy, Component, inject, OnInit, signal} from '@angular/core';
import {DOCUMENT} from '@angular/common';
import {translate, TranslocoDirective} from "@jsverse/transloco";
import {Clipboard} from "@angular/cdk/clipboard";
import {ToastrService} from "ngx-toastr";
import {SettingsService} from "../settings.service";

@Component({
  selector: 'app-manage-plex-patch',
  imports: [TranslocoDirective],
  templateUrl: './manage-plex-patch.component.html',
  styleUrl: './manage-plex-patch.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ManagePlexPatchComponent implements OnInit {

  private readonly document = inject(DOCUMENT);
  private readonly clipboard = inject(Clipboard);
  private readonly toastr = inject(ToastrService);
  private readonly settingsService = inject(SettingsService);

  protected readonly releasesUrl = 'https://github.com/kl3mta3/Librariann-Plex-Patcher/releases/latest';

  /**
   * The address this browser is currently reaching Librariann on. Uses baseURI rather than location.origin so a
   * configured BaseUrl is included - the patcher appends /embed to whatever it's given.
   */
  serverAddress = signal<string>('');
  /** Configured Kestrel port, shown for when the browser's address isn't reachable from the Plex machine. */
  port = signal<number | null>(null);
  /** Optional configured hostname, if the admin set one. */
  hostName = signal<string>('');

  /** Sample appsettings.json snippet for the common all-on-one-machine case. */
  protected readonly embeddingExample = `"EmbeddingOrigins": [
    "http://127.0.0.1:32400"
],`;

  ngOnInit(): void {
    this.serverAddress.set(this.document.baseURI.replace(/\/+$/, ''));

    this.settingsService.getServerSettings().subscribe(settings => {
      this.port.set(settings.port);
      this.hostName.set(settings.hostName || '');
    });
  }

  copy(value: string) {
    this.clipboard.copy(value);
    this.toastr.success(translate('toasts.copied-to-clipboard'));
  }
}

import {ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, inject, OnInit} from '@angular/core';
import {FormControl, FormGroup, ReactiveFormsModule} from '@angular/forms';
import {ToastrService} from 'ngx-toastr';
import {catchError, debounceTime, distinctUntilChanged, filter, interval, of, Subscription, switchMap, tap} from 'rxjs';
import {KokoroInstallStatus, KokoroLatestRelease, KokoroProcessStatus, KokoroStatus, SettingsService} from '../settings.service';
import {ServerSettings} from '../_models/server-settings';
import {DirectoryPickerComponent, DirectoryPickerResult} from '../_modals/directory-picker/directory-picker.component';
import {translate, TranslocoDirective} from "@jsverse/transloco";
import {SettingItemComponent} from "../../settings/_components/setting-item/setting-item.component";
import {takeUntilDestroyed} from "@angular/core/rxjs-interop";
import {ModalService} from "../../_services/modal.service";

/**
 * Kokoro/TTS settings, split out of manage-media-settings.component.ts into its own admin tab - these settings
 * govern the book reader's text-to-speech provider, not media storage/encoding, so they don't belong alongside
 * cover/PDF/bookmark settings.
 */
@Component({
  selector: 'app-manage-tts-settings',
  templateUrl: './manage-tts-settings.component.html',
  styleUrls: ['./manage-tts-settings.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, TranslocoDirective, SettingItemComponent]
})
export class ManageTtsSettingsComponent implements OnInit {

  private readonly cdRef = inject(ChangeDetectorRef);
  private readonly settingsService = inject(SettingsService);
  private readonly toastr = inject(ToastrService);
  private readonly modalService = inject(ModalService);
  private readonly destroyRef = inject(DestroyRef);

  serverSettings!: ServerSettings;
  settingsForm: FormGroup = new FormGroup({});

  protected kokoroStatus: KokoroStatus | null = null;
  protected kokoroStatusChecking = false;
  protected kokoroLatestRelease: KokoroLatestRelease | null = null;
  protected kokoroReleaseChecking = false;
  protected kokoroProcessStatus: KokoroProcessStatus | null = null;
  protected kokoroProcessBusy = false;
  protected kokoroInstallStatus: KokoroInstallStatus | null = null;
  protected kokoroInstalling = false;
  private kokoroInstallPollSub: Subscription | null = null;


  ngOnInit(): void {
    this.settingsService.getServerSettings().subscribe((settings: ServerSettings) => {
      this.serverSettings = settings;
      this.settingsForm.addControl('kokoroEndpointUrl', new FormControl(this.serverSettings.kokoroEndpointUrl || ''));
      this.settingsForm.addControl('kokoroExecutablePath', new FormControl(this.serverSettings.kokoroExecutablePath || ''));
      this.settingsForm.addControl('kokoroUseGpu', new FormControl(this.serverSettings.kokoroUseGpu ?? false));
      this.settingsForm.addControl('kokoroSyncFfmpegPath', new FormControl(this.serverSettings.kokoroSyncFfmpegPath ?? true));

      this.refreshKokoroProcessStatus();
      this.checkKokoroInstallStatusOnLoad();

      // Automatically save settings as we edit them
      this.settingsForm.valueChanges.pipe(
        distinctUntilChanged(),
        debounceTime(100),
        filter(_ => this.settingsForm.valid),
        takeUntilDestroyed(this.destroyRef),
        switchMap(_ => {
          const data = this.packData();
          return this.settingsService.updateServerSettings(data).pipe(catchError(err => {
            console.error(err);
            return of(null);
          }));
        }),
        tap(settings => {
          if (!settings) {
            return;
          }

          if (settings.hasOwnProperty('result') && settings.hasOwnProperty('value')) {
            this.serverSettings = (settings as any).value;
          } else {
            this.serverSettings = settings;
          }

          this.resetForm();
          this.cdRef.markForCheck();
        })
      ).subscribe();

      this.cdRef.markForCheck();
    });
  }

  resetForm() {
    this.settingsForm.get('kokoroEndpointUrl')?.setValue(this.serverSettings.kokoroEndpointUrl, {onlySelf: true, emitEvent: false});
    this.settingsForm.get('kokoroExecutablePath')?.setValue(this.serverSettings.kokoroExecutablePath, {onlySelf: true, emitEvent: false});
    this.settingsForm.get('kokoroUseGpu')?.setValue(this.serverSettings.kokoroUseGpu, {onlySelf: true, emitEvent: false});
    this.settingsForm.get('kokoroSyncFfmpegPath')?.setValue(this.serverSettings.kokoroSyncFfmpegPath, {onlySelf: true, emitEvent: false});
    this.settingsForm.markAsPristine();
    this.cdRef.markForCheck();
  }

  packData() {
    const modelSettings = Object.assign({}, this.serverSettings);
    modelSettings.kokoroEndpointUrl = this.settingsForm.get('kokoroEndpointUrl')?.value ?? '';
    modelSettings.kokoroExecutablePath = this.settingsForm.get('kokoroExecutablePath')?.value ?? '';
    modelSettings.kokoroUseGpu = this.settingsForm.get('kokoroUseGpu')?.value ?? false;
    modelSettings.kokoroSyncFfmpegPath = this.settingsForm.get('kokoroSyncFfmpegPath')?.value ?? true;

    return modelSettings;
  }

  checkKokoroStatus(): void {
    this.kokoroStatusChecking = true;
    this.cdRef.markForCheck();
    this.settingsService.testKokoroConnection().subscribe({
      next: (res) => {
        this.kokoroStatus = res;
        this.kokoroStatusChecking = false;
        this.cdRef.markForCheck();
      },
      error: () => {
        // The endpoint itself doesn't error (it reports IsReachable: false) - this only fires on something
        // more fundamental (network/auth), so surface it the same way rather than claiming a real status.
        this.kokoroStatus = {isConfigured: true, isReachable: false};
        this.kokoroStatusChecking = false;
        this.cdRef.markForCheck();
      },
    });
  }

  checkKokoroLatestRelease(): void {
    this.kokoroReleaseChecking = true;
    this.cdRef.markForCheck();
    this.settingsService.getKokoroLatestRelease().subscribe({
      next: (res) => {
        this.kokoroLatestRelease = res;
        this.kokoroReleaseChecking = false;
        this.cdRef.markForCheck();
      },
      error: () => {
        this.kokoroLatestRelease = {success: false};
        this.kokoroReleaseChecking = false;
        this.cdRef.markForCheck();
      },
    });
  }

  refreshKokoroProcessStatus(): void {
    this.settingsService.getKokoroProcessStatus().subscribe({
      next: (res) => {
        this.kokoroProcessStatus = res;
        this.cdRef.markForCheck();
      },
      error: () => {
        this.kokoroProcessStatus = null;
        this.cdRef.markForCheck();
      },
    });
  }

  /** Picks up an install that was already running before a page refresh, rather than losing its progress. */
  private checkKokoroInstallStatusOnLoad(): void {
    this.settingsService.getKokoroInstallStatus().subscribe({
      next: (status) => {
        this.kokoroInstallStatus = status;
        if (status.inProgress) {
          this.kokoroInstalling = true;
          this.pollKokoroInstallStatus();
        }
        this.cdRef.markForCheck();
      },
      error: () => {},
    });
  }

  startKokoroInstall(): void {
    this.kokoroInstalling = true;
    this.kokoroInstallStatus = null;
    this.cdRef.markForCheck();
    this.settingsService.startKokoroInstall().subscribe({
      next: (status) => {
        this.kokoroInstallStatus = status;
        this.cdRef.markForCheck();
        this.pollKokoroInstallStatus();
      },
      error: () => {
        this.kokoroInstalling = false;
        this.cdRef.markForCheck();
      },
    });
  }

  /** e.g. "159 MB / 353 MB (45%)", or just "159 MB" if the server hasn't reported a total yet. */
  kokoroInstallProgressLabel(): string {
    const status = this.kokoroInstallStatus;
    if (!status) return '';

    const downloaded = ManageTtsSettingsComponent.formatBytes(status.bytesDownloaded);
    if (!status.totalBytes) return downloaded;

    const total = ManageTtsSettingsComponent.formatBytes(status.totalBytes);
    const percent = Math.round((status.bytesDownloaded / status.totalBytes) * 100);
    return `${downloaded} / ${total} (${percent}%)`;
  }

  private static formatBytes(bytes: number): string {
    if (!bytes) return '0 MB';
    const mb = bytes / (1024 * 1024);
    return `${mb.toFixed(mb >= 100 ? 0 : 1)} MB`;
  }

  private pollKokoroInstallStatus(): void {
    this.kokoroInstallPollSub?.unsubscribe();
    this.kokoroInstallPollSub = interval(1000).pipe(
      switchMap(() => this.settingsService.getKokoroInstallStatus()),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe(status => {
      this.kokoroInstallStatus = status;
      if (!status.inProgress) {
        this.kokoroInstalling = false;
        this.kokoroInstallPollSub?.unsubscribe();

        if (status.success) {
          this.toastr.success(translate('manage-tts-settings.kokoro-install-success'));
          // The install just set kokoroExecutablePath server-side - refresh the form so the field shows it,
          // and re-check process status now that there's actually something to start.
          this.settingsService.getServerSettings().subscribe(settings => {
            this.serverSettings = settings;
            this.resetForm();
          });
          this.refreshKokoroProcessStatus();
        } else if (status.error) {
          this.toastr.error(status.error);
        }
      }
      this.cdRef.markForCheck();
    });
  }

  startKokoroProcess(): void {
    this.kokoroProcessBusy = true;
    this.cdRef.markForCheck();
    this.settingsService.startKokoroProcess().subscribe({
      next: (res) => {
        this.kokoroProcessStatus = res;
        this.kokoroProcessBusy = false;
        if (res.error) this.toastr.error(res.error);
        this.cdRef.markForCheck();
      },
      error: () => {
        this.kokoroProcessBusy = false;
        this.cdRef.markForCheck();
      },
    });
  }

  stopKokoroProcess(): void {
    this.kokoroProcessBusy = true;
    this.cdRef.markForCheck();
    this.settingsService.stopKokoroProcess().subscribe({
      next: (res) => {
        this.kokoroProcessStatus = res;
        this.kokoroProcessBusy = false;
        this.cdRef.markForCheck();
      },
      error: () => {
        this.kokoroProcessBusy = false;
        this.cdRef.markForCheck();
      },
    });
  }

  openDirectoryChooser(existingDirectory: string, formControl: string) {
    const modalRef = this.modalService.open(DirectoryPickerComponent);
    modalRef.setInput('startingFolder', existingDirectory || '');
    modalRef.setInput('helpUrl', '');
    modalRef.closed.subscribe((closeResult: DirectoryPickerResult) => {
      if (closeResult.success && closeResult.folderPath !== '') {
        this.settingsForm.get(formControl)?.setValue(closeResult.folderPath);
        this.settingsForm.markAsDirty();
        this.cdRef.markForCheck();
      }
    });
  }
}

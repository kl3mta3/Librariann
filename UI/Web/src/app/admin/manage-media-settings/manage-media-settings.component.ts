import {ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, inject, OnInit} from '@angular/core';
import {FormControl, FormGroup, ReactiveFormsModule, Validators} from '@angular/forms';
import {ToastrService} from 'ngx-toastr';
import {catchError, debounceTime, distinctUntilChanged, filter, interval, of, Subscription, switchMap, tap} from 'rxjs';
import {FfmpegInstallStatus, SettingsService} from '../settings.service';
import {ServerSettings} from '../_models/server-settings';
import {DirectoryPickerComponent, DirectoryPickerResult} from '../_modals/directory-picker/directory-picker.component';
import {allEncodeFormats} from '../_models/encode-format';
import {translate, TranslocoDirective, TranslocoService} from "@jsverse/transloco";
import {allCoverImageSizes, CoverImageSize} from '../_models/cover-image-size';
import {allPdfRenderResolutions, PdfRenderResolution} from '../_models/pdf-render-resolution';
import {SettingItemComponent} from "../../settings/_components/setting-item/setting-item.component";
import {EncodeFormatPipe} from "../../_pipes/encode-format.pipe";
import {CoverImageSizePipe} from "../../_pipes/cover-image-size.pipe";
import {PdfRenderResolutionPipe} from "../../_pipes/pdf-render-resolution.pipe"
import {ConfirmService} from "../../shared/confirm.service";
import {takeUntilDestroyed} from "@angular/core/rxjs-interop";
import {ModalService} from "../../_services/modal.service";

@Component({
  selector: 'app-manage-media-settings',
  templateUrl: './manage-media-settings.component.html',
  styleUrls: ['./manage-media-settings.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, TranslocoDirective, SettingItemComponent, EncodeFormatPipe, CoverImageSizePipe, PdfRenderResolutionPipe]
})
export class ManageMediaSettingsComponent implements OnInit {

  private readonly translocoService = inject(TranslocoService);
  private readonly cdRef = inject(ChangeDetectorRef);
  private readonly confirmService = inject(ConfirmService);
  private readonly settingsService = inject(SettingsService);
  private readonly toastr = inject(ToastrService);
  private readonly modalService = inject(ModalService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly allEncodeFormats = allEncodeFormats;
  protected readonly allCoverImageSizes = allCoverImageSizes;
  protected readonly allPdfRenderResolutions = allPdfRenderResolutions;

  serverSettings!: ServerSettings;
  settingsForm: FormGroup = new FormGroup({});

  protected ffmpegInstallStatus: FfmpegInstallStatus | null = null;
  protected ffmpegInstalling = false;
  private ffmpegInstallPollSub: Subscription | null = null;

  ngOnInit(): void {
    this.settingsService.getServerSettings().subscribe((settings: ServerSettings) => {
      this.serverSettings = settings;
      this.settingsForm.addControl('encodeMediaAs', new FormControl(this.serverSettings.encodeMediaAs, [Validators.required]));
      this.settingsForm.addControl('bookmarksDirectory', new FormControl(this.serverSettings.bookmarksDirectory, [Validators.required]));
      this.settingsForm.addControl('coverImageSize', new FormControl(this.serverSettings.coverImageSize || CoverImageSize.Default, [Validators.required]));
      this.settingsForm.addControl('pdfRenderResolution', new FormControl(this.serverSettings.pdfRenderResolution || PdfRenderResolution.Default, [Validators.required]));
      this.settingsForm.addControl('ffmpegPath', new FormControl(this.serverSettings.ffmpegPath || 'ffmpeg', [Validators.required]));
      this.settingsForm.addControl('metadataProviderContactEmail', new FormControl(this.serverSettings.metadataProviderContactEmail || '', [Validators.email]));

      this.checkFfmpegInstallStatusOnLoad();

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

          const encodingChanged = this.serverSettings.encodeMediaAs !== settings.encodeMediaAs;
          if (encodingChanged) {
            this.toastr.info(translate('manage-media-settings.media-warning'));
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
    this.settingsForm.get('encodeMediaAs')?.setValue(this.serverSettings.encodeMediaAs, {onlySelf: true, emitEvent: false});
    this.settingsForm.get('bookmarksDirectory')?.setValue(this.serverSettings.bookmarksDirectory, {onlySelf: true, emitEvent: false});
    this.settingsForm.get('coverImageSize')?.setValue(this.serverSettings.coverImageSize, {onlySelf: true, emitEvent: false});
    this.settingsForm.get('pdfRenderResolution')?.setValue(this.serverSettings.pdfRenderResolution, {onlySelf: true, emitEvent: false});
    this.settingsForm.get('ffmpegPath')?.setValue(this.serverSettings.ffmpegPath, {onlySelf: true, emitEvent: false});
    this.settingsForm.get('metadataProviderContactEmail')?.setValue(this.serverSettings.metadataProviderContactEmail, {onlySelf: true, emitEvent: false});
    this.settingsForm.markAsPristine();
    this.cdRef.markForCheck();
  }

  packData() {
    const modelSettings = Object.assign({}, this.serverSettings);
    modelSettings.encodeMediaAs = parseInt(this.settingsForm.get('encodeMediaAs')?.value, 10);
    modelSettings.bookmarksDirectory = this.settingsForm.get('bookmarksDirectory')?.value;
    modelSettings.coverImageSize = parseInt(this.settingsForm.get('coverImageSize')?.value, 10);
    modelSettings.pdfRenderResolution = parseInt(this.settingsForm.get('pdfRenderResolution')?.value, 10);
    modelSettings.ffmpegPath = this.settingsForm.get('ffmpegPath')?.value;
    modelSettings.metadataProviderContactEmail = this.settingsForm.get('metadataProviderContactEmail')?.value ?? '';

    return modelSettings;
  }


  async resetToDefaults() {
    if (!await this.confirmService.confirm(translate('toasts.confirm-reset-server-settings'))) return;

    this.settingsService.resetServerSettings().subscribe((settings: ServerSettings) => {
      this.serverSettings = settings;
      this.resetForm();
      this.toastr.success(this.translocoService.translate('toasts.server-settings-updated'));
    }, (err: any) => {
      console.error('error: ', err);
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

  /** Picks up an install that was already running before a page refresh, rather than losing its progress. */
  private checkFfmpegInstallStatusOnLoad(): void {
    this.settingsService.getFfmpegInstallStatus().subscribe({
      next: (status) => {
        this.ffmpegInstallStatus = status;
        if (status.inProgress) {
          this.ffmpegInstalling = true;
          this.pollFfmpegInstallStatus();
        }
        this.cdRef.markForCheck();
      },
      error: () => {},
    });
  }

  startFfmpegInstall(): void {
    this.ffmpegInstalling = true;
    this.ffmpegInstallStatus = null;
    this.cdRef.markForCheck();
    this.settingsService.startFfmpegInstall().subscribe({
      next: (status) => {
        this.ffmpegInstallStatus = status;
        this.cdRef.markForCheck();
        this.pollFfmpegInstallStatus();
      },
      error: () => {
        this.ffmpegInstalling = false;
        this.cdRef.markForCheck();
      },
    });
  }

  /** e.g. "45 MB / 89 MB (50%)", or just "45 MB" if the server hasn't reported a total yet. */
  ffmpegInstallProgressLabel(): string {
    const status = this.ffmpegInstallStatus;
    if (!status) return '';

    const downloaded = ManageMediaSettingsComponent.formatBytes(status.bytesDownloaded);
    if (!status.totalBytes) return downloaded;

    const total = ManageMediaSettingsComponent.formatBytes(status.totalBytes);
    const percent = Math.round((status.bytesDownloaded / status.totalBytes) * 100);
    return `${downloaded} / ${total} (${percent}%)`;
  }

  private static formatBytes(bytes: number): string {
    if (!bytes) return '0 MB';
    const mb = bytes / (1024 * 1024);
    return `${mb.toFixed(mb >= 100 ? 0 : 1)} MB`;
  }

  private pollFfmpegInstallStatus(): void {
    this.ffmpegInstallPollSub?.unsubscribe();
    this.ffmpegInstallPollSub = interval(1000).pipe(
      switchMap(() => this.settingsService.getFfmpegInstallStatus()),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe(status => {
      this.ffmpegInstallStatus = status;
      if (!status.inProgress) {
        this.ffmpegInstalling = false;
        this.ffmpegInstallPollSub?.unsubscribe();

        if (status.success) {
          this.toastr.success(translate('manage-media-settings.ffmpeg-install-success'));
          // The install just set ffmpegPath server-side - refresh the form so the field shows it.
          this.settingsService.getServerSettings().subscribe(settings => {
            this.serverSettings = settings;
            this.resetForm();
          });
        } else if (status.error) {
          this.toastr.error(status.error);
        }
      }
      this.cdRef.markForCheck();
    });
  }
}

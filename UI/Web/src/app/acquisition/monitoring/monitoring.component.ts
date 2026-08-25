import {DatePipe} from '@angular/common';
import {ChangeDetectionStrategy, Component, inject, signal} from '@angular/core';
import {FormControl, FormGroup, ReactiveFormsModule, Validators} from '@angular/forms';
import {TranslocoDirective, TranslocoService} from '@jsverse/transloco';
import {ActivatedRoute} from '@angular/router';
import {ToastrService} from 'ngx-toastr';
import {DownloadClientOption, QualityProfile} from '../../_models/acquisition/integration-provider';
import {
  MonitoringSearchRun,
  MonitoringSearchStatus,
  MonitoringTarget,
  MonitoringTargetKind,
  WantedItem,
  WantedItemStatus,
} from '../../_models/acquisition/monitoring';
import {LibrariannMediaType} from '../../_models/metadata/librariann-metadata';
import {MonitoringService} from '../../_services/monitoring.service';
import {AcquisitionGrabService} from '../../_services/acquisition-grab.service';
import {QualityProfileService} from '../../_services/quality-profile.service';
import {ConfirmService} from '../../shared/confirm.service';

@Component({
  selector: 'app-monitoring',
  imports: [ReactiveFormsModule, TranslocoDirective, DatePipe],
  templateUrl: './monitoring.component.html',
  styleUrl: './monitoring.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MonitoringComponent {
  private readonly monitoringService = inject(MonitoringService);
  private readonly acquisitionGrabService = inject(AcquisitionGrabService);
  private readonly qualityProfileService = inject(QualityProfileService);
  private readonly confirmService = inject(ConfirmService);
  private readonly transloco = inject(TranslocoService);
  private readonly toastr = inject(ToastrService);
  private readonly route = inject(ActivatedRoute);

  protected readonly targets = signal<MonitoringTarget[]>([]);
  protected readonly history = signal<MonitoringSearchRun[]>([]);
  protected readonly profiles = signal<QualityProfile[]>([]);
  protected readonly wanted = signal<WantedItem[]>([]);
  protected readonly clients = signal<DownloadClientOption[]>([]);
  protected readonly editing = signal(false);
  protected readonly saving = signal(false);
  protected readonly searchingId = signal<number | null>(null);
  protected readonly syncingId = signal<number | null>(null);
  protected readonly Kind = MonitoringTargetKind;
  protected readonly MediaType = LibrariannMediaType;
  protected readonly SearchStatus = MonitoringSearchStatus;
  protected readonly WantedStatus = WantedItemStatus;

  protected readonly form = new FormGroup({
    id: new FormControl(0, {nonNullable: true}),
    librarySeriesId: new FormControl<number | undefined>(undefined, {nonNullable: true}),
    kind: new FormControl(MonitoringTargetKind.Book, {nonNullable: true}),
    mediaType: new FormControl(LibrariannMediaType.Book, {nonNullable: true}),
    qualityProfileId: new FormControl(0, {nonNullable: true, validators: [Validators.min(1)]}),
    title: new FormControl('', {nonNullable: true, validators: [Validators.required, Validators.maxLength(512)]}),
    author: new FormControl('', {nonNullable: true, validators: [Validators.maxLength(256)]}),
    isbn: new FormControl('', {nonNullable: true, validators: [Validators.maxLength(32)]}),
    language: new FormControl('English', {nonNullable: true, validators: [Validators.required]}),
    externalProviderKey: new FormControl('', {nonNullable: true}),
    externalItemId: new FormControl('', {nonNullable: true}),
    monitorMissing: new FormControl(true, {nonNullable: true}),
    monitorFuture: new FormControl(true, {nonNullable: true}),
    automaticGrabEnabled: new FormControl(false, {nonNullable: true}),
    downloadClientId: new FormControl<number | undefined>(undefined, {nonNullable: true}),
    minimumAutomaticGrabScore: new FormControl(90, {nonNullable: true, validators: [Validators.min(0), Validators.max(500)]}),
    isEnabled: new FormControl(true, {nonNullable: true}),
    searchIntervalHours: new FormControl(24, {nonNullable: true, validators: [Validators.min(1), Validators.max(720)]}),
  });

  constructor() {
    this.reload();
    this.acquisitionGrabService.getClients().subscribe(clients => this.clients.set(clients));
    this.qualityProfileService.getAll().subscribe(profiles => {
      this.profiles.set(profiles);
      this.selectDefaultProfile();
    });
    this.form.controls.mediaType.valueChanges.subscribe(() => this.selectDefaultProfile());
    this.form.controls.automaticGrabEnabled.valueChanges.subscribe(enabled => {
      this.form.controls.downloadClientId.setValidators(enabled ? [Validators.required] : []);
      this.form.controls.downloadClientId.updateValueAndValidity();
    });
    this.route.queryParamMap.subscribe(params => {
      const seriesId = Number(params.get('seriesId'));
      const title = params.get('title');
      if (!seriesId || !title) return;
      const libraryType = Number(params.get('libraryType'));
      const mediaType = libraryType === 0 ? LibrariannMediaType.Manga
        : libraryType === 2 || libraryType === 4 ? LibrariannMediaType.Book : LibrariannMediaType.Comic;
      this.newTarget();
      this.form.patchValue({
        librarySeriesId: seriesId,
        title,
        mediaType,
        kind: mediaType === LibrariannMediaType.Book ? MonitoringTargetKind.Book : MonitoringTargetKind.Series,
      });
      this.selectDefaultProfile();
    });
  }

  protected matchingProfiles(): QualityProfile[] {
    return this.profiles().filter(profile => profile.mediaType === this.form.controls.mediaType.value);
  }

  protected newTarget(): void {
    this.editing.set(true);
    this.form.reset({
      id: 0, librarySeriesId: undefined, kind: MonitoringTargetKind.Book, mediaType: LibrariannMediaType.Book, qualityProfileId: 0,
      title: '', author: '', isbn: '', language: 'English', externalProviderKey: '', externalItemId: '',
      monitorMissing: true, monitorFuture: true, isEnabled: true, searchIntervalHours: 24,
      automaticGrabEnabled: false, downloadClientId: undefined, minimumAutomaticGrabScore: 90,
    });
    this.selectDefaultProfile();
  }

  protected edit(target: MonitoringTarget): void {
    this.editing.set(true);
    this.form.reset({
      id: target.id, librarySeriesId: target.librarySeriesId, kind: target.kind, mediaType: target.mediaType, qualityProfileId: target.qualityProfileId,
      title: target.title, author: target.author, isbn: target.isbn, language: target.language,
      externalProviderKey: target.externalProviderKey, externalItemId: target.externalItemId,
      monitorMissing: target.monitorMissing, monitorFuture: target.monitorFuture, isEnabled: target.isEnabled,
      automaticGrabEnabled: target.automaticGrabEnabled, downloadClientId: target.downloadClientId,
      minimumAutomaticGrabScore: target.minimumAutomaticGrabScore,
      searchIntervalHours: target.searchIntervalHours,
    });
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    this.monitoringService.upsert(this.form.getRawValue()).subscribe({
      next: () => {
        this.saving.set(false);
        this.editing.set(false);
        this.toastr.success(this.transloco.translate('monitoring.saved'));
        this.reload();
      },
      error: () => this.saving.set(false),
    });
  }

  protected async remove(target: MonitoringTarget): Promise<void> {
    if (!await this.confirmService.confirm(this.transloco.translate('monitoring.confirm-delete', {title: target.title}))) return;
    this.monitoringService.delete(target.id).subscribe(() => this.reload());
  }

  protected searchNow(target: MonitoringTarget): void {
    this.searchingId.set(target.id);
    this.monitoringService.searchNow(target.id).subscribe({
      next: () => {
        this.searchingId.set(null);
        this.toastr.success(this.transloco.translate('monitoring.search-queued'));
      },
      error: () => this.searchingId.set(null),
    });
  }

  protected syncCatalog(target: MonitoringTarget): void {
    this.syncingId.set(target.id);
    this.monitoringService.syncCatalog(target.id).subscribe({
      next: () => {
        this.syncingId.set(null);
        this.toastr.success(this.transloco.translate('monitoring.catalog-queued'));
      },
      error: () => this.syncingId.set(null),
    });
  }

  protected wantedStatusLabel(status: WantedItemStatus): string {
    return WantedItemStatus[status] ?? 'Unknown';
  }

  protected kindLabel(kind: MonitoringTargetKind): string {
    return MonitoringTargetKind[kind] ?? 'Unknown';
  }

  protected mediaLabel(mediaType: LibrariannMediaType): string {
    return LibrariannMediaType[mediaType] ?? 'Unknown';
  }

  protected statusLabel(status: MonitoringSearchStatus): string {
    return MonitoringSearchStatus[status]?.replace(/([a-z])([A-Z])/g, '$1 $2') ?? 'Unknown';
  }

  private selectDefaultProfile(): void {
    const options = this.matchingProfiles();
    if (!options.some(profile => profile.id === this.form.controls.qualityProfileId.value)) {
      this.form.controls.qualityProfileId.setValue(options[0]?.id ?? 0);
    }
  }

  private reload(): void {
    this.monitoringService.getAll().subscribe(targets => this.targets.set(targets));
    this.monitoringService.getHistory(undefined, 50).subscribe(history => this.history.set(history));
    this.monitoringService.getWanted().subscribe(wanted => this.wanted.set(wanted));
  }
}

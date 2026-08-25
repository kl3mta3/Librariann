import {DatePipe} from '@angular/common';
import {ChangeDetectionStrategy, Component, inject, signal} from '@angular/core';
import {FormControl, FormGroup, ReactiveFormsModule, Validators} from '@angular/forms';
import {TranslocoDirective} from '@jsverse/transloco';
import {TranslocoService} from '@jsverse/transloco';
import {ToastrService} from 'ngx-toastr';
import {BytesPipe} from '../../_pipes/bytes.pipe';
import {AcquisitionMediaFormat, DownloadClientOption, DownloadProtocol, InteractiveSearchResponse, QualityProfile, ReleaseDecision} from '../../_models/acquisition/integration-provider';
import {AcquisitionSearchService} from '../../_services/acquisition-search.service';
import {AcquisitionGrabService} from '../../_services/acquisition-grab.service';
import {AccountService} from '../../_services/account.service';
import {QualityProfileService} from '../../_services/quality-profile.service';

@Component({
  selector: 'app-interactive-search',
  imports: [ReactiveFormsModule, TranslocoDirective, BytesPipe, DatePipe],
  templateUrl: './interactive-search.component.html',
  styleUrl: './interactive-search.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InteractiveSearchComponent {
  private readonly searchService = inject(AcquisitionSearchService);
  private readonly grabService = inject(AcquisitionGrabService);
  private readonly toastr = inject(ToastrService);
  private readonly transloco = inject(TranslocoService);
  private readonly qualityProfileService = inject(QualityProfileService);
  protected readonly accountService = inject(AccountService);

  protected readonly searching = signal(false);
  protected readonly response = signal<InteractiveSearchResponse | null>(null);
  protected readonly clients = signal<DownloadClientOption[]>([]);
  protected readonly profiles = signal<QualityProfile[]>([]);
  protected readonly grabbing = signal<string | null>(null);
  protected readonly Format = AcquisitionMediaFormat;
  protected readonly form = new FormGroup({
    title: new FormControl('', {nonNullable: true, validators: [Validators.required]}),
    author: new FormControl('', {nonNullable: true}),
    qualityProfileId: new FormControl(0, {nonNullable: true, validators: [Validators.min(1)]}),
    ownedFormat: new FormControl<AcquisitionMediaFormat | null>(null),
  });

  constructor() {
    this.qualityProfileService.getAll().subscribe(profiles => {
      this.profiles.set(profiles);
      if (profiles.length > 0 && this.form.controls.qualityProfileId.value === 0) {
        this.form.controls.qualityProfileId.setValue(profiles[0].id);
      }
    });
    if (this.accountService.canGrabReleases()) {
      this.grabService.getClients().subscribe(clients => this.clients.set(clients));
    }
  }

  search(): void {
    if (this.form.invalid) return;
    const value = this.form.getRawValue();
    this.searching.set(true);
    this.searchService.search({
      title: value.title,
      author: value.author,
      qualityProfileId: value.qualityProfileId,
      ownedFormat: value.ownedFormat ?? undefined,
    }).subscribe({
      next: response => {
        this.response.set(response);
        this.searching.set(false);
      },
      error: () => this.searching.set(false),
    });
  }

  formatLabel(format: AcquisitionMediaFormat): string {
    return AcquisitionMediaFormat[format]?.toUpperCase() ?? 'UNKNOWN';
  }

  trackRelease(result: ReleaseDecision): string {
    return `${result.release.providerKey}:${result.release.providerReleaseId}`;
  }

  compatibleClients(protocol: DownloadProtocol): DownloadClientOption[] {
    return this.clients().filter(client => client.protocol === protocol);
  }

  grab(result: ReleaseDecision, clientId: string): void {
    if (!result.grabToken || !clientId) return;
    this.grabbing.set(result.grabToken);
    this.grabService.grab(result.grabToken, Number(clientId)).subscribe({
      next: response => {
        this.grabbing.set(null);
        this.toastr.success(this.transloco.translate('interactive-search.grabbed', {
          title: response.releaseTitle,
          client: response.downloadClientName,
        }));
      },
      error: () => this.grabbing.set(null),
    });
  }
}

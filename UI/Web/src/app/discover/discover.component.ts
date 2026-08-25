import {DatePipe} from '@angular/common';
import {ChangeDetectionStrategy, Component, computed, inject, signal} from '@angular/core';
import {FormControl, FormGroup, ReactiveFormsModule, Validators} from '@angular/forms';
import {RouterLink} from '@angular/router';
import {ToastrService} from 'ngx-toastr';
import {AcquisitionMediaFormat, DownloadClientOption, DownloadProtocol, ReleaseDecision} from '../_models/acquisition/integration-provider';
import {LibrariannMediaType} from '../_models/metadata/librariann-metadata';
import {UnifiedDiscoveryResponse} from '../_models/search/unified-discovery';
import {BytesPipe} from '../_pipes/bytes.pipe';
import {AccountService} from '../_services/account.service';
import {AcquisitionGrabService} from '../_services/acquisition-grab.service';
import {DiscoveryService} from '../_services/discovery.service';
import {ImageService} from '../_services/image.service';
import {AgeRating} from '../_models/metadata/age-rating';

@Component({
  selector: 'app-discover',
  imports: [ReactiveFormsModule, RouterLink, BytesPipe, DatePipe],
  templateUrl: './discover.component.html',
  styleUrl: './discover.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DiscoverComponent {
  private readonly discovery = inject(DiscoveryService);
  private readonly grabService = inject(AcquisitionGrabService);
  private readonly toastr = inject(ToastrService);
  protected readonly accountService = inject(AccountService);
  protected readonly imageService = inject(ImageService);
  protected readonly MediaType = LibrariannMediaType;

  protected readonly searching = signal(false);
  protected readonly searched = signal(false);
  protected readonly result = signal<UnifiedDiscoveryResponse | null>(null);
  protected readonly clients = signal<DownloadClientOption[]>([]);
  protected readonly grabbing = signal<string | null>(null);
  protected readonly canIncludeAdult = computed(() => {
    const rating = this.accountService.currentUser()?.ageRestriction.ageRating;
    return rating === AgeRating.NotApplicable || (rating !== undefined && rating >= AgeRating.AdultsOnly);
  });

  protected readonly form = new FormGroup({
    query: new FormControl('', {nonNullable: true, validators: [Validators.required, Validators.minLength(2)]}),
    author: new FormControl('', {nonNullable: true}),
    mediaType: new FormControl(LibrariannMediaType.Book, {nonNullable: true}),
    includeAdult: new FormControl(false, {nonNullable: true}),
  });

  constructor() {
    if (this.accountService.canGrabReleases()) {
      this.grabService.getClients().subscribe(clients => this.clients.set(clients));
    }
  }

  search(): void {
    if (this.form.invalid) return;
    const value = this.form.getRawValue();
    this.searching.set(true);
    this.discovery.search({
      query: value.query,
      author: value.author,
      isbn: '',
      language: '',
      mediaType: value.mediaType,
      includeAdult: value.includeAdult && this.canIncludeAdult(),
    }).subscribe({
      next: result => {
        this.result.set(result);
        this.searched.set(true);
        this.searching.set(false);
      },
      error: () => this.searching.set(false),
    });
  }

  formatLabel(format: AcquisitionMediaFormat): string {
    return AcquisitionMediaFormat[format]?.toUpperCase() ?? 'UNKNOWN';
  }

  compatibleClients(protocol: DownloadProtocol): DownloadClientOption[] {
    return this.clients().filter(client => client.protocol === protocol);
  }

  grab(decision: ReleaseDecision, clientId: string): void {
    if (!decision.grabToken || !clientId) return;
    this.grabbing.set(decision.grabToken);
    this.grabService.grab(decision.grabToken, Number(clientId)).subscribe({
      next: response => {
        this.grabbing.set(null);
        this.toastr.success(`${response.releaseTitle} was sent to ${response.downloadClientName}.`);
      },
      error: () => this.grabbing.set(null),
    });
  }
}

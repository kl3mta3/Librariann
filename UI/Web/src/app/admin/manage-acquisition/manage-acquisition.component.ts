import {ChangeDetectionStrategy, Component, inject, signal} from '@angular/core';
import {FormControl, FormGroup, ReactiveFormsModule, Validators} from '@angular/forms';
import {TranslocoDirective, TranslocoService} from '@jsverse/transloco';
import {ToastrService} from 'ngx-toastr';
import {
  DownloadClientKind,
  IndexerProtocol,
  IntegrationProvider,
  IntegrationProviderCategory,
  AcquisitionMediaFormat,
  QualityProfile,
  UpsertIntegrationProvider
} from '../../_models/acquisition/integration-provider';
import {IntegrationProviderService} from '../../_services/integration-provider.service';
import {ConfirmService} from '../../shared/confirm.service';
import {QualityProfileService} from '../../_services/quality-profile.service';
import {LibrariannMediaType} from '../../_models/metadata/librariann-metadata';

@Component({
  selector: 'app-manage-acquisition',
  imports: [ReactiveFormsModule, TranslocoDirective],
  templateUrl: './manage-acquisition.component.html',
  styleUrl: './manage-acquisition.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ManageAcquisitionComponent {
  private readonly providerService = inject(IntegrationProviderService);
  private readonly toastr = inject(ToastrService);
  private readonly transloco = inject(TranslocoService);
  private readonly confirmService = inject(ConfirmService);
  private readonly qualityProfileService = inject(QualityProfileService);

  protected readonly providers = signal<IntegrationProvider[]>([]);
  protected readonly editing = signal(false);
  protected readonly saving = signal(false);
  protected readonly testingId = signal<number | null>(null);
  protected readonly ProviderCategory = IntegrationProviderCategory;
  protected readonly MediaType = LibrariannMediaType;
  protected readonly profiles = signal<QualityProfile[]>([]);
  protected readonly editingProfile = signal(false);
  protected readonly savingProfile = signal(false);

  protected readonly form = new FormGroup({
    id: new FormControl(0, {nonNullable: true}),
    name: new FormControl('', {nonNullable: true, validators: [Validators.required]}),
    category: new FormControl(IntegrationProviderCategory.DownloadClient, {nonNullable: true}),
    providerType: new FormControl('utorrent', {nonNullable: true, validators: [Validators.required]}),
    baseUrl: new FormControl('', {nonNullable: true, validators: [Validators.required]}),
    allowPrivateNetwork: new FormControl(false, {nonNullable: true}),
    isEnabled: new FormControl(true, {nonNullable: true}),
    downloadCategory: new FormControl('librariann', {nonNullable: true}),
    remotePath: new FormControl('', {nonNullable: true}),
    localPath: new FormControl('', {nonNullable: true}),
    username: new FormControl('', {nonNullable: true}),
    password: new FormControl('', {nonNullable: true}),
    apiKey: new FormControl('', {nonNullable: true}),
  });

  protected readonly profileForm = new FormGroup({
    id: new FormControl(0, {nonNullable: true}),
    name: new FormControl('', {nonNullable: true, validators: [Validators.required]}),
    mediaType: new FormControl(LibrariannMediaType.Book, {nonNullable: true}),
    language: new FormControl('English', {nonNullable: true, validators: [Validators.required]}),
    upgradeAllowed: new FormControl(true, {nonNullable: true}),
    preferRetail: new FormControl(true, {nonNullable: true}),
    cutoffFormat: new FormControl(AcquisitionMediaFormat.Epub, {nonNullable: true}),
    minimumSizeMb: new FormControl<number | null>(null),
    maximumSizeMb: new FormControl<number | null>(null),
    scores: new FormGroup({
      epub: new FormControl(100, {nonNullable: true}),
      azw3: new FormControl(90, {nonNullable: true}),
      mobi: new FormControl(70, {nonNullable: true}),
      pdf: new FormControl(50, {nonNullable: true}),
      cbz: new FormControl(100, {nonNullable: true}),
      cbr: new FormControl(80, {nonNullable: true}),
      cb7: new FormControl(75, {nonNullable: true}),
    }),
  });

  constructor() {
    this.reload();
    this.form.controls.category.valueChanges.subscribe(() => this.selectProviderDefaults());
    this.profileForm.controls.mediaType.valueChanges.subscribe(() => this.resetProfileFormats());
  }

  edit(provider: IntegrationProvider): void {
    this.editing.set(true);
    this.form.reset({
      id: provider.id,
      name: provider.name,
      category: provider.category,
      providerType: provider.providerType,
      baseUrl: provider.baseUrl,
      allowPrivateNetwork: provider.allowPrivateNetwork,
      isEnabled: provider.isEnabled,
      downloadCategory: provider.downloadCategory,
      remotePath: provider.remotePath,
      localPath: provider.localPath,
      username: '',
      password: '',
      apiKey: '',
    });
  }

  newProvider(): void {
    this.editing.set(true);
    this.form.reset({
      id: 0,
      name: '',
      category: IntegrationProviderCategory.DownloadClient,
      providerType: 'utorrent',
      baseUrl: '',
      allowPrivateNetwork: false,
      isEnabled: true,
      downloadCategory: 'librariann',
      remotePath: '',
      localPath: '',
      username: '',
      password: '',
      apiKey: '',
    });
  }

  cancel(): void {
    this.editing.set(false);
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const isDownloadClient = value.category === IntegrationProviderCategory.DownloadClient;
    const isIndexer = value.category === IntegrationProviderCategory.Indexer;
    const model: UpsertIntegrationProvider = {
      id: value.id,
      name: value.name,
      category: value.category,
      providerType: value.providerType,
      baseUrl: value.baseUrl,
      allowPrivateNetwork: value.allowPrivateNetwork,
      isEnabled: value.isEnabled,
      downloadCategory: value.downloadCategory,
      remotePath: isDownloadClient ? value.remotePath : '',
      localPath: isDownloadClient ? value.localPath : '',
      tags: [],
      downloadClientKind: isDownloadClient ? this.downloadKind(value.providerType) : undefined,
      indexerProtocol: isIndexer ? this.indexerProtocol(value.providerType) : undefined,
      username: value.username || undefined,
      password: value.password || undefined,
      apiKey: value.apiKey || undefined,
      clearUsername: false,
      clearPassword: false,
      clearApiKey: false,
    };

    this.saving.set(true);
    const request = model.id > 0 ? this.providerService.update(model) : this.providerService.create(model);
    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.editing.set(false);
        this.toastr.success('Provider saved');
        this.reload();
      },
      error: () => this.saving.set(false),
    });
  }

  async remove(provider: IntegrationProvider): Promise<void> {
    if (!await this.confirmService.confirm(this.transloco.translate('manage-acquisition.confirm-delete', {name: provider.name}))) return;
    this.providerService.delete(provider.id).subscribe(() => this.reload());
  }

  test(provider: IntegrationProvider): void {
    this.testingId.set(provider.id);
    this.providerService.test(provider.id).subscribe({
      next: result => {
        this.testingId.set(null);
        result.isSuccess ? this.toastr.success(result.message) : this.toastr.error(result.message);
      },
      error: () => this.testingId.set(null),
    });
  }

  providerLabel(provider: IntegrationProvider): string {
    if (provider.downloadClientKind === DownloadClientKind.QBittorrent) return 'qBittorrent';
    if (provider.downloadClientKind === DownloadClientKind.Sabnzbd) return 'SABnzbd';
    if (provider.downloadClientKind === DownloadClientKind.UTorrent) return 'µTorrent';
    if (provider.indexerProtocol === IndexerProtocol.Torznab) return 'Torznab';
    if (provider.indexerProtocol === IndexerProtocol.Newznab) return 'Newznab';
    if (provider.category === IntegrationProviderCategory.Metadata && provider.providerType === 'open-library') return 'Open Library';
    if (provider.category === IntegrationProviderCategory.Metadata && provider.providerType === 'google-books') return 'Google Books';
    if (provider.category === IntegrationProviderCategory.Metadata && provider.providerType === 'anilist') return 'AniList';
    if (provider.category === IntegrationProviderCategory.Metadata && provider.providerType === 'mangadex') return 'MangaDex';
    if (provider.category === IntegrationProviderCategory.Metadata && provider.providerType === 'comic-vine') return 'Comic Vine';
    return provider.providerType;
  }

  protected formatOptions(): {format: AcquisitionMediaFormat; control: ScoreControl; label: string}[] {
    if (this.profileForm.controls.mediaType.value === LibrariannMediaType.Book) {
      return [
        {format: AcquisitionMediaFormat.Epub, control: 'epub', label: 'EPUB'},
        {format: AcquisitionMediaFormat.Azw3, control: 'azw3', label: 'AZW3'},
        {format: AcquisitionMediaFormat.Mobi, control: 'mobi', label: 'MOBI'},
        {format: AcquisitionMediaFormat.Pdf, control: 'pdf', label: 'PDF'},
      ];
    }
    return [
      {format: AcquisitionMediaFormat.Cbz, control: 'cbz', label: 'CBZ'},
      {format: AcquisitionMediaFormat.Cbr, control: 'cbr', label: 'CBR'},
      {format: AcquisitionMediaFormat.Cb7, control: 'cb7', label: 'CB7'},
      {format: AcquisitionMediaFormat.Pdf, control: 'pdf', label: 'PDF'},
    ];
  }

  newQualityProfile(): void {
    this.editingProfile.set(true);
    this.profileForm.reset({id: 0, name: '', mediaType: LibrariannMediaType.Book, language: 'English',
      upgradeAllowed: true, preferRetail: true, cutoffFormat: AcquisitionMediaFormat.Epub,
      minimumSizeMb: null, maximumSizeMb: null,
      scores: {epub: 100, azw3: 90, mobi: 70, pdf: 50, cbz: 100, cbr: 80, cb7: 75}});
  }

  editQualityProfile(profile: QualityProfile): void {
    this.editingProfile.set(true);
    this.profileForm.reset({
      id: profile.id,
      name: profile.name,
      mediaType: profile.mediaType,
      language: profile.language,
      upgradeAllowed: profile.upgradeAllowed,
      preferRetail: profile.preferRetail,
      cutoffFormat: profile.cutoffFormat,
      minimumSizeMb: profile.minimumSizeBytes ? profile.minimumSizeBytes / 1048576 : null,
      maximumSizeMb: profile.maximumSizeBytes ? profile.maximumSizeBytes / 1048576 : null,
      scores: {
        epub: profile.formatScores[AcquisitionMediaFormat.Epub] ?? 0,
        azw3: profile.formatScores[AcquisitionMediaFormat.Azw3] ?? 0,
        mobi: profile.formatScores[AcquisitionMediaFormat.Mobi] ?? 0,
        pdf: profile.formatScores[AcquisitionMediaFormat.Pdf] ?? 0,
        cbz: profile.formatScores[AcquisitionMediaFormat.Cbz] ?? 0,
        cbr: profile.formatScores[AcquisitionMediaFormat.Cbr] ?? 0,
        cb7: profile.formatScores[AcquisitionMediaFormat.Cb7] ?? 0,
      },
    });
  }

  saveQualityProfile(): void {
    if (this.profileForm.invalid) return;
    const value = this.profileForm.getRawValue();
    const scores: Record<number, number> = {};
    for (const option of this.formatOptions()) scores[option.format] = value.scores[option.control];
    this.savingProfile.set(true);
    this.qualityProfileService.upsert({
      id: value.id,
      name: value.name,
      mediaType: value.mediaType,
      language: value.language,
      upgradeAllowed: value.upgradeAllowed,
      preferRetail: value.preferRetail,
      cutoffFormat: value.cutoffFormat,
      minimumSizeBytes: value.minimumSizeMb ? Math.round(value.minimumSizeMb * 1048576) : undefined,
      maximumSizeBytes: value.maximumSizeMb ? Math.round(value.maximumSizeMb * 1048576) : undefined,
      formatScores: scores,
    }).subscribe({
      next: () => { this.savingProfile.set(false); this.editingProfile.set(false); this.reloadProfiles(); },
      error: () => this.savingProfile.set(false),
    });
  }

  async removeQualityProfile(profile: QualityProfile): Promise<void> {
    if (!await this.confirmService.confirm(this.transloco.translate('manage-acquisition.confirm-delete-profile', {name: profile.name}))) return;
    this.qualityProfileService.delete(profile.id).subscribe(() => this.reloadProfiles());
  }

  profileMediaLabel(mediaType: number): string {
    return LibrariannMediaType[mediaType] ?? 'Unknown';
  }

  private reload(): void {
    this.providerService.getAll().subscribe(providers => this.providers.set(providers));
    this.reloadProfiles();
  }

  private reloadProfiles(): void {
    this.qualityProfileService.getAll().subscribe(profiles => this.profiles.set(profiles));
  }

  private selectProviderDefaults(): void {
    const category = this.form.controls.category.value;
    const type = category === IntegrationProviderCategory.DownloadClient
      ? 'utorrent'
      : category === IntegrationProviderCategory.Indexer ? 'torznab' : 'open-library';
    this.form.controls.providerType.setValue(type);
    if (category === IntegrationProviderCategory.Metadata) this.form.controls.baseUrl.setValue('https://openlibrary.org');
  }

  protected selectMetadataBaseUrl(): void {
    if (this.form.controls.category.value !== IntegrationProviderCategory.Metadata) return;
    const type = this.form.controls.providerType.value;
    this.form.controls.baseUrl.setValue(type === 'google-books'
      ? 'https://www.googleapis.com/books/v1'
      : type === 'anilist' ? 'https://graphql.anilist.co'
        : type === 'mangadex' ? 'https://api.mangadex.org'
          : type === 'comic-vine' ? 'https://comicvine.gamespot.com/api/' : 'https://openlibrary.org');
  }

  private resetProfileFormats(): void {
    const book = this.profileForm.controls.mediaType.value === LibrariannMediaType.Book;
    this.profileForm.controls.cutoffFormat.setValue(book ? AcquisitionMediaFormat.Epub : AcquisitionMediaFormat.Cbz);
  }

  private downloadKind(providerType: string): DownloadClientKind {
    if (providerType === 'qbittorrent') return DownloadClientKind.QBittorrent;
    if (providerType === 'sabnzbd') return DownloadClientKind.Sabnzbd;
    return DownloadClientKind.UTorrent;
  }

  private indexerProtocol(providerType: string): IndexerProtocol {
    return providerType === 'newznab' ? IndexerProtocol.Newznab : IndexerProtocol.Torznab;
  }
}

type ScoreControl = 'epub' | 'azw3' | 'mobi' | 'pdf' | 'cbz' | 'cbr' | 'cb7';

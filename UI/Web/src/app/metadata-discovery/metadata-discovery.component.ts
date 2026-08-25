import {ChangeDetectionStrategy, Component, computed, inject, signal} from '@angular/core';
import {FormControl, FormGroup, ReactiveFormsModule} from '@angular/forms';
import {ActivatedRoute} from '@angular/router';
import {TranslocoDirective} from '@jsverse/transloco';
import {
  LibrariannMediaType,
  MetadataFieldKey,
  MetadataMatchDecision
} from '../_models/metadata/librariann-metadata';
import {LibraryType} from '../_models/library/library';
import {Series} from '../_models/series';
import {LibrariannMetadataService} from '../_services/librariann-metadata.service';
import {SeriesService} from '../_services/series.service';
import {AccountService} from '../_services/account.service';
import {AgeRating} from '../_models/metadata/age-rating';

@Component({
  selector: 'app-metadata-discovery',
  imports: [ReactiveFormsModule, TranslocoDirective],
  templateUrl: './metadata-discovery.component.html',
  styleUrl: './metadata-discovery.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MetadataDiscoveryComponent {
  private readonly metadataService = inject(LibrariannMetadataService);
  private readonly seriesService = inject(SeriesService);
  private readonly route = inject(ActivatedRoute);
  private readonly accountService = inject(AccountService);
  protected readonly MediaType = LibrariannMediaType;
  protected readonly searching = signal(false);
  protected readonly searched = signal(false);
  protected readonly results = signal<MetadataMatchDecision[]>([]);
  protected readonly failures = signal<{providerKey: string; providerName: string; message: string}[]>([]);
  protected readonly targetSeries = signal<Series | null>(null);
  protected readonly applyingToken = signal<string | null>(null);
  protected readonly appliedTokens = signal<ReadonlySet<string>>(new Set());
  protected readonly applyMessage = signal('');
  protected readonly canIncludeAdult = computed(() => {
    const user = this.accountService.currentUser();
    const rating = user?.ageRestriction.ageRating;
    return rating === AgeRating.NotApplicable || (rating !== undefined && rating >= AgeRating.AdultsOnly);
  });

  protected readonly form = new FormGroup({
    mediaType: new FormControl(LibrariannMediaType.Book, {nonNullable: true}),
    title: new FormControl('', {nonNullable: true}),
    author: new FormControl('', {nonNullable: true}),
    series: new FormControl('', {nonNullable: true}),
    isbn: new FormControl('', {nonNullable: true}),
    language: new FormControl('', {nonNullable: true}),
    publicationYear: new FormControl<number | null>(null),
    includeAdult: new FormControl(false, {nonNullable: true}),
  });

  constructor() {
    const seriesId = Number(this.route.snapshot.queryParamMap.get('seriesId'));
    const libraryType = Number(this.route.snapshot.queryParamMap.get('libraryType'));
    if (Number.isInteger(seriesId) && seriesId > 0) {
      this.seriesService.getSeries(seriesId).subscribe(series => {
        this.targetSeries.set(series);
        this.form.patchValue({
          title: series.name,
          mediaType: this.toMediaType(libraryType),
        });
      });
    }
  }

  search(): void {
    const value = this.form.getRawValue();
    if (![value.title, value.author, value.isbn].some(field => field.trim().length > 0)) return;
    this.searching.set(true);
    this.metadataService.search({
      mediaType: value.mediaType,
      title: value.title,
      author: value.author,
      series: value.series,
      isbn: value.isbn,
      language: value.language,
      publicationYear: value.publicationYear ?? undefined,
      includeAdult: value.includeAdult && this.canIncludeAdult(),
      identifiers: {},
    }).subscribe({
      next: response => {
        this.results.set(response.results);
        this.failures.set(response.providerFailures);
        this.searched.set(true);
        this.searching.set(false);
      },
      error: () => this.searching.set(false),
    });
  }

  trackResult(result: MetadataMatchDecision): string {
    return `${result.candidate.providerKey}:${result.candidate.externalId}`;
  }

  apply(result: MetadataMatchDecision): void {
    const series = this.targetSeries();
    if (!series || !result.applyToken || this.appliedTokens().has(result.applyToken)) return;
    this.applyingToken.set(result.applyToken);
    this.applyMessage.set('');
    this.metadataService.apply(series.id, result.applyToken, [
      MetadataFieldKey.Description,
      MetadataFieldKey.Cover,
      MetadataFieldKey.Authors,
      MetadataFieldKey.Publisher,
      MetadataFieldKey.PublicationDate,
      MetadataFieldKey.Language,
      MetadataFieldKey.Genres,
      MetadataFieldKey.WebLinks,
    ]).subscribe({
      next: response => {
        const applied = response.fields.filter(field => field.applied).length;
        this.appliedTokens.update(tokens => new Set([...tokens, result.applyToken]));
        this.applyMessage.set(`${applied} metadata field${applied === 1 ? '' : 's'} applied to ${series.name}.`);
        this.applyingToken.set(null);
      },
      error: () => {
        this.applyMessage.set('Metadata could not be applied. Refresh the search result and try again.');
        this.applyingToken.set(null);
      },
    });
  }

  private toMediaType(libraryType: number): LibrariannMediaType {
    if (libraryType === LibraryType.Manga) return LibrariannMediaType.Manga;
    if (libraryType === LibraryType.Comic || libraryType === LibraryType.ComicVine) return LibrariannMediaType.Comic;
    return LibrariannMediaType.Book;
  }
}

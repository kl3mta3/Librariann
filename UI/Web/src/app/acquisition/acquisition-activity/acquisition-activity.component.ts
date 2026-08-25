import {DatePipe, DecimalPipe} from '@angular/common';
import {ChangeDetectionStrategy, Component, computed, inject, signal} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {RouterLink} from '@angular/router';
import {TranslocoDirective, TranslocoService} from '@jsverse/transloco';
import {ToastrService} from 'ngx-toastr';
import {
  AcquisitionDownload,
  AcquisitionDownloadStatus,
  AcquisitionMediaFormat,
  ImportAnalysisResult,
  ImportCandidate,
  ImportDestinationOption,
} from '../../_models/acquisition/integration-provider';
import {AcquisitionQueueService} from '../../_services/acquisition-queue.service';
import {SettingsTabId} from '../../sidenav/preference-nav/preference-nav.component';

enum ActivityView {
  Queue = 'queue',
  Import = 'import',
  History = 'history',
  Failed = 'failed',
}

@Component({
  selector: 'app-acquisition-activity',
  imports: [TranslocoDirective, DatePipe, DecimalPipe, FormsModule, RouterLink],
  templateUrl: './acquisition-activity.component.html',
  styleUrl: './acquisition-activity.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AcquisitionActivityComponent {
  private readonly queueService = inject(AcquisitionQueueService);
  private readonly toastr = inject(ToastrService);
  private readonly transloco = inject(TranslocoService);
  protected readonly items = signal<AcquisitionDownload[]>([]);
  protected readonly refreshing = signal(false);
  protected readonly analyzingId = signal<number | null>(null);
  protected readonly importingId = signal<number | null>(null);
  protected readonly queueActionId = signal<number | null>(null);
  protected readonly analyses = signal<Record<number, ImportAnalysisResult>>({});
  protected readonly destinations = signal<ImportDestinationOption[]>([]);
  protected readonly drafts = signal<Record<number, ImportDraft>>({});
  protected readonly Status = AcquisitionDownloadStatus;
  protected readonly View = ActivityView;
  protected readonly SettingsTabId = SettingsTabId;
  protected readonly selectedView = signal(ActivityView.Queue);
  protected readonly activeCount = computed(() => this.items().filter(item => item.status === AcquisitionDownloadStatus.Downloading || item.status === AcquisitionDownloadStatus.Queued).length);
  protected readonly importCount = computed(() => this.items().filter(item => item.status === AcquisitionDownloadStatus.Completed ||
    item.status === AcquisitionDownloadStatus.ImportPending || item.status === AcquisitionDownloadStatus.Importing ||
    item.status === AcquisitionDownloadStatus.NeedsManualMatch).length);
  protected readonly failedCount = computed(() => this.items().filter(item => item.status === AcquisitionDownloadStatus.Failed).length);
  protected readonly historyCount = computed(() => this.items().filter(item => item.status === AcquisitionDownloadStatus.Imported || item.status === AcquisitionDownloadStatus.Removed).length);
  protected readonly filteredItems = computed(() => this.items().filter(item => {
    switch (this.selectedView()) {
      case ActivityView.Queue:
        return item.status === AcquisitionDownloadStatus.Queued || item.status === AcquisitionDownloadStatus.Downloading;
      case ActivityView.Import:
        return item.status === AcquisitionDownloadStatus.Completed || item.status === AcquisitionDownloadStatus.ImportPending ||
          item.status === AcquisitionDownloadStatus.Importing || item.status === AcquisitionDownloadStatus.NeedsManualMatch;
      case ActivityView.History:
        return item.status === AcquisitionDownloadStatus.Imported || item.status === AcquisitionDownloadStatus.Removed;
      case ActivityView.Failed:
        return item.status === AcquisitionDownloadStatus.Failed;
    }
  }));

  constructor() {
    this.load();
    this.queueService.getImportDestinations().subscribe(destinations => this.destinations.set(destinations));
  }

  refresh(): void {
    this.refreshing.set(true);
    this.queueService.poll().subscribe({
      next: () => this.load(),
      error: () => this.refreshing.set(false),
    });
  }

  selectView(view: ActivityView): void {
    this.selectedView.set(view);
  }

  retry(item: AcquisitionDownload): void {
    if (item.status !== AcquisitionDownloadStatus.Failed) return;
    this.queueActionId.set(item.id);
    this.queueService.retry(item.id).subscribe({
      next: () => {
        this.toastr.success(this.transloco.translate('acquisition-activity.retry-success'));
        this.queueActionId.set(null);
        this.selectedView.set(ActivityView.Queue);
        this.load();
      },
      error: () => this.queueActionId.set(null),
    });
  }

  remove(item: AcquisitionDownload): void {
    if (item.status === AcquisitionDownloadStatus.Removed || item.status === AcquisitionDownloadStatus.Importing) return;
    const confirmed = window.confirm(this.transloco.translate('acquisition-activity.remove-confirm', {title: item.releaseTitle}));
    if (!confirmed) return;

    this.queueActionId.set(item.id);
    this.queueService.remove(item.id, false).subscribe({
      next: () => {
        this.toastr.success(this.transloco.translate('acquisition-activity.remove-success'));
        this.queueActionId.set(null);
        this.load();
      },
      error: () => this.queueActionId.set(null),
    });
  }

  analyze(item: AcquisitionDownload): void {
    this.analyzingId.set(item.id);
    this.queueService.analyze(item.id).subscribe({
      next: result => {
        this.analyses.update(analyses => ({...analyses, [item.id]: result}));
        const firstCandidate = result.candidates[0];
        const destination = firstCandidate
          ? this.destinations().find(option => option.supportedFormats.includes(firstCandidate.format))
          : undefined;
        this.drafts.update(drafts => ({
          ...drafts,
          [item.id]: {folderId: destination?.folderId ?? 0, destinationSubdirectory: '', destinationBaseName: ''},
        }));
        this.toastr.success(result.message);
        this.analyzingId.set(null);
        this.load();
      },
      error: () => this.analyzingId.set(null),
    });
  }

  compatibleDestinations(candidate: ImportCandidate): ImportDestinationOption[] {
    return this.destinations().filter(destination => destination.supportedFormats.includes(candidate.format));
  }

  canCommit(itemId: number, candidate: ImportCandidate): boolean {
    const folderId = Number(this.draft(itemId).folderId);
    return this.compatibleDestinations(candidate).some(destination => destination.folderId === folderId);
  }

  draft(itemId: number): ImportDraft {
    return this.drafts()[itemId] ?? {folderId: 0, destinationSubdirectory: '', destinationBaseName: ''};
  }

  updateDraft(itemId: number, changes: Partial<ImportDraft>): void {
    this.drafts.update(drafts => ({...drafts, [itemId]: {...this.draft(itemId), ...changes}}));
  }

  commit(item: AcquisitionDownload, candidate: ImportCandidate): void {
    const draft = this.draft(item.id);
    const destination = this.destinations().find(option => option.folderId === Number(draft.folderId));
    if (!destination || !destination.supportedFormats.includes(candidate.format)) return;
    this.importingId.set(item.id);
    this.queueService.commitImport({
      downloadId: item.id,
      libraryId: destination.libraryId,
      folderId: destination.folderId,
      candidateRelativePath: candidate.relativePath,
      destinationSubdirectory: draft.destinationSubdirectory,
      destinationBaseName: draft.destinationBaseName,
    }).subscribe({
      next: result => {
        this.toastr.success(`${result.fileName} imported into ${result.libraryName}`);
        this.importingId.set(null);
        this.analyses.update(analyses => {
          const next = {...analyses};
          delete next[item.id];
          return next;
        });
        this.load();
      },
      error: () => this.importingId.set(null),
    });
  }

  statusLabel(status: AcquisitionDownloadStatus): string {
    return AcquisitionDownloadStatus[status]?.replace(/([a-z])([A-Z])/g, '$1 $2') ?? 'Unknown';
  }

  formatLabel(format: AcquisitionMediaFormat): string {
    return AcquisitionMediaFormat[format]?.toUpperCase() ?? 'UNKNOWN';
  }

  trackItem(item: AcquisitionDownload): number {
    return item.id;
  }

  private load(): void {
    this.queueService.getAll().subscribe({
      next: items => {
        this.items.set(items);
        this.refreshing.set(false);
      },
      error: () => this.refreshing.set(false),
    });
  }
}

interface ImportDraft {
  folderId: number;
  destinationSubdirectory: string;
  destinationBaseName: string;
}

import {HttpClient} from '@angular/common/http';
import {inject, Injectable} from '@angular/core';
import {environment} from '../../environments/environment';
import {
  AcquisitionDownload,
  CommitImportRequest,
  CommitImportResult,
  ImportAnalysisResult,
  ImportDestinationOption,
} from '../_models/acquisition/integration-provider';

@Injectable({providedIn: 'root'})
export class AcquisitionQueueService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl + 'acquisitionqueue';

  getAll() {
    return this.http.get<AcquisitionDownload[]>(this.baseUrl);
  }

  poll() {
    return this.http.post<void>(this.baseUrl + '/poll', {});
  }

  retry(downloadId: number) {
    return this.http.post<void>(`${this.baseUrl}/${downloadId}/retry`, {});
  }

  remove(downloadId: number, deleteData = false) {
    return this.http.post<void>(`${this.baseUrl}/${downloadId}/remove`, {deleteData});
  }

  analyze(downloadId: number) {
    return this.http.post<ImportAnalysisResult>(environment.apiUrl + 'acquisitionimport/analyze', {}, {params: {downloadId}});
  }

  getImportDestinations() {
    return this.http.get<ImportDestinationOption[]>(environment.apiUrl + 'acquisitionimport/destinations');
  }

  commitImport(request: CommitImportRequest) {
    return this.http.post<CommitImportResult>(environment.apiUrl + 'acquisitionimport/commit', request);
  }
}

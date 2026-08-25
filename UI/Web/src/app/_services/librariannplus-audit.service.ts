import {inject, Injectable} from '@angular/core';
import {HttpClient, HttpParams} from '@angular/common/http';
import {Observable} from 'rxjs';
import {map} from 'rxjs/operators';
import {environment} from '../../environments/environment';
import {UtilityService} from '../shared/_services/utility.service';
import {LibrariannPlusAuditEntry} from '../_models/librariannplus/librariann-plus-audit-entry';
import {LibrariannPlusAuditFilter} from '../_models/librariannplus/librariann-plus-audit-filter';
import {LibrariannPlusAuditStats, LibrariannPlusMyAuditStats} from '../_models/librariannplus/librariann-plus-audit-stats';
import {LibrariannPlusAuditSeriesInfo} from '../_models/librariannplus/librariann-plus-audit-series-info';
import {PaginatedResult} from '../_models/pagination';

@Injectable({
  providedIn: 'root'
})
export class LibrariannPlusAuditService {
  private readonly httpClient = inject(HttpClient);
  private readonly utilityService = inject(UtilityService);
  private readonly baseUrl = environment.apiUrl + 'librariann-plus-audit/';

  getSeriesInfo(seriesId: number): Observable<LibrariannPlusAuditSeriesInfo> {
    return this.httpClient.get<LibrariannPlusAuditSeriesInfo>(
      `${this.baseUrl}entries/series/${seriesId}`
    );
  }

  getEntries(filter: LibrariannPlusAuditFilter, pageNum?: number, itemsPerPage?: number): Observable<PaginatedResult<LibrariannPlusAuditEntry[]>> {
    const params = this.utilityService.addPaginationIfExists(new HttpParams(), pageNum, itemsPerPage);
    return this.httpClient.post<Array<LibrariannPlusAuditEntry>>(
      `${this.baseUrl}entries`, filter, { observe: 'response', params }
    ).pipe(map(res => this.utilityService.createPaginatedResult<LibrariannPlusAuditEntry>(res)));
  }

  getStats() {
    return this.httpClient.get<LibrariannPlusAuditStats>(`${this.baseUrl}stats`);
  }

  getMyStats() {
    return this.httpClient.get<LibrariannPlusMyAuditStats>(`${this.baseUrl}my-stats`);
  }

  getMyActivity(filter: LibrariannPlusAuditFilter, pageNum?: number, itemsPerPage?: number): Observable<PaginatedResult<LibrariannPlusAuditEntry[]>> {
    const params = this.utilityService.addPaginationIfExists(new HttpParams(), pageNum, itemsPerPage);
    return this.httpClient.post<Array<LibrariannPlusAuditEntry>>(
      `${this.baseUrl}my-activity`, filter, { observe: 'response', params }
    ).pipe(map(res => this.utilityService.createPaginatedResult<LibrariannPlusAuditEntry>(res)));
  }

  getFailedScrobbleEvents() {
    return this.httpClient.get<number>(this.baseUrl + 'failed-scrobble-events');
  }
}

import {HttpClient} from '@angular/common/http';
import {inject, Injectable} from '@angular/core';
import {environment} from '../../environments/environment';
import {MonitoringSearchRun, MonitoringTarget, UpsertMonitoringTarget, WantedItem} from '../_models/acquisition/monitoring';

@Injectable({providedIn: 'root'})
export class MonitoringService {
  private readonly http = inject(HttpClient);
  private readonly url = environment.apiUrl + 'monitoring';

  getAll() {
    return this.http.get<MonitoringTarget[]>(this.url);
  }

  getHistory(targetId?: number, take = 100) {
    const params: Record<string, string | number> = {take};
    if (targetId) params['targetId'] = targetId;
    return this.http.get<MonitoringSearchRun[]>(this.url + '/history', {params});
  }

  getWanted(targetId?: number) {
    const params: Record<string, number> = {};
    if (targetId) params['targetId'] = targetId;
    return this.http.get<WantedItem[]>(this.url + '/wanted', {params});
  }

  upsert(target: UpsertMonitoringTarget) {
    return this.http.post<MonitoringTarget>(this.url, target);
  }

  delete(id: number) {
    return this.http.delete<void>(this.url, {params: {id}});
  }

  searchNow(id: number) {
    return this.http.post<void>(this.url + '/search-now', null, {params: {id}});
  }

  syncCatalog(id: number) {
    return this.http.post<void>(this.url + '/sync-catalog', null, {params: {id}});
  }
}

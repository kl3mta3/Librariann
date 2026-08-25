import {HttpClient} from '@angular/common/http';
import {inject, Injectable} from '@angular/core';
import {environment} from '../../environments/environment';
import {QualityProfile, UpsertQualityProfile} from '../_models/acquisition/integration-provider';

@Injectable({providedIn: 'root'})
export class QualityProfileService {
  private readonly http = inject(HttpClient);

  getAll() {
    return this.http.get<QualityProfile[]>(environment.apiUrl + 'qualityprofile');
  }

  upsert(profile: UpsertQualityProfile) {
    return this.http.post<QualityProfile>(environment.apiUrl + 'qualityprofile', profile);
  }

  delete(id: number) {
    return this.http.delete<void>(environment.apiUrl + 'qualityprofile', {params: {id}});
  }
}

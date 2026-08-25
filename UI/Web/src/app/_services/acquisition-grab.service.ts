import {HttpClient} from '@angular/common/http';
import {inject, Injectable} from '@angular/core';
import {environment} from '../../environments/environment';
import {DownloadClientOption, GrabReleaseResponse} from '../_models/acquisition/integration-provider';

@Injectable({providedIn: 'root'})
export class AcquisitionGrabService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl + 'acquisitiongrab';

  getClients() {
    return this.http.get<DownloadClientOption[]>(this.baseUrl + '/clients');
  }

  grab(grabToken: string, downloadClientId: number) {
    return this.http.post<GrabReleaseResponse>(this.baseUrl, {grabToken, downloadClientId});
  }
}

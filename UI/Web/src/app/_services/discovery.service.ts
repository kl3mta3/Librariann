import {HttpClient} from '@angular/common/http';
import {inject, Injectable} from '@angular/core';
import {environment} from '../../environments/environment';
import {UnifiedDiscoveryRequest, UnifiedDiscoveryResponse} from '../_models/search/unified-discovery';

@Injectable({providedIn: 'root'})
export class DiscoveryService {
  private readonly http = inject(HttpClient);

  search(request: UnifiedDiscoveryRequest) {
    return this.http.post<UnifiedDiscoveryResponse>(environment.apiUrl + 'discovery', request);
  }
}

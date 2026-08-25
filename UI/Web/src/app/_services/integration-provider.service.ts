import {inject, Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {environment} from '../../environments/environment';
import {IntegrationProvider, ProviderTestResult, UpsertIntegrationProvider} from '../_models/acquisition/integration-provider';

@Injectable({providedIn: 'root'})
export class IntegrationProviderService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl + 'integrationprovider';

  getAll() {
    return this.http.get<IntegrationProvider[]>(this.baseUrl);
  }

  create(provider: UpsertIntegrationProvider) {
    return this.http.post<IntegrationProvider>(this.baseUrl, provider);
  }

  update(provider: UpsertIntegrationProvider) {
    return this.http.put<IntegrationProvider>(this.baseUrl, provider);
  }

  delete(id: number) {
    return this.http.delete(this.baseUrl, {params: {id}});
  }

  test(id: number) {
    return this.http.post<ProviderTestResult>(this.baseUrl + '/test', {}, {params: {id}});
  }
}

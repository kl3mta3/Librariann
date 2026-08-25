import {HttpClient} from '@angular/common/http';
import {inject, Injectable} from '@angular/core';
import {environment} from '../../environments/environment';
import {
  ApplyMetadataResponse,
  MetadataFieldKey,
  MetadataLookupRequest,
  MetadataLookupResponse
} from '../_models/metadata/librariann-metadata';

@Injectable({providedIn: 'root'})
export class LibrariannMetadataService {
  private readonly http = inject(HttpClient);

  search(request: MetadataLookupRequest) {
    return this.http.post<MetadataLookupResponse>(environment.apiUrl + 'metadatalookup/search', request);
  }

  apply(seriesId: number, applyToken: string, fields: MetadataFieldKey[]) {
    return this.http.post<ApplyMetadataResponse>(environment.apiUrl + 'metadatalookup/apply', {
      seriesId,
      applyToken,
      fields,
    });
  }
}

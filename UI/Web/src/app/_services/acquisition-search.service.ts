import {inject, Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {environment} from '../../environments/environment';
import {AcquisitionMediaFormat, InteractiveSearchResponse} from '../_models/acquisition/integration-provider';

export interface AcquisitionSearchQuery {
  title: string;
  author: string;
  qualityProfileId: number;
  ownedFormat?: AcquisitionMediaFormat;
}

@Injectable({providedIn: 'root'})
export class AcquisitionSearchService {
  private readonly http = inject(HttpClient);

  search(query: AcquisitionSearchQuery) {
    return this.http.post<InteractiveSearchResponse>(environment.apiUrl + 'acquisitionsearch', {
      qualityProfileId: query.qualityProfileId,
      search: {
        query: '',
        title: query.title,
        author: query.author,
        series: '',
        isbn: '',
        categories: [],
      },
      evaluation: {
        expectedTitle: query.title,
        expectedAuthor: query.author,
        expectedEdition: '',
        allowedLanguages: [],
        formatScores: {},
        ownedFormat: query.ownedFormat,
        preferRetail: false,
      },
    });
  }
}

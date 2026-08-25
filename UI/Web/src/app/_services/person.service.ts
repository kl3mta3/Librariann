import {inject, Injectable} from '@angular/core';
import {HttpClient, HttpParams} from "@angular/common/http";
import {environment} from "../../environments/environment";
import {Person, PersonRole} from "../_models/metadata/person";
import {PaginatedResult} from "../_models/pagination";
import {Series} from "../_models/series";
import {map} from "rxjs/operators";
import {UtilityService} from "../shared/_services/utility.service";
import {BrowsePerson} from "../_models/metadata/browse/browse-person";
import {StandaloneChapter} from "../_models/standalone-chapter";
import {TextResonse} from "../_types/text-response";
import {FilterV2} from "../_models/metadata/v2/filter-v2";
import {PersonFilterField} from "../_models/metadata/v2/person-filter-field";
import {PersonSortField} from "../_models/metadata/v2/person-sort-field";
import {AuthorMetadataCandidate} from "../_models/metadata/author-metadata-candidate";

@Injectable({
  providedIn: 'root'
})
export class PersonService {
  private httpClient = inject(HttpClient);
  private utilityService = inject(UtilityService);


  baseUrl = environment.apiUrl;

  updatePerson(person: Person) {
    return this.httpClient.post<Person>(this.baseUrl + "person/update", person);
  }

  get(name: string) {
    return this.httpClient.get<Person | null>(this.baseUrl + `person?name=${name}`);
  }

  searchPerson(name: string) {
    return this.httpClient.get<Array<Person>>(this.baseUrl + `person/search?queryString=${encodeURIComponent(name)}`);
  }

  getRolesForPerson(personId: number) {
    return this.httpClient.get<Array<PersonRole>>(this.baseUrl + `person/roles?personId=${personId}`);
  }

  getSeriesMostKnownFor(personId: number) {
    return this.httpClient.get<Array<Series>>(this.baseUrl + `person/series-known-for?personId=${personId}`);
  }

  setFollowed(personId: number, followed: boolean) {
    return this.httpClient.post<boolean>(this.baseUrl + `person/follow?personId=${personId}&followed=${followed}`, {});
  }

  getChaptersByRole(personId: number, role: PersonRole) {
    return this.httpClient.get<Array<StandaloneChapter>>(this.baseUrl + `person/chapters-by-role?personId=${personId}&role=${role}`);
  }

  getAuthorsToBrowse(filter: FilterV2<PersonFilterField, PersonSortField>, pageNum?: number, itemsPerPage?: number) {
    let params = new HttpParams();
    params = this.utilityService.addPaginationIfExists(params, pageNum, itemsPerPage);

    return this.httpClient.post<PaginatedResult<BrowsePerson[]>>(this.baseUrl + `person/all`, filter, {observe: 'response', params}).pipe(
      map((response: any) => {
        return this.utilityService.createPaginatedResult(response) as PaginatedResult<BrowsePerson[]>;
      })
    );
  }

  downloadCover(personId: number) {
    return this.httpClient.post<string>(this.baseUrl + 'person/fetch-cover?personId=' + personId, {}, TextResonse);
  }

  searchAuthorMetadata(query: string) {
    const params = new HttpParams().set('query', query);
    return this.httpClient.get<AuthorMetadataCandidate[]>(this.baseUrl + 'person/metadata-search', {params});
  }

  applyAuthorMetadata(personId: number, candidate: AuthorMetadataCandidate, overwriteExisting = false) {
    return this.httpClient.post<Person>(this.baseUrl + 'person/metadata-apply', {
      personId,
      providerKey: candidate.providerKey,
      externalId: candidate.externalId,
      overwriteExisting
    });
  }

  refreshAllAuthorMetadata() {
    return this.httpClient.post<void>(this.baseUrl + 'person/metadata-refresh-all', {});
  }

  isValidAlias(personId: number, alias: string, name: string) {
    const req = {personId, name, alias}
    return this.httpClient.post<boolean>(this.baseUrl + `person/valid-alias`, req, TextResonse).pipe(
      map(valid => valid + '' === 'true')
    );
  }

  mergePerson(destId: number, srcId: number) {
    return this.httpClient.post<Person>(this.baseUrl + 'person/merge', {destId, srcId});
  }

}

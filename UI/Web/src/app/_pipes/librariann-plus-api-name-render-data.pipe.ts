import {inject, Pipe, PipeTransform} from '@angular/core';
import {LibrariannPlusApiName} from "../_models/librariannplus/librariann-plus-api-name.enum";
import {TranslocoService} from "@jsverse/transloco";

@Pipe({
  name: 'librariannPlusApiNameRenderData',
})
export class LibrariannPlusApiNameRenderDataPipe implements PipeTransform {

  private readonly transloco = inject(TranslocoService);

  transform(value: LibrariannPlusApiName): {title: string, description: string, icon: string} {
    switch (value) {
      case LibrariannPlusApiName.CoverRequests:
        return {title: this.t('cover-requests-title'), description: this.t('cover-requests-description'), icon: 'fa-solid fa-image'};
      case LibrariannPlusApiName.MetadataSync:
        return {title: this.t('metadata-sync-title'), description: this.t('metadata-sync-description'), icon: 'fa-solid fa-database'};
      case LibrariannPlusApiName.SeriesMatched:
        return {title: this.t('series-matched-title'), description: this.t('series-matched-description'), icon: 'fa-solid fa-magnifying-glass'};
      case LibrariannPlusApiName.Scrobbles:
        return {title: this.t('scrobbles-title'), description: this.t('scrobbles-description'), icon: 'fa-solid fa-paper-plane'};
      case LibrariannPlusApiName.MalStackImport:
        return {title: this.t('mal-stack-import-title'), description: this.t('mal-stack-import-description'), icon: 'fa-solid fa-layer-group'};
      case LibrariannPlusApiName.WantToRead:
        return {title: this.t('want-to-read-title'), description: this.t('want-to-read-description'), icon: 'fa-solid fa-bookmark'};
      case LibrariannPlusApiName.Recommendations:
        return {title: this.t('recommendations-title'), description: this.t('recommendations-description'), icon: 'fa-solid fa-wand-magic-sparkles'};
      case LibrariannPlusApiName.Reviews:
        return {title: this.t('reviews-title'), description: this.t('reviews-description'), icon: 'fa-solid fa-pen-fancy'};
    }
  }

  private t(key: string) {
    return this.transloco.translate('librariann-plus-api-name-title-desc-pipe.' + key);
  }

}

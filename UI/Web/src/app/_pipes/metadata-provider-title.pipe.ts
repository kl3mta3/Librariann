import {Pipe, PipeTransform} from '@angular/core';
import {MetadataProvider} from "../_models/librariannplus/metadata-provider.enum";
import {translate} from "@jsverse/transloco";

@Pipe({
  name: 'metadataProviderTitle',
})
export class MetadataProviderTitlePipe implements PipeTransform {

  // We need to use the static method to avoid code duplication as this pipe is constructed in LibrariannPlusEventDescriptionPipe
  transform(value: MetadataProvider | null): string {
    switch (value) {
      case MetadataProvider.Hardcover:
        return translate('metadata-provider-title-pipe.hardcover');
      case MetadataProvider.Mangabaka:
        return translate('metadata-provider-title-pipe.mangabaka');
      case MetadataProvider.ComicBookRoundup:
        return translate('metadata-provider-title-pipe.cbr');
      default:
        return '';
    }
  }

}

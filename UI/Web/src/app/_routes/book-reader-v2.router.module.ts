import {Routes} from '@angular/router';
import {FoliateReaderPocComponent} from '../book-reader-v2/foliate-reader-poc.component';
import {readingProfileResolver} from '../_resolvers/reading-profile.resolver';

// foliate-js based reader - the app's only book reader now (see the migration plan). The old column-pagination
// reader and its `book-reader.router.module.ts` route were removed once this reached full feature parity.
export const routes: Routes = [
  {
    path: ':chapterId',
    component: FoliateReaderPocComponent,
    resolve: {
      readingProfile: readingProfileResolver
    }
  }
];

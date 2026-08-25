import {Routes} from '@angular/router';
import {AudiobookReaderComponent} from '../audiobook-reader/_components/audiobook-reader/audiobook-reader.component';
import {readingProfileResolver} from "../_resolvers/reading-profile.resolver";

export const routes: Routes = [
  {
      path: ':chapterId',
      component: AudiobookReaderComponent,
      resolve: {
        readingProfile: readingProfileResolver
      }
  }
];

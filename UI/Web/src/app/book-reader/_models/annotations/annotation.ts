import {AgeRating} from "../../../_models/metadata/age-rating";

export interface Annotation {
  id: number;
  xPath: string;
  endingXPath: string | null;
  /**
   * EPUB CFI position, used by the foliate-js reader. Null until lazily backfilled from xPath/endingXPath/
   * selectedText the first time this annotation's book is opened in that reader - see the migration plan.
   */
  cfi: string | null;
  selectedText: string | null;
  comment: string;
  commentHtml: string;
  commentPlainText: string;
  containsSpoiler: boolean;
  pageNumber: number;
  selectedSlotIndex: number;
  chapterTitle: string | null;
  highlightCount: number;
  likes: number[];
  ownerUserId: number;
  ownerUsername: string;
  createdUtc: string;
  lastModifiedUtc: string;
  /**
   * A calculated selection of the surrounding text. This does not update after creation.
   */
  context: string | null;
  chapterId: number;
  libraryId: number;
  volumeId: number;
  seriesId: number;

  seriesName: string;
  libraryName: string;
  ageRating: AgeRating;
}

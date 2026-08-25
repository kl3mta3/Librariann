export enum LibrariannMediaType {
  Book = 1,
  Comic = 2,
  Manga = 3,
}

export interface MetadataLookupRequest {
  mediaType: LibrariannMediaType;
  title: string;
  author: string;
  series: string;
  isbn: string;
  language: string;
  publicationYear?: number;
  includeAdult?: boolean;
  identifiers: Record<string, string>;
}

export interface NormalizedMetadataCandidate {
  providerKey: string;
  providerName: string;
  externalId: string;
  mediaType: LibrariannMediaType;
  isAdult: boolean;
  title: string;
  alternateTitles: string[];
  authors: string[];
  series: string;
  publicationYear?: number;
  languages: string[];
  isbns: string[];
  publishers: string[];
  genres: string[];
  description: string;
  coverUri?: string;
  detailsUri?: string;
  identifiers: Record<string, string>;
}

export interface MetadataMatchDecision {
  candidate: NormalizedMetadataCandidate;
  score: number;
  matchReasons: string[];
  applyToken: string;
}

export interface MetadataLookupResponse {
  results: MetadataMatchDecision[];
  providerFailures: {providerKey: string; providerName: string; message: string}[];
}

export enum MetadataFieldKey {
  Description = 3,
  Cover = 4,
  Authors = 5,
  Publisher = 10,
  PublicationDate = 11,
  Language = 12,
  Genres = 13,
  WebLinks = 16,
}

export interface ApplyMetadataResponse {
  seriesId: number;
  providerKey: string;
  providerItemId: string;
  fields: {field: MetadataFieldKey; applied: boolean; reason: string}[];
}

import {AcquisitionMediaFormat, InteractiveSearchResponse} from '../acquisition/integration-provider';
import {LibrariannMediaType, MetadataLookupResponse} from '../metadata/librariann-metadata';
import {SearchResultGroup} from './search-result-group';

export interface UnifiedDiscoveryRequest {
  query: string;
  author: string;
  isbn: string;
  language: string;
  mediaType: LibrariannMediaType;
  qualityProfileId?: number;
  ownedFormat?: AcquisitionMediaFormat;
  includeAdult?: boolean;
}

export interface UnifiedDiscoveryResponse {
  library: SearchResultGroup;
  externalMetadata?: MetadataLookupResponse;
  releases?: InteractiveSearchResponse;
  access: {
    canSearchLibrary: boolean;
    canSearchExternalMetadata: boolean;
    canSearchReleases: boolean;
    restrictedByContentPolicy: boolean;
  };
  qualityProfileId?: number;
}

export interface AuthorMetadataCandidate {
  providerKey: string;
  providerName: string;
  externalId: string;
  name: string;
  aliases: string[];
  birthDate: string;
  deathDate: string;
  topWork: string;
  workCount: number;
  portraitUri?: string;
  detailsUri?: string;
  matchScore: number;
  matchReasons: string[];
}


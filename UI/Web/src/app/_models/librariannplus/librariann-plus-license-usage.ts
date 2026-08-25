import {LibrariannPlusApiName} from "./librariann-plus-api-name.enum";

export interface LibrariannPlusLicenseUsage {
  generatedAtUtc: string;
  stats: ApiUsage[];
}

export interface ApiUsage {
  apiName: LibrariannPlusApiName;
  lifetimeCount: number;
  last30DaysCount: number;
  dailyBuckets: DailyBucket[];
}

export interface DailyBucket {
  /** DateOnly **/
  date: string;
  count: number;
}

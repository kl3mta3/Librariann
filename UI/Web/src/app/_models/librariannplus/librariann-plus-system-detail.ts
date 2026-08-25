import {ScrobbleProvider} from "../../_services/scrobbling.service";

export interface LibrariannPlusSystemDetail {
  provider: ScrobbleProvider;
  validUntilUtc: string | null;
  userInfo: LibrariannPlusUserInfo | null;
}

export interface LibrariannPlusUserInfo {
  username: string;
}

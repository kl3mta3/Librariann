import {LibrariannPlusRegistrationErrorCode} from "./librariann-plus-registration-error-code";

export interface LibrariannPlusRegisterResult {
  success: boolean;
  errorCode?: LibrariannPlusRegistrationErrorCode;
  isSubscriptionActive: boolean;
}

import {LibrariannPlusBillingInterval} from "./license-info";

export interface LibrariannPlusProductInfo {
  productName?: string;
  priceAmount: number;
  priceCurrency: string;
  billingInterval: LibrariannPlusBillingInterval;
}

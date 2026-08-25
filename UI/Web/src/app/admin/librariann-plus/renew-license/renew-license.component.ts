import {ChangeDetectionStrategy, Component, computed, inject, output, signal} from '@angular/core';
import {ToastrService} from 'ngx-toastr';
import {TranslocoDirective, TranslocoService} from '@jsverse/transloco';
import {LicenseService} from '../../../_services/license.service';
import {LibrariannPlusBillingInterval} from '../../../_models/librariannplus/license-info';
import {LibrariannPlusProductInfo} from '../../../_models/librariannplus/librariann-plus-product-info';
import {LibrariannPlusBillingIntervalPipe} from '../../../_pipes/librariann-plus-billing-interval.pipe';
import {ManageLicenseModalScreen} from '../_modals/manage-license-modal/manage-license-modal-screen';

@Component({
  selector: 'app-renew-license',
  imports: [TranslocoDirective, LibrariannPlusBillingIntervalPipe],
  templateUrl: './renew-license.component.html',
  styleUrl: './renew-license.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RenewLicenseComponent implements ManageLicenseModalScreen {

  private readonly licenseService = inject(LicenseService);
  private readonly toastr = inject(ToastrService);
  private readonly translocoService = inject(TranslocoService);

  readonly back = output<void>();
  readonly dismiss = output<void>();
  /** Navigate to the change-license-email screen. */
  readonly changeEmail = output<void>();

  protected readonly licenseInfo = this.licenseService.licenseInfo;
  protected readonly products = signal<LibrariannPlusProductInfo[]>([]);
  protected readonly selectedInterval = signal<LibrariannPlusBillingInterval>(LibrariannPlusBillingInterval.Month);
  protected readonly isSending = signal<boolean>(false);
  /** Stripe Checkout (Pay Now) URL returned after a successful renew request. */
  protected readonly checkoutUrl = signal<string | null>(null);

  protected readonly monthlyProduct = computed((): LibrariannPlusProductInfo | undefined =>
    this.products().find(p => p.billingInterval === LibrariannPlusBillingInterval.Month));
  protected readonly yearlyProduct = computed((): LibrariannPlusProductInfo | undefined =>
    this.products().find(p => p.billingInterval === LibrariannPlusBillingInterval.Year));

  protected readonly selectedProduct = computed((): LibrariannPlusProductInfo | undefined =>
    this.selectedInterval() === LibrariannPlusBillingInterval.Year ? this.yearlyProduct() : this.monthlyProduct());

  protected readonly LibrariannPlusBillingInterval = LibrariannPlusBillingInterval;

  constructor() {
    this.licenseService.getProducts().subscribe(products => this.products.set(products));
  }

  formattedPrice(product: LibrariannPlusProductInfo | undefined): string | null {
    if (!product || product.priceAmount == null || !product.priceCurrency) return null;

    return new Intl.NumberFormat(undefined, {
      style: 'currency',
      currency: product.priceCurrency.toUpperCase(),
    }).format(product.priceAmount / 100);
  }

  selectPeriod(interval: LibrariannPlusBillingInterval) {
    this.selectedInterval.set(interval);
  }

  sendLink() {
    const email = this.licenseInfo()?.registeredEmail;
    if (!email || !this.selectedProduct()) return;

    this.isSending.set(true);
    this.licenseService.renewLicense(email, this.selectedInterval())
      .subscribe({
        next: (checkoutUrl) => {
          this.isSending.set(false);
          this.checkoutUrl.set(checkoutUrl);
        },
        error: () => {
          this.toastr.error(this.translocoService.translate('renew-license.link-sent-error'));
          this.isSending.set(false);
        }
      });
  }
}

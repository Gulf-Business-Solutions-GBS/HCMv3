import { Component, OnInit, inject, signal } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { AppComponentBase } from '@shared/common/app-component-base';
import { ScriptLoaderService } from '@shared/utils/script-loader.service';
import { accountModuleAnimation } from '@shared/animations/routerTransition';

import {
    PayPalPaymentServiceProxy,
    SubscriptionPaymentDto,
    PaymentServiceProxy,
    PayPalConfigurationDto,
} from '@shared/service-proxies/service-proxies';
import { DecimalPipe } from '@angular/common';
import { BusyIfDirective } from '../../../shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'paypal-pre-payment-component',
    templateUrl: './paypal-pre-payment.component.html',
    animations: [accountModuleAnimation()],
    imports: [BusyIfDirective, DecimalPipe, LocalizePipe],
})
export class PayPalPrePaymentComponent extends AppComponentBase implements OnInit {
    private _activatedRoute = inject(ActivatedRoute);
    private _payPalPaymentAppService = inject(PayPalPaymentServiceProxy);
    private _paymentAppService = inject(PaymentServiceProxy);
    private _router = inject(Router);

    readonly config = signal<PayPalConfigurationDto | null>(null);
    readonly payment = signal<SubscriptionPaymentDto>(new SubscriptionPaymentDto());

    readonly paypalIsLoading = signal(true);
    paymentId;
    successCallbackUrl;
    errorCallbackUrl;

    ngOnInit(): void {
        this.setTenantIdCookieIfNeeded();

        this.paymentId = this._activatedRoute.snapshot.queryParams['paymentId'];

        this._payPalPaymentAppService.getConfiguration().subscribe((config: PayPalConfigurationDto) => {
            this.config.set(config);

            let disabledFundings = this.GetDisabledFundingsQueryString(config);
            new ScriptLoaderService()
                .load(
                    'https://www.paypal.com/sdk/js?client-id=' +
                        config.clientId +
                        '&currency=' +
                        this.appSession.application.currency +
                        disabledFundings
                )
                .then(() => {
                    this._paymentAppService.getPayment(this.paymentId).subscribe((result: SubscriptionPaymentDto) => {
                        this.payment.set(result);
                        this.successCallbackUrl = result.successUrl;
                        this.errorCallbackUrl = result.errorUrl;

                        this.paypalIsLoading.set(false);
                        this.preparePaypalButton();
                    });
                });
        });
    }

    preparePaypalButton(): void {
        const self = this;
        (<any>window).paypal
            .Buttons({
                createOrder: function (data, actions) {
                    return actions.order.create({
                        purchase_units: [
                            {
                                amount: {
                                    value: self.payment().totalAmount,
                                    currency_code: self.appSession.application.currency,
                                },
                            },
                        ],
                    });
                },
                onApprove: function (data, actions) {
                    self._payPalPaymentAppService.confirmPayment(self.paymentId, data.orderID).subscribe(() => {
                        if (self.successCallbackUrl) {
                            self._router.navigate([self.successCallbackUrl], {
                                queryParams: {
                                    paymentId: self.paymentId,
                                },
                            });
                        }
                    });
                },
            })
            .render('#paypal-button');
    }

    GetDisabledFundingsQueryString(config: PayPalConfigurationDto): string {
        if (!config.disabledFundings || config.disabledFundings.length <= 0) {
            return '';
        }

        return '&disable-funding=' + config.disabledFundings.join();
    }

    setTenantIdCookieIfNeeded(): void {
        if (!this._activatedRoute.snapshot.queryParams['tenantId']) {
            return;
        }

        let tenantId = parseInt(this._activatedRoute.snapshot.queryParams['tenantId']);
        abp.multiTenancy.setTenantIdCookie(tenantId);
    }
}

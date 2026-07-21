import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { AppComponentBase } from '@shared/common/app-component-base';
import { ScriptLoaderService } from '@shared/utils/script-loader.service';
import { accountModuleAnimation } from '@shared/animations/routerTransition';

import {
    StripePaymentServiceProxy,
    PaymentServiceProxy,
    SubscriptionPaymentDto,
    StripeConfigurationDto,
    SubscriptionPaymentGatewayType,
    SubscriptionStartType,
    StripeCreatePaymentSessionInput,
} from '@shared/service-proxies/service-proxies';
import { AppConsts } from '@shared/AppConsts';
import { DecimalPipe } from '@angular/common';
import { BusyIfDirective } from '../../../shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'stripe-pre-payment-component',
    templateUrl: './stripe-pre-payment.component.html',
    animations: [accountModuleAnimation()],
    imports: [BusyIfDirective, DecimalPipe, LocalizePipe],
})
export class StripePrePaymentComponent extends AppComponentBase implements OnInit {
    private _activatedRoute = inject(ActivatedRoute);
    private _stripePaymentAppService = inject(StripePaymentServiceProxy);
    private _paymentAppService = inject(PaymentServiceProxy);

    amount = 0;
    description = '';

    subscriptionPayment: SubscriptionPaymentDto;
    readonly payment = signal<SubscriptionPaymentDto>(new SubscriptionPaymentDto());
    readonly stripeIsLoading = signal(true);

    paymentId: number;

    ngOnInit(): void {
        this.spinnerService.show();
        this.paymentId = this._activatedRoute.snapshot.queryParams['paymentId'];
        this.stripeIsLoading.set(true);

        new ScriptLoaderService()
            .load('https://js.stripe.com/v3')
            .then(() => {
                this._stripePaymentAppService.getConfiguration().subscribe(
                    (config: StripeConfigurationDto) => {
                        this._stripePaymentAppService
                            .createPaymentSession(
                                new StripeCreatePaymentSessionInput({
                                    paymentId: this.paymentId,
                                    successUrl: AppConsts.appBaseUrl + '/account/stripe-post-payment',
                                    cancelUrl: AppConsts.appBaseUrl + '/account/stripe-cancel-payment',
                                })
                            )
                            .subscribe(
                                (sessionId) => {
                                    this._paymentAppService.getPayment(this.paymentId).subscribe(
                                        (result: SubscriptionPaymentDto) => {
                                            this.payment.set(result);

                                            this.spinnerService.hide();

                                            let stripe = (<any>window).Stripe(config.publishableKey);
                                            let checkoutButton = document.getElementById('stripe-checkout');
                                            checkoutButton.addEventListener('click', function () {
                                                stripe.redirectToCheckout({ sessionId: sessionId });
                                            });

                                            this.stripeIsLoading.set(false);
                                        },
                                        (err) => {
                                            this.spinnerService.hide();
                                        }
                                    );
                                },
                                (err) => {
                                    this.spinnerService.hide();
                                }
                            );
                    },
                    (err) => {
                        this.spinnerService.hide();
                    }
                );
            })
            .catch((err) => {
                this.spinnerService.hide();
            });
    }
}

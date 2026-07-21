import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { accountModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    PaymentServiceProxy,
    PaymentGatewayModel,
    SubscriptionPaymentDto,
    UpdatePaymentDto,
} from '@shared/service-proxies/service-proxies';
import { PaymentHelperService } from './payment-helper.service';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    templateUrl: './gateway-selection.component.html',
    animations: [accountModuleAnimation()],
    imports: [FormsModule, DecimalPipe, LocalizePipe],
})
export class GatewaySelectionComponent extends AppComponentBase implements OnInit {
    private _activatedRoute = inject(ActivatedRoute);
    private _router = inject(Router);
    private _paymnetHelperService = inject(PaymentHelperService);
    private _paymentAppService = inject(PaymentServiceProxy);

    readonly payment = signal<SubscriptionPaymentDto>(new SubscriptionPaymentDto());
    tenantId: number = abp.session.tenantId;
    paymentId: number;

    readonly paymentGateways = signal<PaymentGatewayModel[]>([]);
    readonly supportsRecurringPayments = signal(false);
    readonly recurringPaymentEnabled = signal(false);

    ngOnInit(): void {
        this.paymentId = parseInt(this._activatedRoute.snapshot.queryParams['paymentId']);
        this._paymentAppService.getPayment(this.paymentId).subscribe((result: SubscriptionPaymentDto) => {
            this.payment.set(result);
            abp.multiTenancy.setTenantIdCookie(result.tenantId);
        });

        this._paymentAppService.getActiveGateways(undefined).subscribe((result: PaymentGatewayModel[]) => {
            this.paymentGateways.set(result);
            this.supportsRecurringPayments.set(result.some((pg) => pg.supportsRecurringPayments));
        });
    }

    checkout(gatewayType) {
        let input = {} as UpdatePaymentDto;
        input.paymentId = this.paymentId;
        input.isRecurring = this.recurringPaymentEnabled();
        input.gateway = gatewayType.toString();

        this._paymentAppService.updatePayment(input).subscribe(() => {
            this._router.navigate(
                ['account/' + this.getPaymentGatewayType(gatewayType).toLocaleLowerCase() + '-pre-payment'],
                {
                    queryParams: {
                        paymentId: this.paymentId,
                    },
                }
            );
        });
    }

    getPaymentGatewayType(gatewayType): string {
        return this._paymnetHelperService.getPaymentGatewayType(gatewayType);
    }
}

import { Component, inject, viewChild, signal } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { PaymentServiceProxy, SubscriptionPaymentProductDto } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { PermissionTreeComponent } from '../shared/permission-tree.component';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';
import { DecimalPipe } from '@angular/common';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'showDetailModal',
    templateUrl: './show-detail-modal.component.html',
    imports: [AppBsModalDirective, DecimalPipe, LocalizePipe],
})
export class ShowDetailModalComponent extends AppComponentBase {
    private _paymentService = inject(PaymentServiceProxy);

    readonly modal = viewChild<ModalDirective>('showDetailModal');
    readonly permissionTree = viewChild<PermissionTreeComponent>('permissionTree');

    readonly products = signal<SubscriptionPaymentProductDto[]>([]);
    extraProperties: string;

    resettingPermissions = false;

    show(paymentId: number): void {
        this._paymentService.getPayment(paymentId).subscribe((result) => {
            this.products.set(result.subscriptionPaymentProducts);
            this.modal().show();
        });
    }

    close(): void {
        this.modal().hide();
    }
}

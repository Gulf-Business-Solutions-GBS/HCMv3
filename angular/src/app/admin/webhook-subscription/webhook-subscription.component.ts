import { Component, inject, viewChild, signal } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { WebhookSubscriptionServiceProxy } from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs/operators';
import { CreateOrEditWebhookSubscriptionModalComponent } from './create-or-edit-webhook-subscription-modal.component';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { Router } from '@angular/router';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';

import { BusyIfDirective } from '../../../shared/utils/busy-if.directive';
import { TableModule } from 'primeng/table';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';

@Component({
    templateUrl: './webhook-subscription.component.html',
    styleUrls: ['./webhook-subscription.component.css'],
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        TableModule,
        CreateOrEditWebhookSubscriptionModalComponent,
        LocalizePipe,
        PermissionPipe,
    ],
})
export class WebhookSubscriptionComponent extends AppComponentBase {
    private _webhookSubscriptionService = inject(WebhookSubscriptionServiceProxy);
    private _router = inject(Router);

    readonly createOrEditWebhookSubscriptionModal = viewChild<CreateOrEditWebhookSubscriptionModalComponent>(
        'createOrEditWebhookSubscriptionModal'
    );

    getSubscriptions(event?: any): void {
        this.primengTableHelper.showLoadingIndicator();

        this._webhookSubscriptionService
            .getAllSubscriptions()
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.primengTableHelper.totalRecordsCount.set(result.items.length);
                this.primengTableHelper.records.set(result.items);
                this.primengTableHelper.hideLoadingIndicator();
            });
    }

    createSubscription(): void {
        this.createOrEditWebhookSubscriptionModal().show();
    }

    goToSubscriptionDetail(subscriptionId: string): void {
        this._router.navigate(['app/admin/webhook-subscriptions-detail'], {
            queryParams: {
                id: subscriptionId,
            },
        });
    }
}

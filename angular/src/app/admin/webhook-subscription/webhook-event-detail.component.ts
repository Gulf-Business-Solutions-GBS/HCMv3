import { Component, OnInit, inject, viewChild, signal } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    WebhookSendAttemptServiceProxy,
    WebhookSubscription,
    WebhookEventServiceProxy,
    WebhookEvent,
} from '@shared/service-proxies/service-proxies';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { Router, ActivatedRoute } from '@angular/router';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '../../../shared/utils/busy-if.directive';
import { TableModule } from 'primeng/table';
import { TieredMenu } from 'primeng/tieredmenu';
import { MenuItem } from 'primeng/api';
import { CreateOrEditWebhookSubscriptionModalComponent } from './create-or-edit-webhook-subscription-modal.component';
import { LuxonFormatPipe } from '../../../shared/utils/luxon-format.pipe';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    templateUrl: './webhook-event-detail.component.html',
    styleUrls: ['./webhook-event-detail.component.css'],
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        TableModule,
        TieredMenu,
        CreateOrEditWebhookSubscriptionModalComponent,
        ModalDirective,
        LuxonFormatPipe,
        LocalizePipe,
    ],
})
export class WebhookEventDetailComponent extends AppComponentBase implements OnInit {
    private _webhookEventService = inject(WebhookEventServiceProxy);
    private _webhookSendAttemptService = inject(WebhookSendAttemptServiceProxy);
    private _router = inject(Router);
    private _activatedRoute = inject(ActivatedRoute);

    readonly detailModal = viewChild<ModalDirective>('detailModal');
    subscription: WebhookSubscription;
    readonly loading = signal(true);
    webhookEventId: string;
    webhookEvent: WebhookEvent;
    readonly activeSendAttemptMenuItems = signal<MenuItem[]>([]);

    maxDataLength = 300;
    listMaxResponseLength = 100;
    detailModalText = '';

    ngOnInit() {
        this.webhookEventId = this._activatedRoute.snapshot.queryParams['id'];
        this.getDetail();
    }

    getSendAttempts(event?: any): void {
        this.primengTableHelper.showLoadingIndicator();

        this._webhookSendAttemptService.getAllSendAttemptsOfWebhookEvent(this.webhookEventId).subscribe((result) => {
            this.primengTableHelper.totalRecordsCount.set(result.items.length);
            this.primengTableHelper.records.set(result.items);
            this.primengTableHelper.hideLoadingIndicator();
        });
    }

    getDetail(): void {
        this._webhookEventService.get(this.webhookEventId).subscribe((webhookEvent) => {
            this.webhookEvent = webhookEvent;
            this.loading.set(false);
        });
    }

    goToWebhookSubscriptionDetail(subscriptionId: string): void {
        this._router.navigate(['app/admin/webhook-subscriptions-detail'], {
            queryParams: {
                id: subscriptionId,
            },
        });
    }

    resend(id: string): void {
        this.message.confirm(
            this.l('WebhookEventWillBeSendWithSameParameters'),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (isConfirmed) {
                    this.showMainSpinner();
                    this._webhookSendAttemptService.resend(id).subscribe(
                        () => {
                            abp.notify.success(this.l('WebhookSendAttemptInQueue'));
                            this.hideMainSpinner();
                        },
                        () => {
                            this.hideMainSpinner();
                        }
                    );
                }
            }
        );
    }

    showSendAttemptMenu(event: Event, record: any, menu: any): void {
        this.activeSendAttemptMenuItems.set(this.getSendAttemptMenuItems(record));
        menu.toggle(event);
    }

    getSendAttemptMenuItems(record: any): MenuItem[] {
        const items: MenuItem[] = [];

        if (this.permission.isGranted('Pages.Administration.Webhook.ResendWebhook')) {
            items.push({
                label: this.l('Resend'),
                icon: 'pi pi-replay',
                command: () => this.resend(record.id),
            });
        }

        items.push({
            label: this.l('GoToSubscription'),
            icon: 'pi pi-arrow-right',
            command: () => this.goToWebhookSubscriptionDetail(record.webhookSubscriptionId),
        });

        return items;
    }

    showDetailModal(text): void {
        this.detailModalText = text;
        this.detailModal().show();
    }

    showMoreData(): void {
        document.getElementById('dataDots').classList.add('d-none');
        document.getElementById('dataShowMoreBtn').classList.add('d-none');
        document.getElementById('dataMore').classList.remove('d-none');
    }
}

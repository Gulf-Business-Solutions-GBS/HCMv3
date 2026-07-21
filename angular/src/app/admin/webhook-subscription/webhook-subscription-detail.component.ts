import { Component, OnInit, inject, viewChild, signal } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    WebhookSubscriptionServiceProxy,
    WebhookSendAttemptServiceProxy,
    WebhookSubscription,
    ActivateWebhookSubscriptionInput,
} from '@shared/service-proxies/service-proxies';
import { CreateOrEditWebhookSubscriptionModalComponent } from './create-or-edit-webhook-subscription-modal.component';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { Router, ActivatedRoute } from '@angular/router';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { LazyLoadEvent } from 'primeng/api';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { BreadcrumbItem } from '@app/shared/common/sub-header/sub-header.component';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';
import { TieredMenu } from 'primeng/tieredmenu';
import { MenuItem } from 'primeng/api';
import { BusyIfDirective } from '../../../shared/utils/busy-if.directive';
import { TableModule } from 'primeng/table';
import { LuxonFormatPipe } from '../../../shared/utils/luxon-format.pipe';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';

@Component({
    templateUrl: './webhook-subscription-detail.component.html',
    styleUrls: ['./webhook-subscription-detail.component.css'],
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        TableModule,
        TieredMenu,
        PaginatorModule,
        CreateOrEditWebhookSubscriptionModalComponent,
        ModalDirective,
        LuxonFormatPipe,
        LocalizePipe,
        PermissionPipe,
    ],
})
export class WebhookSubscriptionDetailComponent extends AppComponentBase implements OnInit {
    private readonly _webhookSubscriptionService = inject(WebhookSubscriptionServiceProxy);
    private readonly _webhookSendAttemptService = inject(WebhookSendAttemptServiceProxy);
    private readonly _router = inject(Router);
    private readonly _activatedRoute = inject(ActivatedRoute);

    readonly createOrEditWebhookSubscriptionModal = viewChild<CreateOrEditWebhookSubscriptionModalComponent>(
        'createOrEditWebhookSubscriptionModal'
    );
    readonly paginator = viewChild<Paginator>('paginator');
    readonly detailModal = viewChild<ModalDirective>('detailModal');

    readonly subscriptionId = signal('');
    readonly subscription = signal<WebhookSubscription | null>(null);
    readonly loading = signal(true);
    readonly isSecretBlurActive = signal(true);
    readonly detailModalText = signal('');
    readonly activeHeaderMenuItems = signal<MenuItem[]>([]);
    readonly activeSendAttemptMenuItems = signal<MenuItem[]>([]);

    readonly objectKeys = Object.keys;
    readonly listMaxDataLength = 100;

    readonly breadcrumbs = signal<BreadcrumbItem[]>([
        new BreadcrumbItem(this.l('WebhookSubscriptions'), '/app/admin/webhook-subscriptions'),
        new BreadcrumbItem(this.l('WebhookSubscriptionDetail')),
    ]);

    ngOnInit() {
        this.subscriptionId.set(this._activatedRoute.snapshot.queryParams['id']);
        this.getDetail();
    }

    getSendAttempts(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);

            if (this.primengTableHelper.records() && this.primengTableHelper.records().length > 0) {
                return;
            }
        }

        this.primengTableHelper.showLoadingIndicator();

        this._webhookSendAttemptService
            .getAllSendAttempts(
                this.subscriptionId(),
                this.primengTableHelper.getMaxResultCount(this.paginator(), event),
                this.primengTableHelper.getSkipCount(this.paginator(), event)
            )
            .subscribe((result) => {
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(result.items);
                this.primengTableHelper.hideLoadingIndicator();
            });
    }

    editSubscription(): void {
        this.createOrEditWebhookSubscriptionModal().show();
    }

    getDetail(): void {
        this._webhookSubscriptionService.getSubscription(this.subscriptionId()).subscribe((subscription) => {
            this.subscription.set(subscription);
            this.loading.set(false);
        });
    }

    toggleActivity(): void {
        const currentSubscription = this.subscription();
        if (!currentSubscription) {
            return;
        }

        let message = currentSubscription.isActive
            ? this.l('DeactivateSubscriptionWarningMessage')
            : this.l('ActivateSubscriptionWarningMessage');

        this.message.confirm(message, this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                let input = new ActivateWebhookSubscriptionInput();
                input.subscriptionId = currentSubscription.id;
                input.isActive = !currentSubscription.isActive;

                this._webhookSubscriptionService.activateWebhookSubscription(input).subscribe(() => {
                    this.subscription.update((sub) => {
                        if (sub) {
                            sub.isActive = !sub.isActive;
                        }
                        return sub;
                    });
                });
            }
        });
    }

    viewSecret(): void {
        this.isSecretBlurActive.set(false);
    }

    showHeaderMenu(event: Event, menu: any): void {
        this.activeHeaderMenuItems.set(this.getHeaderMenuItems());
        menu.toggle(event);
    }

    getHeaderMenuItems(): MenuItem[] {
        const items: MenuItem[] = [];
        const currentSubscription = this.subscription();

        if (this.permission.isGranted('Pages.Administration.WebhookSubscription.Edit') && currentSubscription) {
            items.push({
                label: this.l('EditWebhookSubscription'),
                icon: 'pi pi-pencil',
                command: () => this.createOrEditWebhookSubscriptionModal().show(currentSubscription.id),
            });
        }

        if (this.permission.isGranted('Pages.Administration.WebhookSubscription.ChangeActivity')) {
            const isActive = currentSubscription?.isActive;
            items.push({
                label: isActive ? this.l('Disable') : this.l('Enable'),
                icon: isActive ? 'pi pi-ban' : 'pi pi-check',
                command: () => this.toggleActivity(),
            });
        }

        return items;
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

        if (this.permission.isGranted('Pages.Administration.WebhookSubscription.ChangeActivity')) {
            items.push({
                label: this.l('ViewWebhookEvent'),
                icon: 'pi pi-eye',
                command: () => this.goToWebhookDetail(record.webhookEventId),
            });
        }

        return items;
    }

    goToWebhookDetail(webhookId: string): void {
        this._router.navigate(['app/admin/webhook-event-detail'], {
            queryParams: {
                id: webhookId,
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
                        (e) => {
                            this.hideMainSpinner();
                        }
                    );
                }
            }
        );
    }

    showDetailModal(text): void {
        this.detailModalText.set(text);
        this.detailModal().show();
    }
}

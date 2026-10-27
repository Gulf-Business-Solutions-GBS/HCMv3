import { Component, OnInit, inject, viewChild, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ApplicationInfoDto,
    InvoiceServiceProxy,
    PaymentServiceProxy,
    SubscriptionServiceProxy,
    TenantLoginInfoDto,
    UserLoginInfoDto,
    CreateInvoiceDto,
    SubscriptionStartType,
    SubscriptionPaymentType,
    TenantRegistrationServiceProxy,
    EditionWithFeaturesDto,
    StartExtendSubscriptionInput,
    StartUpgradeSubscriptionInput,
    PaymentPeriodType,
    StartTrialToBuySubscriptionInput,
} from '@shared/service-proxies/service-proxies';
import { LazyLoadEvent } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';
import { TabsetComponent, TabDirective } from 'ngx-bootstrap/tabs';
import { FormsModule } from '@angular/forms';
import { NgClass, DecimalPipe } from '@angular/common';
import { TieredMenu } from 'primeng/tieredmenu';
import { MenuItem } from 'primeng/api';
import { BusyIfDirective } from '../../../shared/utils/busy-if.directive';
import { ShowDetailModalComponent } from './show-detail-modal.component';
import { LuxonFormatPipe } from '../../../shared/utils/luxon-format.pipe';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    templateUrl: './subscription-management.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        TabsetComponent,
        TabDirective,
        FormsModule,
        NgClass,
        BusyIfDirective,
        TableModule,
        TieredMenu,
        PaginatorModule,
        ShowDetailModalComponent,
        DecimalPipe,
        LuxonFormatPipe,
        LocalizePipe,
    ],
})
export class SubscriptionManagementComponent extends AppComponentBase implements OnInit {
    private _paymentServiceProxy = inject(PaymentServiceProxy);
    private _invoiceServiceProxy = inject(InvoiceServiceProxy);
    private _subscriptionServiceProxy = inject(SubscriptionServiceProxy);
    private _tenantRegistrationAppService = inject(TenantRegistrationServiceProxy);
    private _activatedRoute = inject(ActivatedRoute);
    private _router = inject(Router);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');
    readonly showDetailModal = viewChild<any>('showDetailModal');

    subscriptionStartType: typeof SubscriptionStartType = SubscriptionStartType;
    subscriptionPaymentType: typeof SubscriptionPaymentType = SubscriptionPaymentType;

    readonly loading = signal(true);
    readonly user = signal<UserLoginInfoDto>(new UserLoginInfoDto());
    readonly tenant = signal<TenantLoginInfoDto>(new TenantLoginInfoDto());
    readonly application = signal<ApplicationInfoDto>(new ApplicationInfoDto());

    filterText = '';
    readonly editions = signal<EditionWithFeaturesDto[]>([]);
    readonly activePaymentMenuItems = signal<MenuItem[]>([]);
    readonly activeBuyNowMenuItems = signal<MenuItem[]>([]);
    readonly activeUpgradeMenuItems = signal<MenuItem[]>([]);

    constructor() {
        super();
        this.filterText = this._activatedRoute.snapshot.queryParams['filterText'] || '';
    }

    ngOnInit(): void {
        this.getSettings();
    }

    createOrShowInvoice(paymentId: number, invoiceNo: string): void {
        if (invoiceNo) {
            window.open('/app/admin/invoice/' + paymentId, '_blank');
        } else {
            this._invoiceServiceProxy
                .createInvoice(new CreateInvoiceDto({ subscriptionPaymentId: paymentId }))
                .subscribe(() => {
                    this.getPaymentHistory();
                    window.open('/app/admin/invoice/' + paymentId, '_blank');
                });
        }
    }

    showPaymentMenu(event: Event, record: any, menu: any): void {
        this.activePaymentMenuItems.set(this.getPaymentMenuItems(record));
        menu.toggle(event);
    }

    getPaymentMenuItems(record: any): MenuItem[] {
        const items: MenuItem[] = [];

        items.push({
            label: this.l('Detail'),
            icon: 'pi pi-info-circle',
            command: () => this.showDetailModal().show(record.id),
        });

        if (this.permission.isGranted('Pages.Administration.Users.Edit')) {
            items.push({
                label: this.l('ShowInvoice'),
                icon: 'pi pi-file',
                command: () => this.createOrShowInvoice(record.id, record.invoiceNo),
            });
        }

        return items;
    }

    showBuyNowMenu(event: Event, menu: any): void {
        this.activeBuyNowMenuItems.set(this.getBuyNowMenuItems());
        menu.toggle(event);
    }

    getBuyNowMenuItems(): MenuItem[] {
        return [
            {
                label: this.l('Monthly'),
                icon: 'pi pi-calendar',
                command: () => this.startBuySubscription('Monthly'),
            },
            {
                label: this.l('Annual'),
                icon: 'pi pi-calendar-plus',
                command: () => this.startBuySubscription('Annual'),
            },
        ];
    }

    showUpgradeMenu(event: Event, editionId: number, menu: any): void {
        this.activeUpgradeMenuItems.set(this.getUpgradeMenuItems(editionId));
        menu.toggle(event);
    }

    getUpgradeMenuItems(editionId: number): MenuItem[] {
        return [
            {
                label: this.l('Monthly'),
                icon: 'pi pi-calendar',
                command: () => this.startUpdateSubscription(editionId, 'Monthly'),
            },
            {
                label: this.l('Annual'),
                icon: 'pi pi-calendar-plus',
                command: () => this.startUpdateSubscription(editionId, 'Annual'),
            },
        ];
    }

    getSettings(): void {
        this.loading.set(true);
        this.appSession.init().then(() => {
            this.loading.set(false);
            this.user.set(this.appSession.user);
            this.tenant.set(this.appSession.tenant);
            this.application.set(this.appSession.application);
        });

        this._tenantRegistrationAppService.getEditionsForSelect().subscribe((result) => {
            this.editions.set(result.editionsWithFeatures);
        });
    }

    getPaymentHistory(event?: LazyLoadEvent) {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);

            if (this.primengTableHelper.records() && this.primengTableHelper.records().length > 0) {
                return;
            }
        }

        this.primengTableHelper.showLoadingIndicator();

        this._paymentServiceProxy
            .getPaymentHistory(
                this.primengTableHelper.getSorting(this.dataTable()),
                this.primengTableHelper.getSkipCount(this.paginator(), event),
                this.primengTableHelper.getMaxResultCount(this.paginator(), event)
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(result.items);
                this.primengTableHelper.hideLoadingIndicator();
            });
    }

    disableRecurringPayments() {
        this._subscriptionServiceProxy.disableRecurringPayments().subscribe((result) => {
            this.tenant.update((currentTenant) => {
                const updated = new TenantLoginInfoDto();
                updated.init({
                    ...currentTenant.toJSON(),
                    subscriptionPaymentType: this.subscriptionPaymentType.RecurringManual,
                });
                return updated;
            });
        });
    }

    enableRecurringPayments() {
        this._subscriptionServiceProxy.enableRecurringPayments().subscribe((result) => {
            this.tenant.update((currentTenant) => {
                const updated = new TenantLoginInfoDto();
                updated.init({
                    ...currentTenant.toJSON(),
                    subscriptionPaymentType: this.subscriptionPaymentType.RecurringAutomatic,
                });
                return updated;
            });
        });
    }

    hasRecurringSubscription(): boolean {
        return this.tenant().subscriptionPaymentType !== this.subscriptionPaymentType.Manual;
    }

    startUpdateSubscription(editionId: number, paymentPeriodType?: string): void {
        let input = new StartUpgradeSubscriptionInput();
        input.targetEditionId = editionId;
        input.paymentPeriodType = PaymentPeriodType[paymentPeriodType];
        input.successUrl = abp.appPath + 'account/upgrade-succeed';
        input.errorUrl = abp.appPath + 'account/payment-failed';
        this._subscriptionServiceProxy.startUpgradeSubscription(input).subscribe((result) => {
            if (result.upgraded) {
                this.message.success(this.l('YourAccountIsUpgraded'));
            } else {
                this._router.navigate(['account/gateway-selection'], {
                    queryParams: {
                        paymentId: result.paymentId,
                    },
                });
            }
        });
    }

    startExtendSubscription(): void {
        let input = new StartExtendSubscriptionInput();
        input.successUrl = abp.appPath + 'account/extend-succeed';
        input.errorUrl = abp.appPath + 'account/payment-failed';
        this._subscriptionServiceProxy.startExtendSubscription(input).subscribe((paymentId) => {
            this._router.navigate(['account/gateway-selection'], {
                queryParams: {
                    paymentId: paymentId,
                },
            });
        });
    }

    startBuySubscription(paymentPeriodType?: string): void {
        let input = new StartTrialToBuySubscriptionInput();
        input.paymentPeriodType = PaymentPeriodType[paymentPeriodType];
        input.successUrl = abp.appPath + 'account/buy-succeed';
        input.errorUrl = abp.appPath + 'account/payment-failed';

        this._subscriptionServiceProxy.startTrialToBuySubscription(input).subscribe((paymentId) => {
            this._router.navigate(['account/gateway-selection'], {
                queryParams: {
                    paymentId: paymentId,
                },
            });
        });
    }
}

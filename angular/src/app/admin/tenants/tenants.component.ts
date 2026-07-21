import { Component, OnInit, ViewEncapsulation, inject, viewChild, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ImpersonationService } from '@app/admin/users/impersonation.service';
import { CommonLookupModalComponent } from '@app/shared/common/lookup/common-lookup-modal.component';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CommonLookupServiceProxy,
    EntityDtoOfInt64,
    FindUsersInput,
    FindUsersOutputDto,
    TenantListDto,
    TenantServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { LazyLoadEvent, MenuItem } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { CreateTenantModalComponent } from './create-tenant-modal.component';
import { EditTenantModalComponent } from './edit-tenant-modal.component';
import { TenantFeaturesModalComponent } from './tenant-features-modal.component';
import { filter as _filter } from 'lodash-es';
import { finalize } from 'rxjs/operators';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';
import { FormsModule } from '@angular/forms';
import { EditionComboComponent } from '../shared/edition-combo.component';
import { DatePickerModule } from 'primeng/datepicker';
import { PrimengDateRangePickerLuxonModifierDirective } from '../../../shared/utils/date-time/primeng-date-range-picker-luxon-modifier.directive';
import { BusyIfDirective } from '../../../shared/utils/busy-if.directive';
import { CommonLookupModalComponent as CommonLookupModalComponent_1 } from '../../shared/common/lookup/common-lookup-modal.component';
import { LuxonFormatPipe } from '../../../shared/utils/luxon-format.pipe';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { TieredMenu } from 'primeng/tieredmenu';

@Component({
    templateUrl: './tenants.component.html',
    encapsulation: ViewEncapsulation.None,
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        FormsModule,
        EditionComboComponent,
        DatePickerModule,
        PrimengDateRangePickerLuxonModifierDirective,
        BusyIfDirective,
        TableModule,
        TieredMenu,
        PaginatorModule,
        CreateTenantModalComponent,
        EditTenantModalComponent,
        TenantFeaturesModalComponent,
        CommonLookupModalComponent_1,
        LuxonFormatPipe,
        LocalizePipe,
        PermissionPipe,
    ],
})
export class TenantsComponent extends AppComponentBase implements OnInit {
    private _tenantService = inject(TenantServiceProxy);
    private _activatedRoute = inject(ActivatedRoute);
    private _commonLookupService = inject(CommonLookupServiceProxy);
    private _impersonationService = inject(ImpersonationService);
    private _dateTimeService = inject(DateTimeService);
    private _router = inject(Router);

    readonly impersonateUserLookupModal = viewChild<CommonLookupModalComponent>('impersonateUserLookupModal');
    readonly createTenantModal = viewChild<CreateTenantModalComponent>('createTenantModal');
    readonly editTenantModal = viewChild<EditTenantModalComponent>('editTenantModal');
    readonly tenantFeaturesModal = viewChild<TenantFeaturesModalComponent>('tenantFeaturesModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly subscriptionDateRange = signal<DateTime[]>([
        this._dateTimeService.getStartOfDay().toDateTime(),
        this._dateTimeService.getEndOfDayPlusDays(30).toDateTime(),
    ]);
    readonly creationDateRange = signal<DateTime[]>([
        this._dateTimeService.getStartOfDay().toDateTime(),
        this._dateTimeService.getEndOfDay().toDateTime(),
    ]);

    _entityTypeFullName = 'HCMv3.MultiTenancy.Tenant';
    readonly entityHistoryEnabled = signal<boolean>(false);
    readonly activeTenantMenuItems = signal<MenuItem[]>([]);

    readonly filters = signal<{
        filterText: string;
        creationDateRangeActive: boolean;
        subscriptionEndDateRangeActive: boolean;
        selectedEditionId: number;
    }>({
        filterText: '',
        creationDateRangeActive: false,
        subscriptionEndDateRangeActive: false,
        selectedEditionId: undefined,
    });

    constructor() {
        super();
        this.setFiltersFromRoute();
    }

    showTenantMenu(event: Event, tenant: TenantListDto, menu: any): void {
        this.activeTenantMenuItems.set(this.getMenuItemsForTenant(tenant));
        menu.toggle(event);
    }

    getMenuItemsForTenant(tenant: TenantListDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (this.permission.isGranted('Pages.Tenants.Impersonation')) {
            items.push({
                label: this.l('LoginAsThisTenant'),
                icon: 'pi pi-sign-in',
                disabled: !tenant.isActive,
                command: () => tenant.isActive && this.showUserImpersonateLookUpModal(tenant),
            });
        }

        if (this.permission.isGranted('Pages.Tenants.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.editTenantModal().show(tenant.id),
            });
        }

        if (this.permission.isGranted('Pages.Tenants.ChangeFeatures')) {
            items.push({
                label: this.l('Features'),
                icon: 'pi pi-cog',
                command: () => this.tenantFeaturesModal().show(tenant.id, tenant.name),
            });
        }

        if (this.permission.isGranted('Pages.Tenants.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteTenant(tenant),
            });
        }

        if (this.permission.isGranted('Pages.Administration.Users.Unlock') && tenant.isTenantAdminLocked) {
            items.push({
                label: this.l('Unlock'),
                icon: 'pi pi-unlock',
                command: () => this.unlockUser(tenant),
            });
        }

        if (this.entityHistoryEnabled()) {
            items.push({
                label: this.l('History'),
                icon: 'pi pi-history',
                command: () => this.showHistory(tenant),
            });
        }

        return items;
    }

    setFiltersFromRoute(): void {
        const currentFilters = this.filters();
        const currentSubscriptionDateRange = [...this.subscriptionDateRange()];
        const currentCreationDateRange = [...this.creationDateRange()];

        if (this._activatedRoute.snapshot.queryParams['subscriptionEndDateStart'] != null) {
            currentFilters.subscriptionEndDateRangeActive = true;
            currentSubscriptionDateRange[0] = this._dateTimeService.fromISODateString(
                this._activatedRoute.snapshot.queryParams['subscriptionEndDateStart']
            );
        } else {
            currentSubscriptionDateRange[0] = this._dateTimeService.getStartOfDay().toDateTime();
        }

        if (this._activatedRoute.snapshot.queryParams['subscriptionEndDateEnd'] != null) {
            currentFilters.subscriptionEndDateRangeActive = true;
            currentSubscriptionDateRange[1] = this._dateTimeService.fromISODateString(
                this._activatedRoute.snapshot.queryParams['subscriptionEndDateEnd']
            );
        } else {
            currentSubscriptionDateRange[1] = this._dateTimeService.getEndOfDayPlusDays(30).toDateTime();
        }

        if (this._activatedRoute.snapshot.queryParams['creationDateStart'] != null) {
            currentFilters.creationDateRangeActive = true;
            currentCreationDateRange[0] = this._dateTimeService.fromISODateString(
                this._activatedRoute.snapshot.queryParams['creationDateStart']
            );
        } else {
            currentCreationDateRange[0] = this._dateTimeService.getEndOfDayMinusDays(7).toDateTime();
        }

        if (this._activatedRoute.snapshot.queryParams['creationDateEnd'] != null) {
            currentFilters.creationDateRangeActive = true;
            currentCreationDateRange[1] = this._dateTimeService.fromISODateString(
                this._activatedRoute.snapshot.queryParams['creationDateEnd']
            );
        } else {
            currentCreationDateRange[1] = this._dateTimeService.getEndOfDay().toDateTime();
        }

        if (this._activatedRoute.snapshot.queryParams['editionId'] != null) {
            currentFilters.selectedEditionId = parseInt(this._activatedRoute.snapshot.queryParams['editionId']);
        }

        this.filters.set(currentFilters);
        this.subscriptionDateRange.set(currentSubscriptionDateRange);
        this.creationDateRange.set(currentCreationDateRange);
    }

    ngOnInit(): void {
        this.filters.update((currentFilters) => ({
            ...currentFilters,
            filterText: this._activatedRoute.snapshot.queryParams['filterText'] || '',
        }));

        this.setIsEntityHistoryEnabled();

        this.impersonateUserLookupModal().configure({
            title: this.l('SelectAUser'),
            dataSource: (skipCount: number, maxResultCount: number, filter: string, tenantId?: number) => {
                let input = new FindUsersInput();
                input.filter = filter;
                input.maxResultCount = maxResultCount;
                input.skipCount = skipCount;
                input.tenantId = tenantId;
                return this._commonLookupService.findUsers(input);
            },
        });
    }

    onSubscriptionDateRangeChange(dateRange: DateTime[]): void {
        this.subscriptionDateRange.set(dateRange);
        if (
            dateRange &&
            dateRange.length === 2 &&
            dateRange[0] &&
            dateRange[1] &&
            this.filters().subscriptionEndDateRangeActive
        ) {
            this.getTenants();
        }
    }

    onCreationDateRangeChange(dateRange: DateTime[]): void {
        this.creationDateRange.set(dateRange);
        if (
            dateRange &&
            dateRange.length === 2 &&
            dateRange[0] &&
            dateRange[1] &&
            this.filters().creationDateRangeActive
        ) {
            this.getTenants();
        }
    }

    getTenants(event?: LazyLoadEvent) {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);

            if (this.primengTableHelper.records() && this.primengTableHelper.records().length > 0) {
                return;
            }
        }

        this.primengTableHelper.showLoadingIndicator();

        const currentFilters = this.filters();
        const currentSubscriptionDateRange = this.subscriptionDateRange();
        const currentCreationDateRange = this.creationDateRange();

        this._tenantService
            .getTenants(
                currentFilters.filterText,
                currentFilters.subscriptionEndDateRangeActive ? currentSubscriptionDateRange[0] : undefined,
                currentFilters.subscriptionEndDateRangeActive
                    ? currentSubscriptionDateRange[1].endOf('day')
                    : undefined,
                currentFilters.creationDateRangeActive ? currentCreationDateRange[0] : undefined,
                currentFilters.creationDateRangeActive ? currentCreationDateRange[1].endOf('day') : undefined,
                currentFilters.selectedEditionId,
                currentFilters.selectedEditionId !== undefined && currentFilters.selectedEditionId + '' !== '-1',
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

    showUserImpersonateLookUpModal(record: any): void {
        if (abp.multiTenancy.getTenantIdCookie()) {
            this.message.warn(this.l('YouAreNotLoggedInAsAHostUser')).then(() => {
                document.location.reload();
            });

            return;
        }

        const modal = this.impersonateUserLookupModal();

        if (modal) {
            modal.tenantId = record.id;
            modal.show();
        }
    }

    unlockUser(record: any): void {
        this._tenantService.unlockTenantAdmin(new EntityDtoOfInt64({ id: record.id })).subscribe(() => {
            this.notify.success(this.l('UnlockedTenandAdmin', record.name));
        });
    }

    reloadPage(): void {
        this.paginator().changePage(this.paginator().getPage());
    }

    createTenant(): void {
        this.createTenantModal().show();
    }

    deleteTenant(tenant: TenantListDto): void {
        this.message.confirm(
            this.l('TenantDeleteWarningMessage', tenant.tenancyName),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (isConfirmed) {
                    this._tenantService.deleteTenant(tenant.id).subscribe(() => {
                        this.reloadPage();
                        this.notify.success(this.l('SuccessfullyDeleted'));
                    });
                }
            }
        );
    }

    showHistory(tenant: TenantListDto): void {
        this._router.navigate([abp.appPath + `/app/admin/entity-changes/${tenant.id}/${this._entityTypeFullName}`]);
    }

    impersonateUser(item: FindUsersOutputDto): void {
        this._impersonationService.impersonateTenant(item.id, this.impersonateUserLookupModal().tenantId);
    }

    updateFilterText(filterText: string): void {
        const currentFilters = this.filters();
        this.filters.set({ ...currentFilters, filterText });
    }

    updateSelectedEditionId(value: string): void {
        const currentFilters = this.filters();
        this.filters.set({
            ...currentFilters,
            selectedEditionId: value ? parseInt(value) : undefined,
        });
    }

    updateSubscriptionEndDateRangeActive(active: boolean): void {
        const currentFilters = this.filters();
        this.filters.set({ ...currentFilters, subscriptionEndDateRangeActive: active });
    }

    updateCreationDateRangeActive(active: boolean): void {
        const currentFilters = this.filters();
        this.filters.set({ ...currentFilters, creationDateRangeActive: active });
    }

    private setIsEntityHistoryEnabled(): void {
        let customSettings = (abp as any).custom;
        this.entityHistoryEnabled.set(
            customSettings.EntityHistory &&
                customSettings.EntityHistory.isEnabled &&
                _filter(
                    customSettings.EntityHistory.enabledEntities,
                    (entityType) => entityType === this._entityTypeFullName
                ).length === 1
        );
    }
}

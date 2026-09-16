import { Component, DestroyRef, OnInit, effect, inject, input, signal, untracked, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { AppComponentBase } from '@shared/common/app-component-base';
import { EmployeeBasicPayDto, EmployeeBasicPaysServiceProxy, GetAllEmployeeBasicPaysInput } from '@shared/service-proxies/service-proxies';
import { LazyLoadEvent, MenuItem } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { TieredMenu } from 'primeng/tieredmenu';
import { finalize } from 'rxjs/operators';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { CreateOrEditEmployeeBasicPayModalComponent } from './create-or-edit-employee-basic-pay-modal.component';
import { EmployeeBasicPayDetailViewComponent } from './employee-basic-pay-detail-view.component';

/** Query parameter holding the basic pay in view; the fragment stays the profile's section id. */
const BASIC_PAY_QUERY_PARAM = 'basicPay';

/**
 * Basic Pays section of the employee profile: the list, or the detail view of one basic pay.
 * The open basic pay is kept in `?basicPay=` so a refresh or a shared link lands on it.
 */
@Component({
    selector: 'employee-basic-pays',
    templateUrl: './employee-basic-pays.component.html',
    imports: [
        BusyIfDirective,
        TableModule,
        PaginatorModule,
        TieredMenu,
        LuxonFormatPipe,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
        CreateOrEditEmployeeBasicPayModalComponent,
        EmployeeBasicPayDetailViewComponent,
    ],
})
export class EmployeeBasicPaysComponent extends AppComponentBase implements OnInit {
    private _service = inject(EmployeeBasicPaysServiceProxy);
    private _route = inject(ActivatedRoute);
    private _router = inject(Router);
    private _destroyRef = inject(DestroyRef);

    readonly employeeId = input<number>();

    readonly createOrEditModal = viewChild<CreateOrEditEmployeeBasicPayModalComponent>('createOrEditModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly activeMenuItems = signal<MenuItem[]>([]);

    /** The basic pay shown in the detail view; undefined shows the list. */
    readonly selectedBasicPayId = signal<number | undefined>(undefined);

    constructor() {
        super();
        effect(() => {
            if (this.employeeId() && !untracked(() => this.selectedBasicPayId())) {
                this.reloadPage();
            }
        });
    }

    ngOnInit(): void {
        // Browser back/forward, a refresh and a shared link all land on the basic pay named in the URL.
        this._route.queryParamMap.pipe(takeUntilDestroyed(this._destroyRef)).subscribe((params) => {
            const id = Number(params.get(BASIC_PAY_QUERY_PARAM));
            this.selectedBasicPayId.set(id > 0 ? id : undefined);
        });
    }

    getRecords(event?: LazyLoadEvent): void {
        if (!this.employeeId()) {
            return;
        }

        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator()?.changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._service
            .getAll(
                new GetAllEmployeeBasicPaysInput({
                    employeeId: this.employeeId(),
                    sorting: this.primengTableHelper.getSorting(this.dataTable()),
                    maxResultCount: this.primengTableHelper.getMaxResultCount(this.paginator(), event),
                    skipCount: this.primengTableHelper.getSkipCount(this.paginator(), event),
                })
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(result.items);
                this.primengTableHelper.hideLoadingIndicator();
            });
    }

    reloadPage(): void {
        const paginator = this.paginator();
        const rows = paginator ? paginator.rows || this.primengTableHelper.defaultRecordsCountPerPage : this.primengTableHelper.defaultRecordsCountPerPage;
        this.getRecords({ first: (paginator ? paginator.getPage() : 0) * rows, rows } as LazyLoadEvent);
    }

    createRecord(): void {
        this.createOrEditModal()?.show(this.employeeId());
    }

    openDetail(record: EmployeeBasicPayDto): void {
        this.setSelectedBasicPay(record.id);
    }

    /** Back from the detail view. The list is rebuilt, so its (onLazyLoad) reloads it with any header or line changes. */
    closeDetail(): void {
        this.setSelectedBasicPay(undefined);
    }

    showMenu(event: Event, record: EmployeeBasicPayDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    getMenuItems(record: EmployeeBasicPayDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (this.permission.isGranted('Pages.PersonnelAdministration.EmployeeBasicPays.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.openDetail(record),
            });
        }

        if (this.permission.isGranted('Pages.PersonnelAdministration.EmployeeBasicPays.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteRecord(record),
            });
        }

        return items;
    }

    deleteRecord(record: EmployeeBasicPayDto): void {
        this.message.confirm(this.l('AreYouSure'), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._service.delete(record.id).subscribe(() => {
                    this.reloadPage();
                    this.notify.success(this.l('SuccessfullyDeleted'));
                });
            }
        });
    }

    private setSelectedBasicPay(id: number | undefined): void {
        this.selectedBasicPayId.set(id);
        // preserveFragment keeps the profile on this section; replaceUrl matches how the profile treats
        // moving around inside one record (not a separate place for the back button to visit).
        this._router.navigate([], {
            relativeTo: this._route,
            queryParams: { [BASIC_PAY_QUERY_PARAM]: id ?? null },
            queryParamsHandling: 'merge',
            preserveFragment: true,
            replaceUrl: true,
        });
    }
}

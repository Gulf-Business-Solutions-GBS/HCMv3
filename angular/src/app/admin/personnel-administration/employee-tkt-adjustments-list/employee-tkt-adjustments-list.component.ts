import { Component, OnInit, inject, viewChild, signal } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    EmployeeTktAdjustmentDto,
    EmployeeTktAdjustmentsServiceProxy,
    GetAllEmployeeTktAdjustmentsInput,
} from '@shared/service-proxies/service-proxies';
import { Table, TableModule } from 'primeng/table';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { LazyLoadEvent, MenuItem } from 'primeng/api';
import { TieredMenu } from 'primeng/tieredmenu';
import { FormsModule } from '@angular/forms';
import { DecimalPipe } from '@angular/common';
import { finalize } from 'rxjs/operators';
import { CreateOrEditEmployeeTktAdjustmentModalComponent } from '../employee-detail/tkt-adjustments/create-or-edit-employee-tkt-adjustment-modal.component';
import { tktAdjustmentTypeKey, tktPaymentTypeKey } from '../employee-detail/tkt-adjustments/tkt-adjustment-labels';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';

/**
 * Every employee's ticket adjustments in one list (legacy menu "Ticket Adjustment",
 * Employee_TktAdjust_ListView). The same rows are also on the Ticket Adjustments tab of each employee's record.
 */
@Component({
    templateUrl: './employee-tkt-adjustments-list.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        TieredMenu,
        CreateOrEditEmployeeTktAdjustmentModalComponent,
        LocalizePipe,
        PermissionPipe,
        LuxonFormatPipe,
        DecimalPipe,
    ],
})
export class EmployeeTktAdjustmentsListComponent extends AppComponentBase implements OnInit {
    private _service = inject(EmployeeTktAdjustmentsServiceProxy);

    readonly createOrEditModal = viewChild<CreateOrEditEmployeeTktAdjustmentModalComponent>('createOrEditModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly filterText = signal<string>('');
    readonly activeMenuItems = signal<MenuItem[]>([]);

    readonly adjustmentTypeKey = tktAdjustmentTypeKey;
    readonly paymentTypeKey = tktPaymentTypeKey;

    ngOnInit(): void {
        // Intentionally empty: the PrimeNG table's (onLazyLoad) triggers the first getRecords().
    }

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._service
            .getAll(
                new GetAllEmployeeTktAdjustmentsInput({
                    employeeId: undefined,
                    filter: this.filterText(),
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
        const rows = this.paginator().rows || this.primengTableHelper.defaultRecordsCountPerPage;
        this.getRecords({ first: this.paginator().getPage() * rows, rows } as LazyLoadEvent);
    }

    createRecord(): void {
        this.createOrEditModal().show(undefined);
    }

    showMenu(event: Event, record: EmployeeTktAdjustmentDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    getMenuItems(record: EmployeeTktAdjustmentDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (this.permission.isGranted('Pages.PersonnelAdministration.EmployeeTktAdjustments.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                // No employee passed: on this list the employee is chosen in the dialog, edit included.
                command: () => this.createOrEditModal().show(undefined, record.id),
            });
        }

        if (this.permission.isGranted('Pages.PersonnelAdministration.EmployeeTktAdjustments.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteRecord(record),
            });
        }

        return items;
    }

    deleteRecord(record: EmployeeTktAdjustmentDto): void {
        this.message.confirm(this.l('AreYouSure'), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._service.delete(record.id).subscribe(() => {
                    this.reloadPage();
                    this.notify.success(this.l('SuccessfullyDeleted'));
                });
            }
        });
    }
}

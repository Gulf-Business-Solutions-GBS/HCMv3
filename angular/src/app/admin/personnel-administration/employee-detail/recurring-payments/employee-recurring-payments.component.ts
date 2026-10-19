import { Component, effect, inject, input, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    EmployeeRecurringPaymentDto,
    EmployeeRecurringPaymentsServiceProxy,
    GetAllEmployeeRecurringPaymentsInput,
    PayInterval,
} from '@shared/service-proxies/service-proxies';
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
import { CreateOrEditEmployeeRecurringPaymentModalComponent } from './create-or-edit-employee-recurring-payment-modal.component';

/**
 * Employee-record section for recurring payments (legacy EmployeeRecurringPayment): a fixed amount of a
 * wage type paid every period between two dates. Same shape as the Remunerations section.
 */
@Component({
    selector: 'employee-recurring-payments',
    templateUrl: './employee-recurring-payments.component.html',
    imports: [
        BusyIfDirective,
        TableModule,
        PaginatorModule,
        TieredMenu,
        LuxonFormatPipe,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
        CreateOrEditEmployeeRecurringPaymentModalComponent,
    ],
})
export class EmployeeRecurringPaymentsComponent extends AppComponentBase {
    private _service = inject(EmployeeRecurringPaymentsServiceProxy);

    readonly employeeId = input<number>();

    readonly createOrEditModal = viewChild<CreateOrEditEmployeeRecurringPaymentModalComponent>('createOrEditModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly activeMenuItems = signal<MenuItem[]>([]);

    constructor() {
        super();
        effect(() => {
            if (this.employeeId()) {
                this.reloadPage();
            }
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
                new GetAllEmployeeRecurringPaymentsInput({
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

    payIntervalName(value: PayInterval): string {
        return PayInterval[value] ?? '';
    }

    showMenu(event: Event, record: EmployeeRecurringPaymentDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    getMenuItems(record: EmployeeRecurringPaymentDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (this.permission.isGranted('Pages.PersonnelAdministration.EmployeeRecurringPayments.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.createOrEditModal()?.show(this.employeeId(), record.id),
            });
        }

        if (this.permission.isGranted('Pages.PersonnelAdministration.EmployeeRecurringPayments.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteRecord(record),
            });
        }

        return items;
    }

    deleteRecord(record: EmployeeRecurringPaymentDto): void {
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

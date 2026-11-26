import { Component, OnInit, inject, viewChild, signal } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    EmployeeAbsenceDto,
    EmployeeAbsencesServiceProxy,
    GetAllEmployeeAbsencesInput,
    HalfdayType,
    LeavePaymentType,
} from '@shared/service-proxies/service-proxies';
import { Table, TableModule } from 'primeng/table';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { LazyLoadEvent, MenuItem } from 'primeng/api';
import { TieredMenu } from 'primeng/tieredmenu';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { DateTime } from 'luxon';
import { CreateOrEditEmployeeAbsenceModalComponent } from '../employee-detail/absences/create-or-edit-employee-absence-modal.component';
import { WorkflowHistoryModalComponent } from '@app/shared/common/workflow-activity/workflow-history-modal.component';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';

/**
 * Every employee's leave in one list (legacy Payroll Transaction > "Leave Transaction" menu item,
 * EmployeeAbsence_ListView). The same leave is also on the Absences tab of each employee's record; this is for HR
 * working across employees.
 */
@Component({
    templateUrl: './employee-absences-list.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        TieredMenu,
        CreateOrEditEmployeeAbsenceModalComponent,
        WorkflowHistoryModalComponent,
        LocalizePipe,
        PermissionPipe,
    ],
})
export class EmployeeAbsencesListComponent extends AppComponentBase implements OnInit {
    private _service = inject(EmployeeAbsencesServiceProxy);

    readonly createOrEditModal = viewChild<CreateOrEditEmployeeAbsenceModalComponent>('createOrEditModal');
    readonly workflowHistoryModal = viewChild<WorkflowHistoryModalComponent>('workflowHistoryModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly filterText = signal<string>('');
    readonly activeMenuItems = signal<MenuItem[]>([]);

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
                new GetAllEmployeeAbsencesInput({
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

    showMenu(event: Event, record: EmployeeAbsenceDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    getMenuItems(record: EmployeeAbsenceDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (record.workflowRequestId) {
            items.push({
                label: this.l('WorkflowHistory'),
                icon: 'pi pi-history',
                command: () => this.workflowHistoryModal()?.show(record.workflowRequestId),
            });
        }

        if (this.permission.isGranted('Pages.PersonnelAdministration.EmployeeAbsences.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                // No employee passed: on this list the employee is chosen in the dialog, edit included.
                command: () => this.createOrEditModal().show(undefined, record.id),
            });
        }

        if (this.permission.isGranted('Pages.PersonnelAdministration.EmployeeAbsences.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteRecord(record),
            });
        }

        return items;
    }

    deleteRecord(record: EmployeeAbsenceDto): void {
        this.message.confirm(this.l('AreYouSure'), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._service.delete(record.id).subscribe(() => {
                    this.reloadPage();
                    this.notify.success(this.l('SuccessfullyDeleted'));
                });
            }
        });
    }

    /** The server sends an unset DateTime as 0001-01-01; legacy's list showed those as blank. */
    formatDate(value: DateTime | undefined): string {
        return value?.isValid && value.year > 1 ? value.toFormat('yyyy-MM-dd') : '';
    }

    /** Localization keys, as used in the edit form's drop-downs. */
    halfdayTypeName(value: HalfdayType): string {
        return HalfdayType[value] ?? '';
    }

    leavePaymentTypeName(value: LeavePaymentType): string {
        return LeavePaymentType[value] ?? '';
    }
}

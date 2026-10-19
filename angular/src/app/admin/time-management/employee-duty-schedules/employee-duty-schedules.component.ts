import { Component, OnInit, inject, signal, viewChild } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    EmployeeDutyScheduleDto,
    EmployeeDutySchedulesServiceProxy,
    GetAllEmployeeDutySchedulesInput,
} from '@shared/service-proxies/service-proxies';
import { LazyLoadEvent, MenuItem } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { TieredMenu } from 'primeng/tieredmenu';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { CreateOrEditEmployeeDutyScheduleModalComponent } from '../../personnel-administration/employee-detail/duty-schedules/create-or-edit-employee-duty-schedule-modal.component';

/**
 * Duty Schedule (legacy Administration > Time Management > Duty Schedule): every employee's roster duty
 * schedule assignments on one list. The same records the employee record's Duty Schedules section shows,
 * and the same dialog -- opened without an employee, it asks for one.
 */
@Component({
    templateUrl: './employee-duty-schedules.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        TieredMenu,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
        LuxonFormatPipe,
        CreateOrEditEmployeeDutyScheduleModalComponent,
    ],
})
export class EmployeeDutySchedulesListComponent extends AppComponentBase implements OnInit {
    private _service = inject(EmployeeDutySchedulesServiceProxy);

    readonly createOrEditModal = viewChild<CreateOrEditEmployeeDutyScheduleModalComponent>('createOrEditModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly activeMenuItems = signal<MenuItem[]>([]);
    readonly filterText = signal<string>('');

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
                new GetAllEmployeeDutySchedulesInput({
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
        this.createOrEditModal().show();
    }

    showMenu(event: Event, record: EmployeeDutyScheduleDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    getMenuItems(record: EmployeeDutyScheduleDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (this.permission.isGranted('Pages.TimeManagement.EmployeeDutySchedules.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.createOrEditModal().show(record.employeeId, record.id),
            });
        }

        if (this.permission.isGranted('Pages.TimeManagement.EmployeeDutySchedules.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteRecord(record),
            });
        }

        return items;
    }

    deleteRecord(record: EmployeeDutyScheduleDto): void {
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

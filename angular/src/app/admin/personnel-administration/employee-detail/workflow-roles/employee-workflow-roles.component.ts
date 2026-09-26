import { Component, effect, inject, input, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    EmployeeWorkflowRoleDto,
    EmployeeWorkflowRolesServiceProxy,
    GetAllEmployeeWorkflowRolesInput,
} from '@shared/service-proxies/service-proxies';
import { Table, TableModule } from 'primeng/table';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { LazyLoadEvent, MenuItem } from 'primeng/api';
import { TieredMenu } from 'primeng/tieredmenu';
import { finalize } from 'rxjs/operators';
import { CreateOrEditEmployeeWorkflowRoleModalComponent } from './create-or-edit-employee-workflow-role-modal.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';

/**
 * Which workflow roles this employee holds, and between which dates.
 *
 * <p>A section of the employee record rather than a screen of its own. Holding a workflow role is a fact
 * about a person, like their manager or their bank details, and the question it answers -- "why did nobody
 * get the request?" -- is asked while looking at somebody.</p>
 *
 * <p>Each row says whether the assignment is in force today. That is the difficulty with these: approver
 * resolution only sees an assignment inside its date range, so a row dated in the past is a chain that
 * reaches nobody while still reading correctly in a list.</p>
 */
@Component({
    selector: 'employee-workflow-roles',
    templateUrl: './employee-workflow-roles.component.html',
    imports: [
        BusyIfDirective,
        TableModule,
        PaginatorModule,
        TieredMenu,
        LuxonFormatPipe,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
        CreateOrEditEmployeeWorkflowRoleModalComponent,
    ],
})
export class EmployeeWorkflowRolesComponent extends AppComponentBase {
    private _service = inject(EmployeeWorkflowRolesServiceProxy);

    readonly employeeId = input<number>();

    readonly createOrEditModal = viewChild<CreateOrEditEmployeeWorkflowRoleModalComponent>('createOrEditModal');
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
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._service
            .getAll(
                new GetAllEmployeeWorkflowRolesInput({
                    employeeId: this.employeeId(),

                    // Scoped to one person, so the screen's own filters are gone. Expired rows stay in the
                    // list: the reason to open this section is often to find the one that has lapsed.
                    filter: undefined,
                    workflowRoleId: undefined,
                    onlyActiveToday: undefined,
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
        const rows = this.paginator()
            ? this.paginator().rows || this.primengTableHelper.defaultRecordsCountPerPage
            : this.primengTableHelper.defaultRecordsCountPerPage;

        this.getRecords({ first: (this.paginator() ? this.paginator().getPage() : 0) * rows, rows } as LazyLoadEvent);
    }

    createRecord(): void {
        this.createOrEditModal().show(this.employeeId());
    }

    showMenu(event: Event, record: EmployeeWorkflowRoleDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    getMenuItems(record: EmployeeWorkflowRoleDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (this.permission.isGranted('Pages.Workflow.EmployeeWorkflowRoles.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.createOrEditModal().show(this.employeeId(), record.id),
            });
        }

        if (this.permission.isGranted('Pages.Workflow.EmployeeWorkflowRoles.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteRecord(record),
            });
        }

        return items;
    }

    deleteRecord(record: EmployeeWorkflowRoleDto): void {
        this.message.confirm(
            this.l('SimpleLookupDeleteWarningMessage', record.workflowRoleNameEn),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (isConfirmed) {
                    this._service.delete(record.id).subscribe(() => {
                        this.reloadPage();
                        this.notify.success(this.l('SuccessfullyDeleted'));
                    });
                }
            }
        );
    }
}

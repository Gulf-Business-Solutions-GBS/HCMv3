import { Component, OnInit, inject, viewChild, signal } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    EmployeeWorkflowRoleDto,
    EmployeeWorkflowRolesServiceProxy,
    GetAllEmployeeWorkflowRolesInput,
    GetAllWorkflowRolesInput,
    WorkflowRoleDto,
    WorkflowRolesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { Table, TableModule } from 'primeng/table';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { LazyLoadEvent, MenuItem } from 'primeng/api';
import { TieredMenu } from 'primeng/tieredmenu';
import { Tooltip } from 'primeng/tooltip';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { CreateOrEditEmployeeWorkflowRoleModalComponent } from './create-or-edit-employee-workflow-role-modal.component';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';

/**
 * Who holds which workflow role, and between which dates.
 *
 * <p>This is the screen that answers "why did nobody get the request?". A chain step pointing at a workflow
 * role resolves its approvers through these rows, and only the rows in force today count -- an assignment
 * whose range has passed is invisible to resolution while still looking perfectly correct in a list. So the
 * grid says per row whether it is live, and offers a filter to show only the live ones.</p>
 */
@Component({
    templateUrl: './employee-workflow-roles.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        TieredMenu,
        Tooltip,
        CreateOrEditEmployeeWorkflowRoleModalComponent,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
        LuxonFormatPipe,
    ],
})
export class EmployeeWorkflowRolesComponent extends AppComponentBase implements OnInit {
    private _service = inject(EmployeeWorkflowRolesServiceProxy);
    private _workflowRolesService = inject(WorkflowRolesServiceProxy);

    readonly createOrEditModal = viewChild<CreateOrEditEmployeeWorkflowRoleModalComponent>('createOrEditModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly filterText = signal<string>('');
    readonly filterWorkflowRoleId = signal<number | undefined>(undefined);
    readonly onlyActiveToday = signal<boolean>(false);
    readonly activeMenuItems = signal<MenuItem[]>([]);
    readonly workflowRoles = signal<WorkflowRoleDto[]>([]);

    ngOnInit(): void {
        this._workflowRolesService
            .getAll(
                new GetAllWorkflowRolesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.workflowRoles.set(result.items));

        // Intentionally no getRecords() here: the PrimeNG table's (onLazyLoad) triggers the first load.
    }

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._service
            .getAll(
                new GetAllEmployeeWorkflowRolesInput({
                    filter: this.filterText(),
                    workflowRoleId: this.filterWorkflowRoleId(),

                    // Sent as undefined rather than false when the box is clear, so the unfiltered view is
                    // genuinely unfiltered instead of asking the server for "not active", which is not what
                    // an unticked box means.
                    onlyActiveToday: this.onlyActiveToday() ? true : undefined,
                    employeeId: undefined,
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
                command: () => this.createOrEditModal().show(record.id),
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
            this.l('SimpleLookupDeleteWarningMessage', record.employeeFullNameEn),
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

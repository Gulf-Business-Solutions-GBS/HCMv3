import { Component, effect, inject, input, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    GetAllWorkflowGroupDetailsInput,
    GetAllWorkflowGroupFieldAccessesInput,
    WorkflowGroupDetailDto,
    WorkflowGroupDetailsServiceProxy,
    WorkflowGroupFieldAccessDto,
    WorkflowGroupFieldAccessesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { LazyLoadEvent, MenuItem } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { TieredMenu } from 'primeng/tieredmenu';
import { finalize } from 'rxjs/operators';
import { FormsModule } from '@angular/forms';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';
import { CreateOrEditWorkflowGroupFieldAccessModalComponent } from './create-or-edit-workflow-group-field-access-modal.component';

@Component({
    selector: 'workflow-group-field-accesses',
    templateUrl: './workflow-group-field-accesses.component.html',
    imports: [
        FormsModule,
        BusyIfDirective,
        TableModule,
        PaginatorModule,
        TieredMenu,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
        CreateOrEditWorkflowGroupFieldAccessModalComponent,
    ],
})
export class WorkflowGroupFieldAccessesComponent extends AppComponentBase {
    private _service = inject(WorkflowGroupFieldAccessesServiceProxy);
    private _groupDetailsService = inject(WorkflowGroupDetailsServiceProxy);

    readonly workflowGroupId = input<number>();

    readonly createOrEditModal = viewChild<CreateOrEditWorkflowGroupFieldAccessModalComponent>('createOrEditModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly activeMenuItems = signal<MenuItem[]>([]);
    readonly groupDetails = signal<WorkflowGroupDetailDto[]>([]);
    readonly selectedGroupDetailId = signal<number | undefined>(undefined);

    constructor() {
        super();
        effect(() => {
            if (this.workflowGroupId()) {
                this._groupDetailsService
                    .getAll(
                        new GetAllWorkflowGroupDetailsInput({
                            workflowGroupId: this.workflowGroupId(),
                            maxResultCount: 1000,
                            skipCount: 0,
                            sorting: undefined,
                        })
                    )
                    .subscribe((result) => this.groupDetails.set(result.items));
            }
        });
    }

    onGroupDetailChange(id: number | undefined): void {
        this.selectedGroupDetailId.set(id);
        this.reloadPage();
    }

    getRecords(event?: LazyLoadEvent): void {
        if (!this.selectedGroupDetailId()) {
            return;
        }

        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._service
            .getAll(
                new GetAllWorkflowGroupFieldAccessesInput({
                    workflowGroupDetailId: this.selectedGroupDetailId(),
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
        const rows = this.paginator() ? this.paginator().rows || this.primengTableHelper.defaultRecordsCountPerPage : this.primengTableHelper.defaultRecordsCountPerPage;
        this.getRecords({ first: (this.paginator() ? this.paginator().getPage() : 0) * rows, rows } as LazyLoadEvent);
    }

    createRecord(): void {
        this.createOrEditModal().show(this.selectedGroupDetailId());
    }

    showMenu(event: Event, record: WorkflowGroupFieldAccessDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    getMenuItems(record: WorkflowGroupFieldAccessDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (this.permission.isGranted('Pages.Workflow.WorkflowGroupFieldAccesses.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.createOrEditModal().show(this.selectedGroupDetailId(), record.id),
            });
        }

        if (this.permission.isGranted('Pages.Workflow.WorkflowGroupFieldAccesses.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteRecord(record),
            });
        }

        return items;
    }

    deleteRecord(record: WorkflowGroupFieldAccessDto): void {
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

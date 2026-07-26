import { Component, OnInit, inject, viewChild, signal } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    EntityDto,
    GetAllWorkflowRequestsInput,
    RequestStatus,
    WorkflowRequestDto,
    WorkflowRequestsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { Table, TableModule } from 'primeng/table';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { LazyLoadEvent, MenuItem } from 'primeng/api';
import { TieredMenu } from 'primeng/tieredmenu';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { CreateOrEditWorkflowRequestModalComponent } from './create-or-edit-workflow-request-modal.component';
import { WorkflowRequestRemarkModalComponent, WorkflowRequestActionType } from './workflow-request-remark-modal.component';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';

@Component({
    templateUrl: './workflow-requests.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        TieredMenu,
        CreateOrEditWorkflowRequestModalComponent,
        WorkflowRequestRemarkModalComponent,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
    ],
})
export class WorkflowRequestsComponent extends AppComponentBase implements OnInit {
    private _service = inject(WorkflowRequestsServiceProxy);

    readonly createOrEditModal = viewChild<CreateOrEditWorkflowRequestModalComponent>('createOrEditModal');
    readonly actionModal = viewChild<WorkflowRequestRemarkModalComponent>('actionModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly filterText = signal<string>('');
    readonly activeMenuItems = signal<MenuItem[]>([]);

    ngOnInit(): void {
        // Intentionally empty: the PrimeNG table's (onLazyLoad) triggers the first getRecords().
    }

    requestStatusLabel(value: RequestStatus): string {
        return this.l('RequestStatus' + RequestStatus[value]);
    }

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._service
            .getAll(
                new GetAllWorkflowRequestsInput({
                    filter: this.filterText(),
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

    showMenu(event: Event, record: WorkflowRequestDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    getMenuItems(record: WorkflowRequestDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (this.permission.isGranted('Pages.Workflow.WorkflowRequests.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.createOrEditModal().show(record.id),
            });
        }

        if (record.requestStatus === RequestStatus.Draft && this.permission.isGranted('Pages.Workflow.WorkflowRequests.Submit')) {
            items.push({
                label: this.l('Submit'),
                icon: 'pi pi-send',
                command: () => this.submitRecord(record),
            });
        }

        if (record.requestStatus === RequestStatus.Open) {
            if (this.permission.isGranted('Pages.Workflow.WorkflowRequests.Approve')) {
                items.push({
                    label: this.l('Approve'),
                    icon: 'pi pi-check',
                    command: () => this.actionModal().show(record.id, 'Approve'),
                });
            }

            if (this.permission.isGranted('Pages.Workflow.WorkflowRequests.Reject')) {
                items.push({
                    label: this.l('Reject'),
                    icon: 'pi pi-times',
                    command: () => this.actionModal().show(record.id, 'Reject'),
                });
            }

            if (this.permission.isGranted('Pages.Workflow.WorkflowRequests.SendBack')) {
                items.push({
                    label: this.l('SendBack'),
                    icon: 'pi pi-reply',
                    command: () => this.actionModal().show(record.id, 'SendBack'),
                });
            }

            if (this.permission.isGranted('Pages.Workflow.WorkflowRequests.Reassign')) {
                items.push({
                    label: this.l('Reassign'),
                    icon: 'pi pi-user-edit',
                    command: () => this.actionModal().show(record.id, 'Reassign'),
                });
            }
        }

        if (this.permission.isGranted('Pages.Workflow.WorkflowRequests.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteRecord(record),
            });
        }

        return items;
    }

    submitRecord(record: WorkflowRequestDto): void {
        this._service.submitWorkflowRequest(new EntityDto({ id: record.id })).subscribe(() => {
            this.reloadPage();
            this.notify.success(this.l('SavedSuccessfully'));
        });
    }

    deleteRecord(record: WorkflowRequestDto): void {
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

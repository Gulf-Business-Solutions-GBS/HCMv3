import { Component, effect, inject, signal, viewChild, input } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ApprovalRequire,
    GetAllRequestDetailsInput,
    RequestDetailDto,
    RequestDetailsServiceProxy,
    WorkflowApproverPersonType,
} from '@shared/service-proxies/service-proxies';
import { Table, TableModule } from 'primeng/table';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { LazyLoadEvent, MenuItem } from 'primeng/api';
import { TieredMenu } from 'primeng/tieredmenu';
import { finalize } from 'rxjs/operators';
import { CreateOrEditRequestDetailModalComponent } from './create-or-edit-request-detail-modal.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';

/**
 * The steps of one approval chain.
 *
 * <p>Used to be a screen of its own, listing every step of every chain behind a master dropdown. A step means
 * nothing away from the chain it belongs to, so it now lives inside the chain instead -- reached from the
 * master rather than picked out of a list.</p>
 *
 * <p>The chain is passed in and never chosen here, which is also why the modal no longer offers a master
 * dropdown: a step created on this screen belongs to the chain on screen.</p>
 */
@Component({
    selector: 'request-master-steps',
    templateUrl: './request-master-steps.component.html',
    imports: [
        BusyIfDirective,
        TableModule,
        PaginatorModule,
        TieredMenu,
        CreateOrEditRequestDetailModalComponent,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
    ],
})
export class RequestMasterStepsComponent extends AppComponentBase {
    private _service = inject(RequestDetailsServiceProxy);

    readonly requestMasterId = input<number>();

    readonly createOrEditModal = viewChild<CreateOrEditRequestDetailModalComponent>('createOrEditModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly activeMenuItems = signal<MenuItem[]>([]);

    constructor() {
        super();
        effect(() => {
            if (this.requestMasterId()) {
                this.reloadPage();
            }
        });
    }

    approvalRequireLabel(value: ApprovalRequire): string {
        return this.l('ApprovalRequire' + ApprovalRequire[value]);
    }

    approverPersonLabel(value: WorkflowApproverPersonType): string {
        return this.l('WorkflowApproverPersonType' + WorkflowApproverPersonType[value]);
    }

    /**
     * Whether this step names an approver at all.
     *
     * <p>A step with none is the creator's own step: approvals begin at the first step that names somebody.
     * Worth marking in the list, because a chain whose every step is None never reaches anyone.</p>
     */
    hasApprover(record: RequestDetailDto): boolean {
        return record.approverPerson !== WorkflowApproverPersonType.None;
    }

    getRecords(event?: LazyLoadEvent): void {
        if (!this.requestMasterId()) {
            return;
        }

        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._service
            .getAll(
                new GetAllRequestDetailsInput({
                    requestMasterId: this.requestMasterId(),
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
        this.createOrEditModal().show(this.requestMasterId());
    }

    showMenu(event: Event, record: RequestDetailDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    getMenuItems(record: RequestDetailDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (this.permission.isGranted('Pages.Workflow.RequestDetails.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.createOrEditModal().show(this.requestMasterId(), record.id),
            });
        }

        if (this.permission.isGranted('Pages.Workflow.RequestDetails.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteRecord(record),
            });
        }

        return items;
    }

    deleteRecord(record: RequestDetailDto): void {
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

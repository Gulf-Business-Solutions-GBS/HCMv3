import { Component, computed, effect, inject, signal, viewChild, input } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ApprovalRequire,
    EntityDto,
    GetAllRequestDetailsInput,
    RequestDetailDto,
    ReorderRequestDetailsInput,
    RequestDetailsServiceProxy,
    WorkflowApproverPersonType,
} from '@shared/service-proxies/service-proxies';
import { Table, TableModule } from 'primeng/table';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { LazyLoadEvent, MenuItem } from 'primeng/api';
import { TieredMenu } from 'primeng/tieredmenu';
import { Tooltip } from 'primeng/tooltip';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
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
    styleUrl: './request-master-steps.component.css',
    imports: [
        BusyIfDirective,
        TableModule,
        PaginatorModule,
        TieredMenu,
        Tooltip,
        ButtonBusyDirective,
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

    /** The order the rows are in on screen, once something has been dragged. Undefined until then. */
    readonly pendingOrder = signal<number[] | undefined>(undefined);

    /** The order the server last gave us, which is what "changed" is measured against. */
    readonly savedOrder = signal<number[]>([]);

    readonly savingOrder = signal(false);

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

                // The baseline for "has the order changed", and the reset after a save or a discard: a
                // fresh load is by definition the order the chain has.
                this.savedOrder.set(result.items.map((r) => r.id));
                this.pendingOrder.set(undefined);

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

    /**
     * Duplicates a step onto the end of the chain.
     *
     * <p>Steps in one chain agree on almost everything -- the same escalation days, the same notification
     * settings, the same send-back and reject rules -- and differ in a field or two. Copying one and
     * changing what differs is the difference between building a chain in a minute and in ten.</p>
     */
    copyRecord(record: RequestDetailDto): void {
        this._service.copyStep(new EntityDto({ id: record.id })).subscribe((created) => {
            this.notify.success(this.l('StepCopied'));
            this.reloadPage();

            // Opened straight away: a copy exists to be changed, and the one field that differs is the
            // reason it was made.
            this.createOrEditModal().show(this.requestMasterId(), created.id);
        });
    }

    /**
     * Records a drag. Nothing is sent yet.
     *
     * <p>Putting a chain in the right order usually takes several drags, and saving each one would mean a
     * round trip per drag and a renumbering the person did not ask for while they are still halfway through
     * rearranging. The order is held here until Save Step Order is pressed.</p>
     *
     * <p>PrimeNG has already moved the row inside the array by the time this fires. The array is copied
     * into the signal rather than left mutated, because a signal holding the same array reference does not
     * notify, and the step numbers shown are positions in that array.</p>
     */
    onRowReorder(): void {
        const rows = [...(this.primengTableHelper.records() as RequestDetailDto[])];

        this.primengTableHelper.records.set(rows);
        this.pendingOrder.set(rows.map((r) => r.id));
    }

    /**
     * True once the rows on screen are in a different order from the one the server last returned.
     *
     * <p>Compared against the order as loaded rather than against a simple "has been dragged" flag, so
     * dragging a row out and back again correctly leaves nothing to save.</p>
     */
    readonly orderChanged = computed(() => {
        const pending = this.pendingOrder();
        const saved = this.savedOrder();

        return !!pending && (pending.length !== saved.length || pending.some((id, i) => id !== saved[i]));
    });

    /** Sends the order the rows are in now and lets the server renumber 1..n. */
    saveOrder(): void {
        const ids = this.pendingOrder();
        if (!ids || this.savingOrder()) {
            return;
        }

        this.savingOrder.set(true);

        this._service
            .reorderSteps(new ReorderRequestDetailsInput({ requestMasterId: this.requestMasterId(), orderedIds: ids }))
            .pipe(finalize(() => this.savingOrder.set(false)))
            .subscribe({
                next: () => {
                    this.notify.success(this.l('StepsReordered'));
                    this.reloadPage();
                },
                // Put the grid back: it is showing an order the chain does not have, and leaving it that
                // way after a refusal invites pressing Save again against the same stale list.
                error: () => this.reloadPage(),
            });
    }

    /** Throws away the dragging and reloads whatever the chain actually has. */
    discardOrder(): void {
        this.reloadPage();
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

        if (this.permission.isGranted('Pages.Workflow.RequestDetails.Create')) {
            items.push({
                label: this.l('Copy'),
                icon: 'pi pi-copy',
                command: () => this.copyRecord(record),
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

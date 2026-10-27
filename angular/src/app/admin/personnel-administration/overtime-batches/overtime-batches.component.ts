import { Component, OnInit, inject, signal, viewChild } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    EntityDto,
    GetAllOvertimeBatchesInput,
    OvertimeBatchListDto,
    OvertimeBatchesServiceProxy,
    RequestStatus,
} from '@shared/service-proxies/service-proxies';
import { Table, TableModule } from 'primeng/table';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { LazyLoadEvent, MenuItem } from 'primeng/api';
import { TieredMenu } from 'primeng/tieredmenu';
import { Tooltip } from 'primeng/tooltip';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '../../../../shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { CreateOrEditOvertimeBatchModalComponent } from './create-or-edit-overtime-batch-modal.component';

/**
 * Department overtime batches (legacy Overtime Request): a manager or HR picks a unit and dates, enters the
 * approved hours per employee per day, and puts the lot forward for approval as one request. Approval writes
 * the overtime records payroll reads.
 */
@Component({
    templateUrl: './overtime-batches.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        TableModule,
        PaginatorModule,
        TieredMenu,
        Tooltip,
        DecimalPipe,
        LocalizePipe,
        PermissionPipe,
        CreateOrEditOvertimeBatchModalComponent,
    ],
})
export class OvertimeBatchesComponent extends AppComponentBase implements OnInit {
    private _service = inject(OvertimeBatchesServiceProxy);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');
    readonly editModal = viewChild<CreateOrEditOvertimeBatchModalComponent>('editModal');

    readonly activeMenuItems = signal<MenuItem[]>([]);

    ngOnInit(): void {
        // Intentionally empty: the PrimeNG table's (onLazyLoad) triggers the first getBatches().
    }

    getBatches(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._service
            .getAll(
                new GetAllOvertimeBatchesInput({
                    organizationUnitId: undefined,
                    workflowStatus: undefined,
                    fromDate: undefined,
                    toDate: undefined,
                    sorting: this.primengTableHelper.getSorting(this.dataTable()),
                    maxResultCount: this.primengTableHelper.getMaxResultCount(this.paginator(), event),
                    skipCount: this.primengTableHelper.getSkipCount(this.paginator(), event),
                })
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(result.items);
            });
    }

    reloadPage(): void {
        const rows = this.paginator().rows || this.primengTableHelper.defaultRecordsCountPerPage;
        this.getBatches({ first: this.paginator().getPage() * rows, rows } as LazyLoadEvent);
    }

    createBatch(): void {
        this.editModal().show();
    }

    showMenu(event: Event, record: OvertimeBatchListDto, menu: TieredMenu): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    /** Every action on every row; one that does not apply is greyed out rather than left out. */
    getMenuItems(record: OvertimeBatchListDto): MenuItem[] {
        const editable = this.isEditable(record);

        return [
            {
                label: this.l('Details'),
                icon: 'pi pi-eye',
                command: () => this.editModal().show(record.id),
            },
            {
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                disabled: !editable || !this.permission.isGranted('Pages.PersonnelAdministration.OvertimeBatches.Edit'),
                command: () => this.editModal().show(record.id),
            },
            {
                label: this.l('Submit'),
                icon: 'pi pi-send',
                disabled:
                    record.workflowStatus !== RequestStatus.Draft ||
                    !record.workflowRequestId ||
                    !this.permission.isGranted('Pages.PersonnelAdministration.OvertimeBatches.Edit'),
                command: () => this.resubmit(record),
            },
            {
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                disabled:
                    !editable || !this.permission.isGranted('Pages.PersonnelAdministration.OvertimeBatches.Delete'),
                command: () => this.deleteBatch(record),
            },
        ];
    }

    resubmit(record: OvertimeBatchListDto): void {
        this.message.confirm('', this.l('AreYouSure'), (isConfirmed) => {
            if (!isConfirmed) {
                return;
            }

            this._service.resubmitOvertimeBatch(new EntityDto({ id: record.id })).subscribe(() => {
                this.notify.success(this.l('SavedSuccessfully'));
                this.reloadPage();
            });
        });
    }

    deleteBatch(record: OvertimeBatchListDto): void {
        this.message.confirm('', this.l('AreYouSure'), (isConfirmed) => {
            if (!isConfirmed) {
                return;
            }

            this._service.delete(record.id).subscribe(() => {
                this.notify.success(this.l('SuccessfullyDeleted'));
                this.reloadPage();
            });
        });
    }

    /** Not routed yet, or sent back; and nothing posted. Matches the server's rule. */
    isEditable(record: OvertimeBatchListDto): boolean {
        return (
            !record.isPosted &&
            (record.workflowStatus === undefined ||
                record.workflowStatus === null ||
                record.workflowStatus === RequestStatus.Draft)
        );
    }

    /** A batch with no workflow status was recorded with workflow off. */
    statusLabel(record: OvertimeBatchListDto): string {
        if (record.workflowStatus === undefined || record.workflowStatus === null) {
            return this.l('Recorded');
        }

        return this.l('WorkflowRequestStatus_' + RequestStatus[record.workflowStatus]);
    }

    statusClass(record: OvertimeBatchListDto): string {
        switch (record.workflowStatus) {
            case RequestStatus.Open:
                return 'badge-warning';
            case RequestStatus.Approved:
            case RequestStatus.Closed:
                return 'badge-success';
            case RequestStatus.Rejected:
                return 'badge-danger';
            case RequestStatus.Draft:
                return 'badge-light-warning';
            default:
                return 'badge-secondary';
        }
    }

    date(value: { toFormat(format: string): string; year?: number } | undefined): string {
        return !value || (value.year !== undefined && value.year <= 1) ? '' : value.toFormat('yyyy-MM-dd');
    }
}

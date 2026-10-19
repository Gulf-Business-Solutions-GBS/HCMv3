import { Component, effect, inject, input, signal, viewChild } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { GetAllLeavePlansInput, LeavePlanDto, LeavePlansServiceProxy, RequestStatus } from '@shared/service-proxies/service-proxies';
import { LazyLoadEvent, MenuItem } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { TieredMenu } from 'primeng/tieredmenu';
import { Tooltip } from 'primeng/tooltip';
import { DateTime } from 'luxon';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '../../../../shared/utils/busy-if.directive';

/**
 * HR's list of leave plans (legacy back-office <c>LeavePlanner_ListView</c>, "Leave Plan").
 *
 * <p>Two places, one component: as its own page under Personnel Administration, and as the Leave Plans section of
 * the employee record, where the host passes <c>employeeId</c> and the page chrome and employee columns are left
 * out. Read and remove only -- a plan is raised by the employee, and decided in the approver's inbox.</p>
 */
@Component({
    selector: 'leave-plans',
    templateUrl: './leave-plans.component.html',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, BusyIfDirective, NgTemplateOutlet, FormsModule, TableModule, PaginatorModule, TieredMenu, Tooltip, LocalizePipe],
})
export class LeavePlansComponent extends AppComponentBase {
    private readonly _service = inject(LeavePlansServiceProxy);

    /** Set by the employee record; unset on the stand-alone page. */
    readonly employeeId = input<number>();

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly activeMenuItems = signal<MenuItem[]>([]);

    filterText = '';
    includePending = true;

    constructor() {
        super();

        // On the employee record the section is created before its employee is known; reload when it arrives.
        effect(() => {
            if (this.employeeId() && this.paginator()) {
                this.reloadPage();
            }
        });
    }

    get embedded(): boolean {
        return !!this.employeeId();
    }

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator()?.changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._service
            .getAll(
                new GetAllLeavePlansInput({
                    filter: this.embedded ? undefined : this.filterText,
                    employeeId: this.employeeId(),
                    fromDate: undefined,
                    toDate: undefined,
                    includePending: this.includePending,
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
        const paginator = this.paginator();
        if (!paginator) {
            return;
        }

        const rows = paginator.rows || this.primengTableHelper.defaultRecordsCountPerPage;
        this.getRecords({ first: paginator.getPage() * rows, rows } as LazyLoadEvent);
    }

    showMenu(event: Event, record: LeavePlanDto, menu: TieredMenu): void {
        this.activeMenuItems.set([
            {
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                disabled: !this.permission.isGranted('Pages.PersonnelAdministration.LeavePlans.Delete'),
                command: () => this.delete(record),
            },
        ]);
        menu.toggle(event);
    }

    delete(record: LeavePlanDto): void {
        this.message.confirm(this.l('AreYouSure'), this.l('Delete'), (isConfirmed) => {
            if (!isConfirmed) {
                return;
            }

            // Raw id: NSwag flattens EntityDto on DELETE.
            this._service.delete(record.id).subscribe(() => {
                this.notify.success(this.l('SuccessfullyDeleted'));
                this.reloadPage();
            });
        });
    }

    employeeName(record: LeavePlanDto): string {
        return this.isRtl ? record.employeeFullNameAr || record.employeeFullNameEn : record.employeeFullNameEn;
    }

    date(value: DateTime | undefined): string {
        return value && value.year > 1 ? value.toFormat('dd/MM/yyyy') : '';
    }

    /** A plan with no workflow status was recorded without approval; it is not given a status it never had. */
    statusLabel(record: LeavePlanDto): string {
        if (record.workflowStatus === undefined || record.workflowStatus === null) {
            return this.l('Recorded');
        }

        return this.l('WorkflowRequestStatus_' + RequestStatus[record.workflowStatus]);
    }

    statusClass(record: LeavePlanDto): string {
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

    private get isRtl(): boolean {
        return document?.documentElement?.getAttribute('dir') === 'rtl';
    }
}

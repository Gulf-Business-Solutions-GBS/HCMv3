import { Component, OnInit, inject, viewChild, signal } from '@angular/core';
import { NgClass } from '@angular/common';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    PayrollControlRecordDto,
    PayrollControlRecordsServiceProxy,
    GetAllPayrollControlRecordsInput,
    PayrollPeriodState,
    SetPayrollPeriodStateInput,
} from '@shared/service-proxies/service-proxies';
import { Table, TableModule } from 'primeng/table';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { LazyLoadEvent } from 'primeng/api';
import { TieredMenu } from 'primeng/tieredmenu';
import { MenuItem } from 'primeng/api';
import { Tooltip } from 'primeng/tooltip';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { CreateOrEditPayrollControlRecordModalComponent } from './create-or-edit-payroll-control-record-modal.component';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '../../../../shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';
import { LuxonFormatPipe } from '../../../../shared/utils/luxon-format.pipe';

@Component({
    templateUrl: './payroll-control-records.component.html',
    animations: [appModuleAnimation()],
    imports: [
        NgClass,
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        TieredMenu,
        Tooltip,
        CreateOrEditPayrollControlRecordModalComponent,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
        LuxonFormatPipe,
    ],
})
export class PayrollControlRecordsComponent extends AppComponentBase implements OnInit {
    private _payrollControlRecordsService = inject(PayrollControlRecordsServiceProxy);

    /** For the template, which cannot reach an imported enum on its own. */
    readonly PayrollPeriodState = PayrollPeriodState;

    readonly createOrEditModal = viewChild<CreateOrEditPayrollControlRecordModalComponent>('createOrEditModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly activeMenuItems = signal<MenuItem[]>([]);

    ngOnInit(): void {
        // Intentionally empty: the PrimeNG table's (onLazyLoad) triggers the first getPayrollControlRecords().
    }

    getPayrollControlRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._payrollControlRecordsService
            .getAll(
                new GetAllPayrollControlRecordsInput({
                    payrollAreaId: undefined,
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
        this.getPayrollControlRecords({ first: this.paginator().getPage() * rows, rows } as LazyLoadEvent);
    }

    createPayrollControlRecord(): void {
        this.createOrEditModal().show();
    }

    showMenu(event: Event, record: PayrollControlRecordDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    /// The state in words, for the badge.
    stateLabel(record: PayrollControlRecordDto): string {
        switch (record.periodState) {
            case PayrollPeriodState.ReleasedForPayroll:
                return this.l('ReleasedForPayroll');
            case PayrollPeriodState.ReleasedForCorrection:
                return this.l('ReleasedForCorrection');
            case PayrollPeriodState.Closed:
                return this.l('Closed');
            default:
                return this.l('Open');
        }
    }

    stateBadgeClass(record: PayrollControlRecordDto): string {
        switch (record.periodState) {
            case PayrollPeriodState.ReleasedForPayroll:
                return 'badge-light-danger';
            case PayrollPeriodState.ReleasedForCorrection:
                return 'badge-light-warning';
            case PayrollPeriodState.Closed:
                return 'badge-light-dark';
            default:
                return 'badge-light-success';
        }
    }

    /**
     * Which of the three buttons a period can use, exactly as legacy enabled them.
     *
     * A period not started, or reopened to correct data, can only be locked. A locked period can be released
     * back for correction or exited. A period already exited can only be locked again -- which starts the next
     * month. Nothing else is offered: legacy had no "reopen a closed period" and neither has this.
     */
    canLockForPayroll(record: PayrollControlRecordDto): boolean {
        return record.periodState !== PayrollPeriodState.ReleasedForPayroll;
    }

    canReleaseForCorrection(record: PayrollControlRecordDto): boolean {
        return record.periodState === PayrollPeriodState.ReleasedForPayroll;
    }

    canExitPayroll(record: PayrollControlRecordDto): boolean {
        return record.periodState === PayrollPeriodState.ReleasedForPayroll;
    }

    get canChangeState(): boolean {
        return this.permission.isGranted('Pages.PayrollManagement.PayrollControlRecords.SetState');
    }

    /**
     * The sentence legacy showed beside the record: what the period allows right now, and which month and run
     * type it is sitting on. Built here rather than on the server so the month name follows the user's language.
     */
    payrollStatus(record: PayrollControlRecordDto): string {
        const month = record.calendarMonthStart;

        if (record.periodState === PayrollPeriodState.ReleasedForPayroll) {
            return this.l('PayrollStatusRegular', month ? month.toFormat('LLLL-yyyy') : '');
        }

        if (record.periodState === PayrollPeriodState.Closed) {
            // Exited: what runs next is an off-cycle, and it belongs to the month after this one.
            const next = month ? month.plus({ months: 1 }) : undefined;
            return this.l('PayrollStatusOffCycle', next ? next.toFormat('LLLL-yyyy') : '');
        }

        return this.l('PayrollStatusOpen');
    }

    /** Locking a closed period starts the next month, so that one is confirmed before it happens. */
    lockForPayroll(record: PayrollControlRecordDto): void {
        if (record.periodState !== PayrollPeriodState.Closed) {
            this.setState(record, PayrollPeriodState.ReleasedForPayroll);
            return;
        }

        const nextMonth = record.calendarMonthStart?.plus({ months: 1 })?.toFormat('LLLL yyyy') ?? '';

        this.message.confirm(this.l('LockStartsNextMonthWarning', nextMonth), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this.setState(record, PayrollPeriodState.ReleasedForPayroll);
            }
        });
    }

    setState(record: PayrollControlRecordDto, state: PayrollPeriodState): void {
        this._payrollControlRecordsService
            .setState(new SetPayrollPeriodStateInput({ id: record.id, state }))
            .subscribe(() => {
                this.reloadPage();
                this.notify.success(this.l('SavedSuccessfully'));
            });
    }

    getMenuItems(record: PayrollControlRecordDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (this.permission.isGranted('Pages.PayrollManagement.PayrollControlRecords.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.createOrEditModal().show(record.id),
            });
        }

        if (this.permission.isGranted('Pages.PayrollManagement.PayrollControlRecords.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deletePayrollControlRecord(record),
            });
        }

        return items;
    }

    deletePayrollControlRecord(record: PayrollControlRecordDto): void {
        this.message.confirm(
            this.l('SimpleLookupDeleteWarningMessage', record.payrollAreaName),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (isConfirmed) {
                    this._payrollControlRecordsService.delete(record.id).subscribe(() => {
                        this.reloadPage();
                        this.notify.success(this.l('SuccessfullyDeleted'));
                    });
                }
            }
        );
    }
}

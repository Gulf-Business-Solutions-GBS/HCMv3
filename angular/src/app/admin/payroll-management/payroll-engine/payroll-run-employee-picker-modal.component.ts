import { Component, computed, inject, output, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LazyLoadEvent } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';

import { AppComponentBase } from '@shared/common/app-component-base';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import {
    GetPayrollRunCandidatesInput,
    PayrollRunCandidateDto,
    PayrollRunCandidateFilter,
    PayrollRunCandidateStatus,
    PayrollRunSetupServiceProxy,
    PayrollRunType,
} from '@shared/service-proxies/service-proxies';

/** What the Payroll Engine screen hands the picker when it opens. */
export interface PayrollRunEmployeePickerArgs {
    payrollAreaName: string;
    payrollRunType: PayrollRunType;
    offCycleReason?: string;
    /** The comma-separated codes already in the Employee Code box. */
    selectedCodes?: string;
    periodStatusText?: string;
    simulationMonth?: number;
    simulationYear?: number;
}

/**
 * Port of legacy uscPayrollEngine's employee popup (uscPayrollEngine.ascx.cs BuildEmployeePopupList lines 788-1020,
 * filters / select all / OK lines 1093-1313): the employees eligible for the run, each Processed or Pending, with the
 * All / Processed / Pending / Selected filters, a search, select all and the selected count. OK writes the selected
 * codes back as one comma-separated string.
 *
 * Selections are kept here across pages and filters, as legacy kept them in session.
 */
@Component({
    selector: 'payrollRunEmployeePickerModal',
    templateUrl: './payroll-run-employee-picker-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, TableModule, PaginatorModule, LocalizePipe, BusyIfDirective],
})
export class PayrollRunEmployeePickerModalComponent extends AppComponentBase {
    private _payrollRunSetupService = inject(PayrollRunSetupServiceProxy);

    readonly modal = viewChild<ModalDirective>('pickerModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    /** Emits the selected employee codes, comma-separated, when the user presses OK. */
    readonly codesSelected = output<string>();

    readonly PayrollRunCandidateFilter = PayrollRunCandidateFilter;
    readonly PayrollRunCandidateStatus = PayrollRunCandidateStatus;

    readonly active = signal<boolean>(false);
    readonly working = signal<boolean>(false);
    readonly args = signal<PayrollRunEmployeePickerArgs | undefined>(undefined);

    readonly statusFilter = signal<PayrollRunCandidateFilter>(PayrollRunCandidateFilter.All);
    readonly filterText = signal<string>('');

    readonly allCount = signal<number>(0);
    readonly processedCount = signal<number>(0);
    readonly pendingCount = signal<number>(0);

    /** Codes ticked so far, kept across pages and filters. Lower-cased: legacy matched codes case-insensitively. */
    readonly selected = signal<Set<string>>(new Set<string>());
    readonly selectedCount = computed(() => this.selected().size);

    show(args: PayrollRunEmployeePickerArgs): void {
        this.args.set(args);
        this.statusFilter.set(PayrollRunCandidateFilter.All);
        this.filterText.set('');
        this.selected.set(new Set<string>(this.splitCodes(args.selectedCodes).map((c) => this.key(c))));
        this.primengTableHelper.records.set([]);
        this.primengTableHelper.totalRecordsCount.set(0);
        this.active.set(true);
        this.modal().show();
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }

    getCandidates(event?: LazyLoadEvent): void {
        if (!this.args()) {
            return;
        }

        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        const input = this.buildInput();
        input.sorting = this.primengTableHelper.getSorting(this.dataTable());
        input.maxResultCount = this.primengTableHelper.getMaxResultCount(this.paginator(), event);
        input.skipCount = this.primengTableHelper.getSkipCount(this.paginator(), event);

        this._payrollRunSetupService
            .getPayrollRunCandidates(input)
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(result.items);
                this.allCount.set(result.allCount);
                this.processedCount.set(result.processedCount);
                this.pendingCount.set(result.pendingCount);
            });
    }

    setStatusFilter(filter: PayrollRunCandidateFilter): void {
        this.statusFilter.set(filter);
        this.reloadFromFirstPage();
    }

    applySearch(): void {
        this.reloadFromFirstPage();
    }

    isSelected(record: PayrollRunCandidateDto): boolean {
        return this.selected().has(this.key(record.employeeCode));
    }

    toggle(record: PayrollRunCandidateDto, checked: boolean): void {
        const next = new Set(this.selected());
        if (checked) {
            next.add(this.key(record.employeeCode));
        } else {
            next.delete(this.key(record.employeeCode));
        }
        this.selected.set(next);
    }

    /**
     * Legacy SelectAllEmployees: ticks (or unticks) EVERY row the current filter and search show, on every page, not
     * just the page on screen.
     */
    selectAll(select: boolean): void {
        this.working.set(true);
        this._payrollRunSetupService
            .getPayrollRunCandidateCodes(this.buildInput())
            .pipe(finalize(() => this.working.set(false)))
            .subscribe((codes) => {
                const next = new Set(this.selected());
                (codes || []).forEach((code) => (select ? next.add(this.key(code)) : next.delete(this.key(code))));
                this.selected.set(next);

                // The Selected filter's rows change with the selection.
                if (this.statusFilter() === PayrollRunCandidateFilter.Selected) {
                    this.reloadFromFirstPage();
                }
            });
    }

    /**
     * Legacy "OK": the codes of the listed employees that are ticked. A code typed by hand that is not in the list is
     * dropped, exactly as legacy rebuilt the box from its list.
     */
    ok(): void {
        const input = this.buildInput();
        input.statusFilter = PayrollRunCandidateFilter.All;
        input.filter = undefined;

        this.working.set(true);
        this._payrollRunSetupService
            .getPayrollRunCandidateCodes(input)
            .pipe(finalize(() => this.working.set(false)))
            .subscribe((codes) => {
                const chosen = (codes || []).filter((code) => this.selected().has(this.key(code)));
                this.codesSelected.emit(chosen.join(','));
                this.close();
            });
    }

    statusBadgeClass(record: PayrollRunCandidateDto): string {
        return record.payrollStatus === PayrollRunCandidateStatus.Processed
            ? 'badge badge-light-success'
            : 'badge badge-light-warning';
    }

    private reloadFromFirstPage(): void {
        this.getCandidates({ first: 0, rows: this.paginator()?.rows || this.primengTableHelper.defaultRecordsCountPerPage } as LazyLoadEvent);
    }

    private buildInput(): GetPayrollRunCandidatesInput {
        const args = this.args();
        return new GetPayrollRunCandidatesInput({
            payrollAreaName: args.payrollAreaName,
            payrollRunType: args.payrollRunType,
            offCycleReason: args.payrollRunType === PayrollRunType.OffCycle ? args.offCycleReason : undefined,
            statusFilter: this.statusFilter(),
            filter: this.filterText() ? this.filterText() : undefined,
            selectedEmployeeCodes: Array.from(this.selected()).join(','),
            simulationMonth: args.simulationMonth,
            simulationYear: args.simulationYear,
            sorting: undefined,
            skipCount: 0,
            maxResultCount: 1000,
        });
    }

    private key(code: string): string {
        return (code || '').trim().toLowerCase();
    }

    private splitCodes(codes?: string): string[] {
        return (codes || '')
            .split(',')
            .map((c) => c.trim())
            .filter((c) => c.length > 0);
    }
}

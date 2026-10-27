import { Component, computed, inject, output, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DateTime } from 'luxon';
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
    GetTimeEngineRunCandidatesInput,
    PayrollRunCandidateDto,
    PayrollRunCandidateFilter,
    PayrollRunCandidateStatus,
    TimeEngineServiceProxy,
} from '@shared/service-proxies/service-proxies';

/** What the Time Engine screen hands the picker when it opens. */
export interface TimeEngineEmployeePickerArgs {
    payrollAreaName: string;
    /** The Start Date box ('yyyy-MM-dd'), or empty (legacy: today). */
    startDate?: string;
    /** The End Date box ('yyyy-MM-dd'), or empty (legacy: the start date). */
    endDate?: string;
    /** The comma-separated codes already in the Employee Code box. */
    selectedCodes?: string;
}

/**
 * Port of legacy uscTimeEngine's employee popup (popupTimeEmployees; uscTimeEngine.ascx.cs BuildTimeEmployeePopupList
 * lines 581-650, filters / select all / OK lines 652-842): the payroll area's employees for the Start-End window, each
 * Processed (payroll already run for the area's current period) or Pending, with the All / Processed / Pending /
 * Selected filters and their counts, a search, select all, the selected count, and the area and period in the
 * header. OK writes the selected codes back as one comma-separated string.
 *
 * Same shape as the Payroll Engine screen's picker (payroll-run-employee-picker-modal.component); selections are kept
 * here across pages and filters, as legacy kept them in session.
 */
@Component({
    selector: 'timeEngineEmployeePickerModal',
    templateUrl: './time-engine-employee-picker-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, TableModule, PaginatorModule, LocalizePipe, BusyIfDirective],
})
export class TimeEngineEmployeePickerModalComponent extends AppComponentBase {
    private _timeEngineService = inject(TimeEngineServiceProxy);

    readonly modal = viewChild<ModalDirective>('pickerModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    /** Emits the selected employee codes, comma-separated, when the user presses OK. */
    readonly codesSelected = output<string>();

    readonly PayrollRunCandidateFilter = PayrollRunCandidateFilter;
    readonly PayrollRunCandidateStatus = PayrollRunCandidateStatus;

    readonly active = signal<boolean>(false);
    readonly working = signal<boolean>(false);
    readonly args = signal<TimeEngineEmployeePickerArgs | undefined>(undefined);

    readonly statusFilter = signal<PayrollRunCandidateFilter>(PayrollRunCandidateFilter.All);
    readonly filterText = signal<string>('');

    readonly allCount = signal<number>(0);
    readonly processedCount = signal<number>(0);
    readonly pendingCount = signal<number>(0);

    /** Legacy lblPopupTimePeriod, as the server worked it out. */
    readonly periodText = signal<string>('');

    /** Codes ticked so far, kept across pages and filters. Lower-cased: legacy matched codes case-insensitively. */
    readonly selected = signal<Set<string>>(new Set<string>());
    readonly selectedCount = computed(() => this.selected().size);

    show(args: TimeEngineEmployeePickerArgs): void {
        this.args.set(args);
        this.statusFilter.set(PayrollRunCandidateFilter.All);
        this.filterText.set('');
        this.periodText.set('');
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

        this._timeEngineService
            .getTimeEngineRunCandidates(input)
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(result.items);
                this.allCount.set(result.allCount);
                this.processedCount.set(result.processedCount);
                this.pendingCount.set(result.pendingCount);
                this.periodText.set(result.periodText);
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

    /** Legacy SelectAllTimeEmployees: ticks (or unticks) every row the current filter and search show, on every page. */
    selectAll(select: boolean): void {
        this.working.set(true);
        this._timeEngineService
            .getTimeEngineRunCandidateCodes(this.buildInput())
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
        this._timeEngineService
            .getTimeEngineRunCandidateCodes(input)
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
        this.getCandidates({
            first: 0,
            rows: this.paginator()?.rows || this.primengTableHelper.defaultRecordsCountPerPage,
        } as LazyLoadEvent);
    }

    private buildInput(): GetTimeEngineRunCandidatesInput {
        const args = this.args();
        return new GetTimeEngineRunCandidatesInput({
            payrollAreaName: args.payrollAreaName,
            startDate: args.startDate ? DateTime.fromISO(args.startDate) : undefined,
            endDate: args.endDate ? DateTime.fromISO(args.endDate) : undefined,
            statusFilter: this.statusFilter(),
            filter: this.filterText() ? this.filterText() : undefined,
            selectedEmployeeCodes: Array.from(this.selected()).join(','),
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

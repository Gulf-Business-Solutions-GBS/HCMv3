import { Component, computed, output, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LazyLoadEvent } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { DateTime } from 'luxon';
import { finalize } from 'rxjs/operators';

import { AppComponentBase } from '@shared/common/app-component-base';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import {
    GetCostingFileCandidatesInput,
    PayrollRunCandidateDto,
    PayrollRunCandidateFilter,
    PayrollRunCandidateStatus,
    PayrollRunType,
} from '@shared/service-proxies/service-proxies';
import { FinanceScreenApi } from './finance-screen-api';

/** What a finance screen hands the picker when it opens. */
export interface CostingEmployeePickerArgs {
    api: FinanceScreenApi;
    payrollAreaName: string;
    payrollRunType: PayrollRunType;
    /** The period end date (legacy txtEndDate). */
    payInDate: DateTime;
    /** The comma-separated codes already in the Employee Code box. */
    selectedCodes?: string;
}

/**
 * Legacy "CostFile Employee Selection" popup (uscCostingFile.ascx popupEmployees, the "..." beside Employee Code;
 * .ascx.cs BuildEmployeePopupList lines 2632-2775 and the filter / select-all / OK handlers to line 2910). Same
 * behaviour and layout as the Payroll Engine picker (PayrollRunEmployeePickerModalComponent), on the finance
 * screens' own endpoints. Legacy lists nobody for an off-cycle run; so does this.
 */
@Component({
    selector: 'costingEmployeePickerModal',
    templateUrl: './costing-employee-picker-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, TableModule, PaginatorModule, LocalizePipe, BusyIfDirective, LuxonFormatPipe],
})
export class CostingEmployeePickerModalComponent extends AppComponentBase {
    readonly modal = viewChild<ModalDirective>('pickerModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    /** Emits the selected employee codes, comma-separated, when the user presses OK. */
    readonly codesSelected = output<string>();

    readonly PayrollRunCandidateFilter = PayrollRunCandidateFilter;
    readonly PayrollRunCandidateStatus = PayrollRunCandidateStatus;

    readonly active = signal<boolean>(false);
    readonly working = signal<boolean>(false);
    readonly args = signal<CostingEmployeePickerArgs | undefined>(undefined);

    readonly statusFilter = signal<PayrollRunCandidateFilter>(PayrollRunCandidateFilter.All);
    readonly filterText = signal<string>('');

    readonly allCount = signal<number>(0);
    readonly processedCount = signal<number>(0);
    readonly pendingCount = signal<number>(0);

    /** Codes ticked so far, kept across pages and filters (legacy kept them in session). Lower-cased. */
    readonly selected = signal<Set<string>>(new Set<string>());
    readonly selectedCount = computed(() => this.selected().size);

    show(args: CostingEmployeePickerArgs): void {
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
        const args = this.args();
        if (!args) {
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

        args.api
            .getCostingFileCandidates(input)
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

    /** Legacy SelectAllEmployees: every row the current filter and search show, on every page. */
    selectAll(select: boolean): void {
        const args = this.args();
        if (!args) {
            return;
        }

        this.working.set(true);
        args.api
            .getCostingFileCandidateCodes(this.buildInput())
            .pipe(finalize(() => this.working.set(false)))
            .subscribe((codes) => {
                const next = new Set(this.selected());
                (codes || []).forEach((code) => (select ? next.add(this.key(code)) : next.delete(this.key(code))));
                this.selected.set(next);

                if (this.statusFilter() === PayrollRunCandidateFilter.Selected) {
                    this.reloadFromFirstPage();
                }
            });
    }

    /** Legacy "Apply Selection": the listed employees that are ticked, as one comma-separated string. */
    ok(): void {
        const args = this.args();
        if (!args) {
            return;
        }

        const input = this.buildInput();
        input.statusFilter = PayrollRunCandidateFilter.All;
        input.filter = undefined;

        this.working.set(true);
        args.api
            .getCostingFileCandidateCodes(input)
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

    private buildInput(): GetCostingFileCandidatesInput {
        const args = this.args();
        return new GetCostingFileCandidatesInput({
            payrollAreaName: args.payrollAreaName,
            payrollRunType: args.payrollRunType,
            payInDate: args.payInDate,
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

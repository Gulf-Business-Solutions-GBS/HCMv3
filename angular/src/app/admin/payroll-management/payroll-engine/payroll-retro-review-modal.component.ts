import { Component, computed, inject, output, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DateTime } from 'luxon';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { LazyLoadEvent } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { finalize } from 'rxjs/operators';

import { AppComponentBase } from '@shared/common/app-component-base';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import {
    GetPayrollRetroReviewEmployeesInput,
    PayrollRetroDateChangeDto,
    PayrollRetroReviewEmployeeDto,
    PayrollRetroReviewFilter,
    PayrollRetroReviewServiceProxy,
    UpdatePayrollRetroDatesInput,
} from '@shared/service-proxies/service-proxies';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';

/** Legacy's four radio tabs. 'Changed' lists edits not yet saved, which only this dialog knows about. */
export type RetroTab = 'All' | 'Retro' | 'NoRetro' | 'Changed';

/** A grid row as shown: the saved values, overlaid with any unsaved edit. Dates are ISO yyyy-MM-dd. */
interface RetroRow {
    employeeId: number;
    employeeCode: string;
    employeeName: string;
    originalRetroFromDate: string | null;
    retroFromDate: string | null;
    originalReason: string;
    reason: string;
    hasRetro: boolean;
    retroMonths: number;
}

/**
 * Port of the legacy Payroll Engine screen's Retro popup
 * (HCM10.Web/UserControls/PayrollManagement/uscPayrollEngine.ascx: popupRetro, popupRetroUpdate, popupRetroConfirm;
 * .ascx.cs region "Retro Popup").
 *
 * As legacy, edits are kept in the dialog ("Apply") until Save, which asks for confirmation and saves every changed
 * employee in one call. Legacy's second popup (Update Retro / Update Selected) is an inline panel here.
 */
@Component({
    selector: 'payrollRetroReviewModal',
    templateUrl: './payroll-retro-review-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        BusyIfDirective,
        ButtonBusyDirective,
        DateInputDirective,
        LocalizePipe,
    ],
})
export class PayrollRetroReviewModalComponent extends AppComponentBase {
    private _retroService = inject(PayrollRetroReviewServiceProxy);

    /** Raised after a successful save, so the screen can refresh its "Show Retro (n)" count. */
    readonly modalSave = output<void>();

    readonly modal = viewChild<ModalDirective>('modal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly canSave = this.isGranted('Pages.PayrollManagement.PayrollEngine.Run');

    readonly payrollAreaName = signal<string>('');
    readonly tab = signal<RetroTab>('Retro');
    readonly filterText = signal<string>('');
    readonly isShown = signal<boolean>(false);
    readonly saving = signal<boolean>(false);

    readonly allCount = signal<number>(0);
    readonly retroCount = signal<number>(0);
    readonly noRetroCount = signal<number>(0);
    readonly periodStart = signal<string | null>(null);
    readonly periodEnd = signal<string | null>(null);

    /** Unsaved edits by employee id (legacy RetroPopupList rows whose IsChanged is true). */
    readonly pending = signal<Map<number, RetroRow>>(new Map());
    readonly changedCount = computed(() => this.pending().size);

    /** Selected rows, kept across pages and tabs (dataKey = employeeId). */
    readonly selected = signal<RetroRow[]>([]);

    // The update panel (legacy popupRetroUpdate).
    readonly editorOpen = signal<boolean>(false);
    /** Null = "Update Selected"; otherwise the single employee being edited. */
    readonly editorRow = signal<RetroRow | null>(null);
    readonly editorDate = signal<string>('');
    readonly editorReason = signal<string>('');
    readonly editorError = signal<string>('');

    readonly editorTitle = computed(() =>
        this.editorRow() ? this.l('PayrollRetroUpdateTitle') : this.l('PayrollRetroUpdateSelected'),
    );

    readonly editorSubtitle = computed(() => {
        const row = this.editorRow();
        return row
            ? `${row.employeeCode} - ${row.employeeName ?? ''}`
            : this.l('PayrollRetroEmployeesSelected', this.selected().length);
    });

    show(payrollAreaName: string): void {
        // Legacy LOAD: a fresh list, nothing pending, opened on the Retro tab.
        this.payrollAreaName.set(payrollAreaName);
        this.tab.set('Retro');
        this.filterText.set('');
        this.pending.set(new Map());
        this.selected.set([]);
        this.closeEditor();
        this.primengTableHelper.records.set([]);
        this.primengTableHelper.totalRecordsCount.set(0);
        this.modal().show();
    }

    shown(): void {
        this.isShown.set(true);
        this.getRecords();
    }

    close(): void {
        this.isShown.set(false);
        this.closeEditor();
        this.modal().hide();
    }

    setTab(tab: RetroTab): void {
        this.tab.set(tab);
        this.refresh();
    }

    /** Reloads from the first page. */
    refresh(): void {
        const paginator = this.paginator();
        if (this.tab() !== 'Changed' && paginator && paginator.getPage() > 0) {
            // Changing page fires (onPageChange), which reloads.
            paginator.changePage(0);
            return;
        }

        this.getRecords();
    }

    getRecordsIfNeeds(event?: LazyLoadEvent): void {
        if (!this.isShown()) {
            return;
        }

        this.getRecords(event);
    }

    getRecords(event?: LazyLoadEvent): void {
        if (this.tab() === 'Changed') {
            this.showChanged();
            return;
        }

        const input = new GetPayrollRetroReviewEmployeesInput();
        input.payrollAreaName = this.payrollAreaName();
        input.retroFilter =
            this.tab() === 'All'
                ? PayrollRetroReviewFilter.All
                : this.tab() === 'NoRetro'
                  ? PayrollRetroReviewFilter.NoRetro
                  : PayrollRetroReviewFilter.Retro;
        input.filter = this.filterText();
        input.sorting = this.dataTable() ? this.primengTableHelper.getSorting(this.dataTable()) : '';
        input.skipCount = this.primengTableHelper.getSkipCount(this.paginator(), event);
        input.maxResultCount = this.primengTableHelper.getMaxResultCount(this.paginator(), event);

        this.primengTableHelper.showLoadingIndicator();
        this._retroService
            .getRetroReviewEmployees(input)
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.periodStart.set(result.periodStartDate?.toISODate() ?? null);
                this.periodEnd.set(result.periodEndDate?.toISODate() ?? null);
                this.allCount.set(result.allCount);
                this.retroCount.set(result.retroCount);
                this.noRetroCount.set(result.noRetroCount);

                const pending = this.pending();
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(
                    (result.items ?? []).map((dto) => pending.get(dto.employeeId) ?? this.toRow(dto)),
                );
            });
    }

    // ------------------------------------------------------------------ the update panel

    /** Legacy LOAD|code: edit one employee, starting from the date shown in the grid. */
    editOne(row: RetroRow): void {
        this.editorRow.set(row);
        this.editorDate.set(row.retroFromDate ?? '');
        // Legacy keeps the typed reason only for a row already changed in this dialog.
        this.editorReason.set(this.pending().has(row.employeeId) ? row.reason : '');
        this.editorError.set('');
        this.editorOpen.set(true);
    }

    /** Legacy LOADSELECTED: one new date and reason for every selected employee. */
    editSelected(): void {
        if (this.selected().length === 0) {
            this.message.warn(this.l('PayrollRetroSelectAtLeastOne'));
            return;
        }

        this.editorRow.set(null);
        this.editorDate.set('');
        this.editorReason.set('');
        this.editorError.set('');
        this.editorOpen.set(true);
    }

    closeEditor(): void {
        this.editorOpen.set(false);
        this.editorRow.set(null);
        this.editorError.set('');
    }

    /** Legacy ApplyRetroUpdatePopup: changes the rows in the dialog only; Save writes them. */
    applyEditor(): void {
        const reason = (this.editorReason() ?? '').trim();
        if (!reason) {
            this.editorError.set(this.l('PayrollRetroReasonRequired'));
            return;
        }

        const single = this.editorRow();
        const targets = single ? [single] : this.selected();
        if (targets.length === 0) {
            this.editorError.set(this.l('PayrollRetroSelectAtLeastOne'));
            return;
        }

        const date = this.editorDate() || null;
        const pending = new Map(this.pending());

        for (const target of targets) {
            const row: RetroRow = { ...target, retroFromDate: date, reason };
            this.recalculate(row);

            // Legacy IsChanged compares with the saved date: setting it back to that is no longer a change.
            if (row.retroFromDate === row.originalRetroFromDate) {
                pending.delete(row.employeeId);
            } else {
                pending.set(row.employeeId, row);
            }
        }

        this.pending.set(pending);
        this.closeEditor();
        this.notify.info(this.l('PayrollRetroAppliedToEmployees', targets.length));
        this.redrawCurrentPage();
    }

    // ------------------------------------------------------------------ save

    /** Legacy PREPARESAVE + popupRetroConfirm + SAVE. */
    save(): void {
        const changes = Array.from(this.pending().values());
        if (changes.length === 0) {
            this.message.warn(this.l('PayrollRetroNoChanges'));
            return;
        }

        if (changes.some((c) => !(c.reason ?? '').trim())) {
            this.message.warn(this.l('PayrollRetroReasonRequired'));
            return;
        }

        this.message.confirm(
            this.l('PayrollRetroConfirmMessage', changes.length),
            this.l('PayrollRetroConfirmTitle'),
            (isConfirmed) => {
                if (!isConfirmed) {
                    return;
                }

                const input = new UpdatePayrollRetroDatesInput();
                input.payrollAreaName = this.payrollAreaName();
                input.changes = changes.map((c) => {
                    const change = new PayrollRetroDateChangeDto();
                    change.employeeId = c.employeeId;
                    change.retroFromDate = c.retroFromDate ? DateTime.fromISO(c.retroFromDate) : undefined;
                    change.reason = c.reason;
                    return change;
                });

                this.saving.set(true);
                this._retroService
                    .updateRetroDates(input)
                    .pipe(finalize(() => this.saving.set(false)))
                    .subscribe((count) => {
                        this.notify.success(this.l('PayrollRetroSavedSuccessfully', count));
                        this.pending.set(new Map());
                        this.selected.set([]);
                        if (this.tab() === 'Changed') {
                            this.tab.set('Retro');
                        }
                        this.refresh();
                        this.modalSave.emit();
                    });
            },
        );
    }

    // ------------------------------------------------------------------ helpers

    isPending(row: RetroRow): boolean {
        return this.pending().has(row.employeeId);
    }

    /** dd/MM/yyyy for an ISO date, as legacy's grid column. */
    formatDate(iso: string | null): string {
        return iso ? DateTime.fromISO(iso).toFormat('dd/MM/yyyy') : '';
    }

    private showChanged(): void {
        const rows = Array.from(this.pending().values()).sort((a, b) =>
            (a.employeeCode ?? '').localeCompare(b.employeeCode ?? ''),
        );
        this.primengTableHelper.records.set(rows);
        this.primengTableHelper.totalRecordsCount.set(rows.length);
    }

    /** Re-shows the current rows with the latest edits, without a server round trip. */
    private redrawCurrentPage(): void {
        if (this.tab() === 'Changed') {
            this.showChanged();
            return;
        }

        const pending = this.pending();
        this.primengTableHelper.records.set(
            this.primengTableHelper
                .records()
                .map((r: RetroRow) => pending.get(r.employeeId) ?? this.restore(r)),
        );
        this.selected.set(this.selected().map((s) => pending.get(s.employeeId) ?? this.restore(s)));
    }

    private toRow(dto: PayrollRetroReviewEmployeeDto): RetroRow {
        const date = dto.retroFromDate?.toISODate() ?? null;
        return {
            employeeId: dto.employeeId,
            employeeCode: dto.employeeCode,
            employeeName: dto.employeeName,
            originalRetroFromDate: date,
            retroFromDate: date,
            originalReason: dto.reason ?? '',
            reason: dto.reason ?? '',
            hasRetro: dto.hasRetro,
            retroMonths: dto.retroMonths,
        };
    }

    /** A row whose edit was undone shows its saved values again. */
    private restore(row: RetroRow): RetroRow {
        if (row.retroFromDate === row.originalRetroFromDate && row.reason === row.originalReason) {
            return row;
        }

        const restored: RetroRow = { ...row, retroFromDate: row.originalRetroFromDate, reason: row.originalReason };
        this.recalculate(restored);
        return restored;
    }

    /** Legacy RetroPopupModel.HasRetro / TotalRetroMonth, for an edited date. */
    private recalculate(row: RetroRow): void {
        const start = this.periodStart();
        const end = this.periodEnd();

        row.hasRetro = !!row.retroFromDate && (!start || row.retroFromDate < start);

        if (!row.hasRetro || !end) {
            row.retroMonths = 0;
            return;
        }

        const from = DateTime.fromISO(row.retroFromDate as string);
        const to = DateTime.fromISO(end);
        const months = (to.year - from.year) * 12 + to.month - from.month;
        row.retroMonths = months > 0 ? months : 0;
    }
}

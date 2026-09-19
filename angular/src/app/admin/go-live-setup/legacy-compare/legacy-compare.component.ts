import { Component, OnDestroy, computed, inject, input, output, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { DateTime } from 'luxon';
import { finalize } from 'rxjs/operators';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { FileDownloadService } from '@shared/utils/file-download.service';
import {
    BillingType,
    GoLiveCompareCause,
    GoLiveCompareStatus,
    GoLiveLegacyCompareExportInput,
    GoLiveLegacyCompareInput,
    GoLiveLegacyCompareLineDto,
    GoLiveLegacyCompareOutput,
    GoLiveLegacyPaymentLineDto,
    GoLiveLegacyPaymentsInput,
    GoLivePayrollResultsInput,
    GoLiveSetupServiceProxy,
} from '@shared/service-proxies/service-proxies';

type StatusFilter = 'all' | 'matched' | 'notMatched';

type Tab = 'upload' | 'summary' | 'lines';

/**
 * Compare with legacy salary: the legacy payroll's registers for the month -- exported by a read-only query the
 * window hands out -- set beside HCMv3's in one grid, rate, number and amount side by side, each cell coloured by
 * whether its pair agrees.
 *
 * <p>A line is matched only when all three agree, and a wage type only when every employee's line does -- the
 * server decides per value, so the colours and the counts cannot disagree.</p>
 *
 * <p>Salaries are confidential. The file goes to the server only to be compared and is not kept there; the
 * result lives in this window and is dropped when it closes.</p>
 */
@Component({
    selector: 'go-live-legacy-compare',
    templateUrl: './legacy-compare.component.html',
    styleUrl: './legacy-compare.component.css',
    imports: [LocalizePipe, DecimalPipe],
})
export class LegacyCompareComponent extends AppComponentBase implements OnDestroy {
    private readonly proxy = inject(GoLiveSetupServiceProxy);
    private readonly fileDownload = inject(FileDownloadService);

    /** The month end being compared. */
    readonly monthEnd = input.required<DateTime>();
    readonly payrollAreaName = input.required<string>();
    readonly closed = output<void>();

    readonly Cause = GoLiveCompareCause;

    readonly causes = [
        GoLiveCompareCause.Data,
        GoLiveCompareCause.Days,
        GoLiveCompareCause.Logic,
        GoLiveCompareCause.Setup,
        GoLiveCompareCause.Rounding,
        GoLiveCompareCause.FollowsOtherLines,
    ];
    readonly groups = [BillingType.Earning, BillingType.Deduction, BillingType.Provision, BillingType.TimeType];
    readonly pageSizes = [50, 100, 200];

    readonly month = computed(() => this.monthEnd().toFormat('MMMM yyyy'));

    /** Upload first; Summary and Lines open once there is a result, and a comparison lands on Summary. */
    readonly tab = signal<Tab>('upload');

    selectTab(tab: Tab): void {
        if (tab === 'upload' || this.result()) {
            this.tab.set(tab);
        }
    }

    // ---------------------------------------------------------------- step 1: the query

    readonly gettingQuery = signal(false);

    downloadQuery(): void {
        this.withQuery((sql, fileName) => {
            const url = URL.createObjectURL(new Blob([sql], { type: 'application/sql' }));
            const link = document.createElement('a');
            link.href = url;
            link.download = fileName;
            link.click();
            setTimeout(() => URL.revokeObjectURL(url), 60000);
        });
    }

    copyQuery(): void {
        this.withQuery((sql) =>
            navigator.clipboard.writeText(sql).then(() => this.notify.success(this.l('GoLiveCompareQueryCopied')))
        );
    }

    private withQuery(use: (sql: string, fileName: string) => void): void {
        const input = new GoLivePayrollResultsInput();
        input.payDate = this.monthEnd().plus({ hours: 12 });
        input.payrollAreaName = this.payrollAreaName();

        this.gettingQuery.set(true);
        this.proxy
            .getLegacySalaryExportQuery(input)
            .pipe(finalize(() => this.gettingQuery.set(false)))
            .subscribe((query) => use(query.sql ?? '', query.fileName ?? 'Legacy Salary Register.sql'));
    }

    // ---------------------------------------------------------------- step 2: the file

    readonly file = signal<File | null>(null);
    readonly comparing = signal(false);
    readonly result = signal<GoLiveLegacyCompareOutput | null>(null);

    onFile(event: Event): void {
        this.file.set((event.target as HTMLInputElement).files?.[0] ?? null);
    }

    async compare(): Promise<void> {
        const file = this.file();
        if (!file || this.comparing()) {
            return;
        }

        this.comparing.set(true);
        let content: string;
        try {
            content = await this.base64(file);
        } catch {
            this.comparing.set(false);
            return;
        }

        const input = new GoLiveLegacyCompareInput();
        input.payDate = this.monthEnd().plus({ hours: 12 });
        input.payrollAreaName = this.payrollAreaName();
        input.fileName = file.name;
        input.base64Content = content;

        this.proxy
            .compareWithLegacySalary(input)
            .pipe(finalize(() => this.comparing.set(false)))
            .subscribe((result) => {
                this.result.set(result);
                this.addedPayments.set(new Set<string>());
                this.resetFilters();
                this.tab.set('summary');
            });
    }

    private base64(file: File): Promise<string> {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = () => resolve(String(reader.result).replace(/^data:[^,]*,/, ''));
            reader.onerror = () => reject(reader.error);
            reader.readAsDataURL(file);
        });
    }

    // ---------------------------------------------------------------- counts

    /** Every wage type, by code, and whether each is matched -- only when every employee's line for it is. */
    readonly wageTypes = computed(() => {
        const byCode = new Map<string, { code: string; name: string; lines: number; linesNotMatched: number }>();
        for (const l of this.result()?.lines ?? []) {
            const code = l.wageTypeCode ?? '';
            let row = byCode.get(code);
            if (!row) {
                row = { code, name: l.wageTypeName ?? '', lines: 0, linesNotMatched: 0 };
                byCode.set(code, row);
            }
            row.lines++;
            row.linesNotMatched += l.status === GoLiveCompareStatus.Matched ? 0 : 1;
        }
        const all = [...byCode.values()].sort((a, b) => a.code.localeCompare(b.code));
        return {
            total: all.length,
            matched: all.filter((w) => w.linesNotMatched === 0),
            notMatched: all.filter((w) => w.linesNotMatched > 0),
        };
    });

    /** Every employee in the comparison, and whether each is matched -- only when every one of their lines is. */
    readonly employees = computed(() => {
        const byCode = new Map<string, { code: string; lines: number; linesNotMatched: number }>();
        for (const l of this.result()?.lines ?? []) {
            const code = l.employeeCode ?? '';
            let row = byCode.get(code);
            if (!row) {
                row = { code, lines: 0, linesNotMatched: 0 };
                byCode.set(code, row);
            }
            row.lines++;
            row.linesNotMatched += l.status === GoLiveCompareStatus.Matched ? 0 : 1;
        }
        const all = [...byCode.values()].sort((a, b) => a.code.localeCompare(b.code, undefined, { numeric: true }));
        const matched = all.filter((e) => e.linesNotMatched === 0);
        return {
            total: all.length,
            matched,
            notMatched: all.filter((e) => e.linesNotMatched > 0),
            /** Share of employees whose whole salary matched, 0 to 100. */
            matchedPercent: all.length === 0 ? 0 : Math.round((matched.length / all.length) * 1000) / 10,
        };
    });

    /** From an employee on the Summary tab to their lines on the Lines tab. */
    showEmployeeLines(code: string): void {
        this.resetFilters();
        this.search.set(code);
        this.tab.set('lines');
    }

    /** From a wage type on the Summary tab to its lines on the Lines tab. */
    showWageTypeLines(code: string): void {
        this.resetFilters();
        this.wageType.set(code);
        this.tab.set('lines');
    }

    /** From a count on the Summary tab to the lines it counts. */
    showLines(status: StatusFilter): void {
        this.resetFilters();
        this.status.set(status);
        this.tab.set('lines');
    }

    // ---------------------------------------------------------------- filters

    readonly search = signal('');
    readonly status = signal<StatusFilter>('all');
    readonly cause = signal<GoLiveCompareCause | null>(null);
    readonly group = signal<BillingType | null>(null);
    readonly wageType = signal('');
    readonly page = signal(1);
    readonly pageSize = signal(50);

    readonly wageTypeOptions = computed(() => {
        const seen = new Map<string, string>();
        for (const line of this.result()?.lines ?? []) {
            if (!seen.has(line.wageTypeCode ?? '')) {
                seen.set(line.wageTypeCode ?? '', line.wageTypeName ?? '');
            }
        }
        return [...seen.entries()].sort((a, b) => a[0].localeCompare(b[0])).map(([code, name]) => ({ code, name }));
    });

    readonly filteredLines = computed(() => {
        const query = this.search().trim().toLowerCase();
        const status = this.status();
        const cause = this.cause();
        const group = this.group();
        const wage = this.wageType();

        return (this.result()?.lines ?? []).filter((l) => {
            const matched = l.status === GoLiveCompareStatus.Matched;
            return (
                (!query || (l.employeeCode ?? '').toLowerCase().includes(query)) &&
                (status === 'all' || (status === 'matched') === matched) &&
                (cause === null || l.cause === cause) &&
                (group === null || l.billingType === group) &&
                (!wage || l.wageTypeCode === wage)
            );
        });
    });

    readonly pageCount = computed(() => Math.max(1, Math.ceil(this.filteredLines().length / this.pageSize())));
    readonly currentPage = computed(() => Math.min(this.page(), this.pageCount()));

    readonly pagedLines = computed(() => {
        const start = (this.currentPage() - 1) * this.pageSize();
        return this.filteredLines().slice(start, start + this.pageSize());
    });

    readonly showingFrom = computed(() =>
        this.filteredLines().length === 0 ? 0 : (this.currentPage() - 1) * this.pageSize() + 1
    );
    readonly showingTo = computed(() => Math.min(this.currentPage() * this.pageSize(), this.filteredLines().length));

    readonly exporting = signal(false);

    // ---------------------------------------------------------------- payments for what only legacy paid

    /** Lines a payment was added for in this window, by employee and wage type. */
    readonly addedPayments = signal(new Set<string>());
    readonly addingPayments = signal(false);

    paymentKey(line: GoLiveLegacyCompareLineDto): string {
        return `${line.employeeCode}|${line.wageTypeCode}`;
    }

    /** The filtered lines a payment can still be added for. */
    readonly payableLines = computed(() =>
        this.filteredLines().filter((l) => l.canAddPayment && !this.addedPayments().has(this.paymentKey(l)))
    );

    // The preview a line's button opens: what is compared, and the payment to add, which can be changed.
    readonly previewLine = signal<GoLiveLegacyCompareLineDto | null>(null);
    readonly previewAmount = signal('');

    readonly previewAmountValue = computed(() => {
        const value = Number(this.previewAmount().replace(/,/g, '').trim());
        return Number.isFinite(value) ? value : NaN;
    });

    openPaymentPreview(line: GoLiveLegacyCompareLineDto): void {
        if (!line.canAddPayment) {
            return;
        }
        this.previewLine.set(line);
        this.previewAmount.set(line.suggestedPaymentAmount === undefined ? '' : String(line.suggestedPaymentAmount));
    }

    onPreviewAmount(event: Event): void {
        this.previewAmount.set((event.target as HTMLInputElement).value);
    }

    closePaymentPreview(): void {
        if (!this.addingPayments()) {
            this.previewLine.set(null);
        }
    }

    confirmPaymentPreview(): void {
        const line = this.previewLine();
        const amount = this.previewAmountValue();
        if (!line || !Number.isFinite(amount) || amount === 0) {
            return;
        }
        this.addPayments([{ line, amount }], () => this.previewLine.set(null));
    }

    /** Every filtered line a payment can be added for, after a confirmation that says how many and for which month. */
    addAllPayments(): void {
        const lines = this.payableLines();
        if (lines.length === 0) {
            return;
        }
        this.message.confirm(
            this.l('GoLiveCompareAddAllPaymentsConfirm', lines.length, this.month()),
            this.l('AreYouSure'),
            (confirmed) => {
                if (confirmed) {
                    this.addPayments(lines.map((line) => ({ line, amount: line.suggestedPaymentAmount ?? 0 })));
                }
            }
        );
    }

    private addPayments(items: { line: GoLiveLegacyCompareLineDto; amount: number }[], done?: () => void): void {
        if (this.addingPayments() || items.length === 0) {
            return;
        }
        const lines = items.map((i) => i.line);

        const input = new GoLiveLegacyPaymentsInput();
        input.payDate = this.monthEnd().plus({ hours: 12 });
        input.lines = items.map(({ line, amount }) => {
            const dto = new GoLiveLegacyPaymentLineDto();
            dto.employeeCode = line.employeeCode;
            dto.wageTypeCode = line.wageTypeCode;
            dto.amount = amount;
            return dto;
        });

        this.addingPayments.set(true);
        this.proxy
            .addLegacyPayments(input)
            .pipe(finalize(() => this.addingPayments.set(false)))
            .subscribe((out) => {
                const refused = new Set((out.refused ?? []).map((r) => `${r.employeeCode}|${r.wageTypeCode}`));
                this.addedPayments.update((added) => {
                    const next = new Set(added);
                    lines.filter((l) => !refused.has(this.paymentKey(l))).forEach((l) => next.add(this.paymentKey(l)));
                    return next;
                });

                if (out.added + out.updated + out.alreadyThere > 0) {
                    this.notify.success(this.l('GoLiveComparePaymentsDone', out.added, out.updated, out.alreadyThere));
                }
                done?.();
                if (out.refused?.length) {
                    this.message.warn(
                        out.refused.map((r) => `${r.employeeCode} / ${r.wageTypeCode}: ${r.reason}`).join('\n'),
                        this.l('GoLiveComparePaymentsRefused', out.refused.length, '')
                    );
                }
            });
    }

    /** The lines as the grid shows them, filters applied, as Excel for a customer to check. */
    exportLines(): void {
        const input = new GoLiveLegacyCompareExportInput();
        input.payMonth = this.monthEnd();
        input.lines = this.filteredLines();

        this.exporting.set(true);
        this.proxy
            .exportLegacyComparison(input)
            .pipe(finalize(() => this.exporting.set(false)))
            .subscribe((file) => this.fileDownload.downloadTempFile(file));
    }

    onSearch(event: Event): void {
        this.search.set((event.target as HTMLInputElement).value);
        this.page.set(1);
    }

    onStatus(event: Event): void {
        this.showStatus((event.target as HTMLSelectElement).value as StatusFilter);
    }

    onCause(event: Event): void {
        const value = (event.target as HTMLSelectElement).value;
        this.cause.set(value === '' ? null : (Number(value) as GoLiveCompareCause));
        this.page.set(1);
    }

    onGroup(event: Event): void {
        const value = (event.target as HTMLSelectElement).value;
        this.group.set(value === '' ? null : (Number(value) as BillingType));
        this.page.set(1);
    }

    onWageType(event: Event): void {
        this.wageType.set((event.target as HTMLSelectElement).value);
        this.page.set(1);
    }

    onPageSize(event: Event): void {
        this.pageSize.set(Number((event.target as HTMLSelectElement).value));
        this.page.set(1);
    }

    goToPage(page: number): void {
        this.page.set(Math.max(1, Math.min(page, this.pageCount())));
    }

    showStatus(status: StatusFilter): void {
        this.status.set(status);
        this.page.set(1);
    }

    resetFilters(): void {
        this.search.set('');
        this.status.set('all');
        this.cause.set(null);
        this.group.set(null);
        this.wageType.set('');
        this.page.set(1);
    }

    // ---------------------------------------------------------------- labels

    isMatched(status: GoLiveCompareStatus): boolean {
        return status === GoLiveCompareStatus.Matched;
    }

    causeKey(cause: GoLiveCompareCause): string {
        return 'GoLiveCompareCause' + GoLiveCompareCause[cause];
    }

    groupKey(group: BillingType | undefined): string {
        return group === undefined || group === null ? '' : 'GoLiveRegisterGroup' + BillingType[group];
    }

    close(): void {
        this.result.set(null);
        this.file.set(null);
        this.closed.emit();
    }

    ngOnDestroy(): void {
        // Nothing of the salaries outlives the window.
        this.result.set(null);
        this.file.set(null);
    }
}

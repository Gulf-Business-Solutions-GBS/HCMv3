import { Component, OnDestroy, computed, inject, input, output, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { DateTime } from 'luxon';
import { finalize } from 'rxjs/operators';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import {
    BillingType,
    GoLiveV2CompareCause,
    GoLiveV2CompareStatus,
    GoLiveV2LegacyCompareInput,
    GoLiveV2LegacyCompareLineDto,
    GoLiveV2LegacyCompareOutput,
    GoLiveV2PayrollResultsInput,
    GoLiveV2ServiceProxy,
} from '@shared/service-proxies/service-proxies';

/** One wage type across every employee: how often it matched, and how far apart the amounts are in total. */
interface WageTypeSummary {
    code: string;
    name: string;
    group: BillingType | undefined;
    lines: number;
    matched: number;
    different: number;
    onlyInLegacy: number;
    onlyInHcmv3: number;
    difference: number;
}

/**
 * Compare with legacy salary: the legacy payroll's registers for the month -- exported by a read-only query the
 * window hands out -- set beside HCMv3's, wage type by wage type, with the likely reason for each difference.
 *
 * <p>Salaries are confidential. The file goes to the server only to be compared and is not kept there; the
 * result lives in this window and is dropped when it closes.</p>
 */
@Component({
    selector: 'v2-legacy-compare',
    templateUrl: './v2-legacy-compare.component.html',
    styleUrl: './v2-legacy-compare.component.css',
    imports: [LocalizePipe, DecimalPipe],
})
export class V2LegacyCompareComponent extends AppComponentBase implements OnDestroy {
    private readonly proxy = inject(GoLiveV2ServiceProxy);

    /** The month end being compared. */
    readonly monthEnd = input.required<DateTime>();
    readonly payrollAreaName = input.required<string>();
    readonly closed = output<void>();

    readonly Status = GoLiveV2CompareStatus;
    readonly Cause = GoLiveV2CompareCause;
    readonly BillingType = BillingType;

    readonly statuses = [
        GoLiveV2CompareStatus.Matched,
        GoLiveV2CompareStatus.Different,
        GoLiveV2CompareStatus.OnlyInLegacy,
        GoLiveV2CompareStatus.OnlyInHcmv3,
    ];
    readonly causes = [
        GoLiveV2CompareCause.Data,
        GoLiveV2CompareCause.Days,
        GoLiveV2CompareCause.Logic,
        GoLiveV2CompareCause.Setup,
        GoLiveV2CompareCause.Rounding,
        GoLiveV2CompareCause.FollowsOtherLines,
    ];
    readonly groups = [BillingType.Earning, BillingType.Deduction, BillingType.Provision, BillingType.TimeType];
    readonly pageSizes = [50, 100, 200];

    readonly month = computed(() => this.monthEnd().toFormat('MMMM yyyy'));

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
        this.withQuery((sql) => navigator.clipboard.writeText(sql).then(() => this.notify.success(this.l('GoLiveV2CompareQueryCopied'))));
    }

    private withQuery(use: (sql: string, fileName: string) => void): void {
        const input = new GoLiveV2PayrollResultsInput();
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
    readonly result = signal<GoLiveV2LegacyCompareOutput | null>(null);

    onFile(event: Event): void {
        const picked = (event.target as HTMLInputElement).files?.[0] ?? null;
        this.file.set(picked);
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

        const input = new GoLiveV2LegacyCompareInput();
        input.payDate = this.monthEnd().plus({ hours: 12 });
        input.payrollAreaName = this.payrollAreaName();
        input.fileName = file.name;
        input.base64Content = content;

        this.proxy
            .compareWithLegacySalary(input)
            .pipe(finalize(() => this.comparing.set(false)))
            .subscribe((result) => {
                this.result.set(result);
                this.resetFilters();
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

    // ---------------------------------------------------------------- filters

    readonly tab = signal<'lines' | 'wages'>('lines');
    readonly search = signal('');
    readonly status = signal<GoLiveV2CompareStatus | null>(null);
    readonly cause = signal<GoLiveV2CompareCause | null>(null);
    readonly group = signal<BillingType | null>(null);
    readonly wageType = signal('');
    readonly hideMatched = signal(true);
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
        const hideMatched = this.hideMatched() && status !== GoLiveV2CompareStatus.Matched;

        return (this.result()?.lines ?? []).filter(
            (l) =>
                (!query ||
                    (l.employeeCode ?? '').toLowerCase().includes(query) ||
                    (l.employeeName ?? '').toLowerCase().includes(query)) &&
                (status === null || l.status === status) &&
                (cause === null || l.cause === cause) &&
                (group === null || l.billingType === group) &&
                (!wage || l.wageTypeCode === wage) &&
                (!hideMatched || l.status !== GoLiveV2CompareStatus.Matched)
        );
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

    /** Every wage type in the result, worst first: the ones that differ most often lead. */
    readonly wageTypeSummary = computed<WageTypeSummary[]>(() => {
        const byCode = new Map<string, WageTypeSummary>();
        for (const l of this.result()?.lines ?? []) {
            const code = l.wageTypeCode ?? '';
            let row = byCode.get(code);
            if (!row) {
                row = {
                    code,
                    name: l.wageTypeName ?? '',
                    group: l.billingType,
                    lines: 0,
                    matched: 0,
                    different: 0,
                    onlyInLegacy: 0,
                    onlyInHcmv3: 0,
                    difference: 0,
                };
                byCode.set(code, row);
            }
            row.lines++;
            row.matched += l.status === GoLiveV2CompareStatus.Matched ? 1 : 0;
            row.different += l.status === GoLiveV2CompareStatus.Different ? 1 : 0;
            row.onlyInLegacy += l.status === GoLiveV2CompareStatus.OnlyInLegacy ? 1 : 0;
            row.onlyInHcmv3 += l.status === GoLiveV2CompareStatus.OnlyInHcmv3 ? 1 : 0;
            row.difference += l.comparesDays ? 0 : (l.difference ?? 0);
        }
        return [...byCode.values()].sort(
            (a, b) => b.lines - b.matched - (a.lines - a.matched) || a.code.localeCompare(b.code)
        );
    });

    onSearch(event: Event): void {
        this.search.set((event.target as HTMLInputElement).value);
        this.page.set(1);
    }

    onStatus(event: Event): void {
        const value = (event.target as HTMLSelectElement).value;
        this.status.set(value === '' ? null : (Number(value) as GoLiveV2CompareStatus));
        this.page.set(1);
    }

    onCause(event: Event): void {
        const value = (event.target as HTMLSelectElement).value;
        this.cause.set(value === '' ? null : (Number(value) as GoLiveV2CompareCause));
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

    onHideMatched(event: Event): void {
        this.hideMatched.set((event.target as HTMLInputElement).checked);
        this.page.set(1);
    }

    onPageSize(event: Event): void {
        this.pageSize.set(Number((event.target as HTMLSelectElement).value));
        this.page.set(1);
    }

    goToPage(page: number): void {
        this.page.set(Math.max(1, Math.min(page, this.pageCount())));
    }

    /** A tile or a wage type row narrows the lines to what it counts. */
    showStatus(status: GoLiveV2CompareStatus): void {
        this.resetFilters();
        this.status.set(status);
        this.hideMatched.set(status !== GoLiveV2CompareStatus.Matched);
        this.tab.set('lines');
    }

    showWageType(code: string): void {
        this.resetFilters();
        this.wageType.set(code);
        this.tab.set('lines');
    }

    resetFilters(): void {
        this.search.set('');
        this.status.set(null);
        this.cause.set(null);
        this.group.set(null);
        this.wageType.set('');
        this.hideMatched.set(true);
        this.page.set(1);
    }

    // ---------------------------------------------------------------- labels

    statusKey(status: GoLiveV2CompareStatus): string {
        switch (status) {
            case GoLiveV2CompareStatus.Matched:
                return 'GoLiveV2CompareMatchedCount';
            case GoLiveV2CompareStatus.Different:
                return 'GoLiveV2CompareDifferentCount';
            case GoLiveV2CompareStatus.OnlyInLegacy:
                return 'GoLiveV2CompareOnlyLegacyCount';
            default:
                return 'GoLiveV2CompareOnlyHcmv3Count';
        }
    }

    statusClass(status: GoLiveV2CompareStatus): string {
        switch (status) {
            case GoLiveV2CompareStatus.Matched:
                return 'badge-light-success';
            case GoLiveV2CompareStatus.Different:
                return 'badge-light-danger';
            default:
                return 'badge-light-warning';
        }
    }

    causeKey(cause: GoLiveV2CompareCause): string {
        return 'GoLiveV2CompareCause' + GoLiveV2CompareCause[cause];
    }

    groupKey(group: BillingType | undefined): string {
        return group === undefined || group === null ? '' : 'GoLiveV2RegisterGroup' + BillingType[group];
    }

    /** Days for time types, which count days; the amount for everything else. */
    valueOf(line: GoLiveV2LegacyCompareLineDto, side: 'legacy' | 'hcmv3'): number | undefined {
        return side === 'legacy'
            ? line.comparesDays
                ? line.legacyNumber
                : line.legacyAmount
            : line.comparesDays
              ? line.hcmv3Number
              : line.hcmv3Amount;
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

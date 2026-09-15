import { Component, OnDestroy, OnInit, computed, effect, inject, signal, untracked } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { DateTime } from 'luxon';
import { firstValueFrom } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import {
    BillingType,
    GetPayrollSheetReportInput,
    GoLiveV2BankFileLayoutDto,
    GoLiveV2BankFileLeftOutReason,
    GoLiveV2BankFileSetupInput,
    GoLiveV2BankFileSetupOutput,
    GoLiveV2GenerateBankFileInput,
    GoLiveV2GenerateBankFileOutput,
    GoLiveV2ClearCompanyDto,
    GoLiveV2ClearCompanyInput,
    GoLiveV2EmployeeRowDto,
    GoLiveV2EmployeeRowResultDto,
    GoLiveV2EmployeeSheetInput,
    GoLiveV2LoadEmployeesInput,
    GoLiveV2LoadEmployeesOutput,
    GoLiveV2NextStep,
    GoLiveV2GridDto,
    GoLiveV2PayrollResultsInput,
    GoLiveV2PayslipsInput,
    GoLiveV2PayslipsServiceProxy,
    GoLiveV2PayrollResultsOutput,
    GoLiveV2PayrollResultRowDto,
    GoLiveV2PayrollRunProgressDto,
    GoLiveV2RunStatus,
    GoLiveV2SalaryRegisterDto,
    GoLiveV2SalaryRegisterInput,
    GoLiveV2SalaryRegisterLineDto,
    GoLiveV2RunPayrollInput,
    GoLiveV2RunPayrollOutput,
    GoLiveV2ServiceProxy,
    GoLiveV2SetUpPayrollMonthInput,
    GoLiveV2SetUpPayrollMonthOutput,
    GoLiveV2StoppedAt,
    PayrollRunType,
    PayrollSheetReportServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { V2DocViewerComponent, V2Document } from './doc-viewer/v2-doc-viewer.component';
import { V2LegacyCompareComponent } from './legacy-compare/v2-legacy-compare.component';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';
import { PasteGridComponent } from '../go-live-setup/shared/paste-grid/paste-grid.component';
import {
    PasteGridColumn,
    PasteGridIssue,
    PasteGridRow,
    isRowEmpty,
} from '../go-live-setup/shared/paste-grid/paste-grid.types';

/** One line in the log: a press of Run payroll, a set-up step, or a load of employees. */
interface Attempt {
    at: Date;
    result?: GoLiveV2RunPayrollOutput;
    setUp?: GoLiveV2SetUpPayrollMonthOutput;
    load?: GoLiveV2LoadEmployeesOutput;
}

/** Live state of a load, for the progress window. One employee is sent per request, so this is exact. */
interface LoadProgress {
    total: number;
    done: number;
    loaded: number;
    failed: number;
    current: { row: number; code: string; name: string } | null;
    startedAt: number;
    finishedAt: number | null;
    /** How it ended: every row done, stopped by the user, or stopped by a server error on this row. */
    outcome: 'running' | 'finished' | 'stopped' | 'error';
    errorRow: number | null;
    /** The latest results, newest first -- enough to see a failure without scrolling a list. */
    recent: GoLiveV2EmployeeRowResultDto[];
}

/**
 * Live state of Run payroll, for its progress window. The run goes on in the background on the server, which
 * keeps where it stands; the window polls that, so a reload or a new login finds the same run.
 */
interface RunProgress {
    month: string;
    startedAt: number;
    finishedAt: number | null;
    server: GoLiveV2PayrollRunProgressDto | null;
}

/** Remembers, per browser, which finished run has been seen, so its result is shown once after a return and not every visit. */
const SeenRunStorageKey = 'GoLiveV2.SeenPayrollRun';

/** How often the screen tells the idle lock someone is here while a run or load is going. */
const KeepAwakeMs = 30000;

/** Two letters, two check digits, then 10 to 30 letters and digits: every country's IBAN has this shape. */
const IbanShape = /^[A-Z]{2}[0-9]{2}[A-Z0-9]{10,30}$/;

/** A Kuwaiti IBAN is always 30 characters. */
const KuwaitIbanLength = 30;

/** The payroll area every new tenant gets, created on first use. */
const DefaultPayrollArea = 'Default Payroll Area';

/**
 * Go-Live Setup V2, built backwards from a working payroll.
 *
 * It starts with the one step a customer actually wants -- Run payroll -- and nothing else. The engine's
 * refusal is shown in its own words, because that wording is what the next step gets built from. Every
 * attempt is kept in a log on the page, so the path from "nothing" to "a payroll that runs" can be shown
 * as it happened.
 *
 * Each field is added only once the engine has asked for it, and says so on screen. The first press sent
 * nothing and was refused for want of a pay date, so the month is the first field; with a month, it asked
 * for a payroll area, so that is the second. With both, the engine found no control record, so a refusal
 * can now come with the step that answers it: here, setting up the month. Once it had one, it asked for a
 * pay calendar, so the same step now lays one down too -- January to December, without asking. With the
 * month in place the run went through and found nobody to pay, which is where "Load employee data" comes
 * in: a step before Run payroll, asking who each employee is and what they are paid, and setting up the rest.
 */
@Component({
    templateUrl: './go-live-v2.component.html',
    styleUrl: './go-live-v2.component.css',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        LocalizePipe,
        DatePipe,
        DecimalPipe,
        PasteGridComponent,
        V2DocViewerComponent,
        V2LegacyCompareComponent,
    ],
})
export class GoLiveV2Component extends AppComponentBase implements OnInit, OnDestroy {
    private readonly proxy = inject(GoLiveV2ServiceProxy);
    private readonly payrollSheetProxy = inject(PayrollSheetReportServiceProxy);
    private readonly fileDownload = inject(FileDownloadService);
    private readonly payslipsProxy = inject(GoLiveV2PayslipsServiceProxy);
    private runPoll: ReturnType<typeof setInterval> | null = null;
    private ticker: ReturnType<typeof setInterval> | null = null;
    private keepAwake: ReturnType<typeof setInterval> | null = null;

    readonly StoppedAt = GoLiveV2StoppedAt;
    readonly NextStep = GoLiveV2NextStep;

    // ---------------------------------------------------------------- the three steps

    /**
     * The steps run in order, and the order is what you press, not what the database already holds. The
     * screen opens on step 1 with the other two shut; loading employees opens step 2, a payroll that
     * runs opens step 3. Loading employees again puts the run and the bank file back where they were
     * before -- they described the employees as they were, and those have just changed.
     */
    readonly activeStep = signal(1);

    /** The furthest step opened so far: 1 on arrival, 2 once employees are loaded, 3 once payroll runs. */
    readonly unlockedStep = signal(1);

    /**
     * The employees as last loaded -- read back on arrival, or just sent -- in the same form as the grid's
     * snapshot below. Null until anything is loaded.
     */
    private readonly loadedSnapshot = signal<string | null>(null);

    /**
     * Whether the grid or the company differs from what was last loaded. Edited, pasted or undone back:
     * the grid is compared by value, so a change put back the way it was counts as no change.
     */
    readonly gridChanged = computed(
        () =>
            this.snapshotOf(this.employeeRows(), this.organizationUnitName(), this.saturdayIsRestDay()) !==
            (this.loadedSnapshot() ?? this.snapshotOf([], '', false))
    );

    /**
     * The furthest step open now. Unsaved changes in the grid shut steps 2 and 3: they would run payroll
     * and make the bank file for employees as they were, not as the grid now shows them.
     */
    readonly openStep = computed(() => (this.gridChanged() ? 1 : this.unlockedStep()));

    readonly steps = computed(() => {
        const unlocked = this.openStep();

        return [
            {
                number: 1,
                title: this.l('GoLiveV2StepLoadName'),
                what: this.l('GoLiveV2StepLoadWhat'),
                icon: 'fa-users',
                done: unlocked > 1,
                ready: true,
                why: '',
            },
            {
                number: 2,
                title: this.l('GoLiveV2StepRunName'),
                what: this.l('GoLiveV2StepRunWhat'),
                icon: 'fa-calculator',
                done: unlocked > 2,
                ready: unlocked >= 2,
                why: this.l('GoLiveV2StepNeedsEmployees'),
            },
            {
                number: 3,
                title: this.l('GoLiveV2StepBankFileName'),
                what: this.l('GoLiveV2StepBankFileWhat'),
                icon: 'fa-file-invoice-dollar',
                done: this.bankFile() != null,
                ready: unlocked >= 3,
                why: this.l('GoLiveV2StepNeedsPayroll'),
            },
        ];
    });

    /** The step on screen, never one that is still shut. */
    readonly currentStep = computed(() => Math.min(this.activeStep(), this.openStep()));

    goToStep(step: number): void {
        if (step <= this.openStep()) {
            this.activeStep.set(step);
        }
    }

    /**
     * A fresh load of employees makes the month's run and its bank file out of date, so they are
     * cleared and their steps shut again: what is on screen always describes the employees now loaded.
     */
    private restartFromStepTwo(): void {
        this.latest.set(null);
        this.lastSetUp.set(null);
        this.results.set(null);
        this.bankFile.set(null);
        this.unlockedStep.set(2);
        this.activeStep.set(2);
    }

    // ---------------------------------------------------------------- clearing the company

    /** What Clear would remove, while its confirmation is open; null when it is closed. */
    readonly clearPreview = signal<GoLiveV2ClearCompanyDto | null>(null);
    readonly clearTyped = signal('');
    readonly clearing = signal(false);

    /** Clear unlocks only once the company's name is typed back -- the server checks it again. */
    readonly canConfirmClear = computed(() => {
        const name = this.clearPreview()?.companyName?.trim().toLowerCase();
        return !!name && this.clearTyped().trim().toLowerCase() === name && !this.clearing();
    });

    readonly clearHasSomething = computed(() => {
        const preview = this.clearPreview();
        return !!preview && (preview.employees > 0 || !!preview.payrollMonthEnd);
    });

    readonly exportingEmployees = signal(false);

    /**
     * The grid as it stands, unsaved edits included, as Excel for a customer to fill in: the same columns in the
     * same order, so their sheet pastes straight back. An empty grid gives the headers alone.
     */
    exportEmployees(): void {
        const input = new GoLiveV2EmployeeSheetInput();
        input.employees = this.buildEmployeeRows().filter((row) => !this.isEmptyRow(row));

        this.exportingEmployees.set(true);
        this.proxy
            .exportEmployeeSheet(input)
            .pipe(finalize(() => this.exportingEmployees.set(false)))
            .subscribe((file) => this.fileDownload.downloadTempFile(file));
    }

    openClear(): void {
        this.clearTyped.set('');
        this.proxy.getClearPreview(this.payrollAreaName().trim()).subscribe((preview) => this.clearPreview.set(preview));
    }

    closeClear(): void {
        if (!this.clearing()) {
            this.clearPreview.set(null);
        }
    }

    onClearTyped(event: Event): void {
        this.clearTyped.set((event.target as HTMLInputElement).value);
    }

    /**
     * Back to a blank company: the grid, the run, the results and the bank file are all gone from the screen
     * as they are from the database, and the month is unset so any month can be chosen -- an earlier one too.
     */
    confirmClear(): void {
        if (!this.canConfirmClear()) {
            return;
        }

        const input = new GoLiveV2ClearCompanyInput();
        input.payrollAreaName = this.payrollAreaName().trim();
        input.confirmCompanyName = this.clearTyped().trim();

        this.clearing.set(true);
        this.proxy
            .clearCompany(input)
            .pipe(finalize(() => this.clearing.set(false)))
            .subscribe(() => {
                this.clearPreview.set(null);
                this.loadedSnapshot.set(null);
                this.employeeRows.set([]);
                this.lastLoad.set(null);
                this.latest.set(null);
                this.lastSetUp.set(null);
                this.results.set(null);
                this.bankFile.set(null);
                this.payMonth.set('');
                this.saturdayIsRestDay.set(false);
                this.unlockedStep.set(1);
                this.activeStep.set(1);
                this.notify.success(this.l('GoLiveV2ClearDone'));
            });
    }

    // ---------------------------------------------------------------- step 1: load employee data

    /** The company, created as the top organisation unit. Asked for, never guessed. */
    readonly organizationUnitName = signal('');

    /** Whether the company gives Saturday as a rest day (Rest Days, 4009). Friday is always the weekly off. */
    readonly saturdayIsRestDay = signal(false);

    onSaturdayRestDayChange(event: Event): void {
        this.saturdayIsRestDay.set((event.target as HTMLInputElement).checked);
    }

    readonly employeeRows = signal<PasteGridRow[]>([]);
    readonly gridIssues = signal<PasteGridIssue[]>([]);
    readonly loading = signal(false);
    readonly lastLoad = signal<GoLiveV2LoadEmployeesOutput | null>(null);
    readonly progress = signal<LoadProgress | null>(null);
    readonly stopRequested = signal(false);

    /** Ticks once a second while a load runs, so elapsed and time-left count down on their own. */
    readonly now = signal(Date.now());

    /** The tenant's banks by name, for the Bank column. */
    readonly bankNames = signal<string[]>([]);

    readonly employeeColumns = computed<PasteGridColumn[]>(() => [
        { key: 'code', header: this.l('GoLiveV2ColCode'), type: 'text', required: true, maxLength: 64, width: 140 },
        {
            key: 'fullName',
            header: this.l('GoLiveV2ColFullName'),
            type: 'text',
            required: true,
            maxLength: 250,
            width: 240,
        },
        { key: 'joiningDate', header: this.l('GoLiveV2ColJoiningDate'), type: 'date', required: true, width: 140 },
        {
            key: 'civilId',
            header: this.l('GoLiveV2ColCivilId'),
            type: 'text',
            required: true,
            maxLength: 18,
            width: 160,
            hint: this.l('GoLiveV2ColCivilIdHint'),
            validate: (value) =>
                /^\d+$/.test(String(value).trim())
                    ? null
                    : { key: 'GoLiveV2RowCivilIdNotANumber', params: [String(value)] },
        },
        {
            key: 'basicSalary',
            header: this.l('GoLiveV2ColBasicSalary'),
            type: 'number',
            required: true,
            min: 0,
            width: 130,
            hint: this.l('GoLiveV2ColBasicSalaryHint'),
        },
        ...(
            [
                ['accommodationAllowance', 'GoLiveV2ColAccommodation'],
                ['mobileAllowance', 'GoLiveV2ColMobile'],
                ['transportAllowance', 'GoLiveV2ColTransport'],
                ['foodAllowance', 'GoLiveV2ColFood'],
                ['housingAllowance', 'GoLiveV2ColHousing'],
                ['otherAllowance', 'GoLiveV2ColOther'],
            ] as const
        ).map(
            ([key, header]): PasteGridColumn => ({
                key,
                header: this.l(header),
                type: 'number',
                min: 0,
                width: 130,
                hint: this.l('GoLiveV2ColAmountHint'),
            })
        ),
        {
            key: 'annualLeaveOpeningBalance',
            header: this.l('GoLiveV2ColAnnualLeaveOpening'),
            type: 'number',
            width: 190,
            hint: this.l('GoLiveV2ColAnnualLeaveOpeningHint'),
        },
        {
            key: 'bankName',
            header: this.l('GoLiveV2ColBank'),
            type: 'choice',
            options: this.bankNames(),
            width: 230,
            hint: this.l('GoLiveV2ColBankHint'),
        },
        {
            key: 'iban',
            header: this.l('GoLiveV2ColIban'),
            type: 'text',
            maxLength: 50,
            width: 290,
            hint: this.l('GoLiveV2ColIbanHint'),
            validate: (value) => {
                const iban = String(value).replace(/\s+/g, '').toUpperCase();
                const ok = IbanShape.test(iban) && (!iban.startsWith('KW') || iban.length === KuwaitIbanLength);
                return ok ? null : { key: 'GoLiveV2RowIbanInvalid', params: [String(value)] };
            },
        },
        {
            key: 'accountNumber',
            header: this.l('GoLiveV2ColAccountNumber'),
            type: 'text',
            maxLength: 50,
            width: 180,
            hint: this.l('GoLiveV2ColAccountNumberHint'),
        },
    ]);

    readonly filledEmployeeCount = computed(() => this.employeeRows().filter((r) => !isRowEmpty(r)).length);

    readonly canLoad = computed(
        () =>
            this.organizationUnitName().trim().length > 0 &&
            this.filledEmployeeCount() > 0 &&
            this.gridIssues().length === 0 &&
            this.payrollAreaName().trim().length > 0 &&
            this.gridChanged()
    );

    /** Why Load is shut, for its tooltip: nothing has changed since the last load. */
    readonly loadUnchanged = computed(() => this.loadedSnapshot() !== null && !this.gridChanged());

    // ---------------------------------------------------------------- step 2: run payroll

    /** 'yyyy-MM' from the month picker; empty until one is chosen. */
    readonly payMonth = signal('');

    /** The last day of the chosen month -- the only pay date the engine accepts. */
    readonly monthEnd = computed(() => {
        const month = this.payMonth();
        return month ? DateTime.fromISO(`${month}-01`).endOf('month').startOf('day') : null;
    });

    /** The payroll area to run, by name -- the engine matches it by name. Also where step 1 puts employees. */
    readonly payrollAreaName = signal(DefaultPayrollArea);

    readonly running = signal(false);
    readonly runProgress = signal<RunProgress | null>(null);

    /** The run goes on without the window, so it can be put away and brought back. */
    readonly runWindowHidden = signal(false);
    readonly BillingType = BillingType;

    // ---------------------------------------------------------------- step 3: see the results

    readonly results = signal<GoLiveV2PayrollResultsOutput | null>(null);

    /** The results worth showing: a month nobody has been paid for yet is an empty table, so it stays hidden. */
    readonly paidResults = computed(() => {
        const results = this.results();
        return results && results.employeesPaid > 0 ? results : null;
    });
    readonly loadingResults = signal(false);
    readonly exporting = signal<'sheet' | 'register' | 'payslips' | null>(null);
    readonly register = signal<GoLiveV2SalaryRegisterDto | null>(null);
    readonly registerFor = signal<GoLiveV2PayrollResultRowDto | null>(null);
    private resultsRequest = 0;

    /** The results, and the bank file's setup, follow the month and payroll area on their own. */
    private readonly resultsFollowSelection = effect(() => {
        this.monthEnd();
        this.payrollAreaName();
        untracked(() => {
            this.showResults();
            this.refreshBankFileSetup();
        });
    });

    /** Compare with legacy salary, open for the month chosen above. */
    readonly compareOpen = signal(false);

    /** The document being looked at, or null. Everything V2 produces goes through here first. */
    readonly openDocument = signal<V2Document | null>(null);

    closeDocument(): void {
        const url = this.openDocument()?.pdfUrl;
        if (url) {
            URL.revokeObjectURL(url);
        }
        this.openDocument.set(null);
    }

    /** A report the server has read back out of its own file: shown as a table, saved as the file. */
    private showGrid(grid: GoLiveV2GridDto, download: () => void): void {
        this.openDocument.set(
            grid.text
                ? { title: grid.title ?? '', kind: 'text', text: grid.text, download }
                : {
                      title: grid.title ?? '',
                      kind: 'grid',
                      columns: grid.columns ?? [],
                      rows: (grid.rows ?? []).map((row) => row ?? []),
                      download,
                  }
        );
    }

    private saveBlob(fileName: string, blob: Blob): void {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        link.click();
        setTimeout(() => URL.revokeObjectURL(url), 60000);
    }

    private base64Blob(base64: string, contentType: string): Blob {
        const binary = atob(base64);
        const bytes = new Uint8Array(binary.length);
        for (let i = 0; i < binary.length; i++) {
            bytes[i] = binary.charCodeAt(i);
        }
        return new Blob([bytes], { type: contentType });
    }

    // ---------------------------------------------------------------- step 3: the bank file

    readonly bankSetup = signal<GoLiveV2BankFileSetupOutput | null>(null);
    readonly payingBankId = signal<number | null>(null);
    readonly bankLayout = signal('');
    readonly companyAccountNo = signal('');
    readonly companyCode = signal('');
    readonly companyMosalId = signal('');
    readonly generatingBankFile = signal(false);
    readonly bankFile = signal<GoLiveV2GenerateBankFileOutput | null>(null);
    readonly LeftOutReason = GoLiveV2BankFileLeftOutReason;
    private bankSetupRequest = 0;

    readonly payingBank = computed(
        () => this.bankSetup()?.payingBanks?.find((b) => b.bankId === this.payingBankId()) ?? null
    );

    readonly selectedLayout = computed(
        () => this.payingBank()?.layouts?.find((l) => l.formatCode === this.bankLayout()) ?? null
    );

    /** Everything the chosen layout needs is filled in. The server checks again, in the same words. */
    readonly canMakeBankFile = computed(() => {
        const layout = this.selectedLayout();
        return (
            !!this.monthEnd() &&
            !!layout &&
            !this.generatingBankFile() &&
            (!layout.needsCompanyAccount || this.companyAccountNo().trim().length > 0) &&
            (!layout.needsCompanyCode || this.companyCode().trim().length > 0) &&
            (!layout.needsMosalId || this.companyMosalId().trim().length > 0)
        );
    });

    /** Search and paging are on the page: the results come whole, a line per employee, so 100 is nothing. */
    readonly resultsSearch = signal('');
    readonly resultsPage = signal(1);
    readonly resultsPageSize = signal(25);
    readonly resultsPageSizes = [25, 50, 100];

    /** By code or name, anywhere in either. */
    readonly filteredResults = computed(() => {
        const rows = this.results()?.employees ?? [];
        const query = this.resultsSearch().trim().toLowerCase();
        return query
            ? rows.filter(
                  (r) =>
                      (r.employeeCode ?? '').toLowerCase().includes(query) ||
                      (r.employeeName ?? '').toLowerCase().includes(query)
              )
            : rows;
    });

    readonly resultsPageCount = computed(() =>
        Math.max(1, Math.ceil(this.filteredResults().length / this.resultsPageSize()))
    );

    /** The page asked for, kept inside the pages there are -- a search can leave fewer. */
    readonly currentResultsPage = computed(() => Math.min(this.resultsPage(), this.resultsPageCount()));

    readonly pagedResults = computed(() => {
        const size = this.resultsPageSize();
        const start = (this.currentResultsPage() - 1) * size;
        return this.filteredResults().slice(start, start + size);
    });

    readonly resultsFrom = computed(() =>
        this.filteredResults().length === 0 ? 0 : (this.currentResultsPage() - 1) * this.resultsPageSize() + 1
    );

    readonly resultsTo = computed(() =>
        Math.min(this.currentResultsPage() * this.resultsPageSize(), this.filteredResults().length)
    );

    /** Up to seven page numbers, centred on the current page. */
    readonly resultsPageNumbers = computed(() => {
        const count = this.resultsPageCount();
        const shown = Math.min(7, count);
        const first = Math.max(1, Math.min(this.currentResultsPage() - 3, count - shown + 1));
        return Array.from({ length: shown }, (_, i) => first + i);
    });

    /** The register's lines by group, in the order a payslip reads: earnings, deductions, provisions, day counters. */
    readonly registerGroups = computed(() => {
        const lines = this.register()?.lines ?? [];
        return [BillingType.Earning, BillingType.Deduction, BillingType.Provision, BillingType.TimeType]
            .map((group) => ({ group, lines: lines.filter((l) => l.group === group) }))
            .filter((g) => g.lines.length > 0);
    });
    readonly settingUp = signal(false);
    readonly latest = signal<GoLiveV2RunPayrollOutput | null>(null);
    readonly lastSetUp = signal<GoLiveV2SetUpPayrollMonthOutput | null>(null);
    readonly attempts = signal<Attempt[]>([]);

    /** A set-up needs the same two things a run does. */
    readonly canSetUp = computed(() => !!this.monthEnd() && this.payrollAreaName().trim().length > 0);

    ngOnInit(): void {
        this.proxy.getBankNames().subscribe((names) => this.bankNames.set(names.items ?? []));
        this.showPreviouslyLoaded();
        this.findRunInProgress();
    }

    ngOnDestroy(): void {
        this.stopTicker();
        this.stopRunPoll();
        this.stopKeepAwake();
    }

    // ---------------------------------------------------------------- step 1

    /**
     * One employee per request, so the progress window can say exactly who is being loaded, how long each takes
     * and how long is left -- and so a slow moment or a server error costs one employee, not the whole sheet.
     * Each request is its own transaction; what was loaded stays loaded, and loading again skips it.
     */
    async loadEmployees(): Promise<void> {
        if (this.loading() || !this.canLoad()) {
            return;
        }

        const sending = this.snapshotOf(this.employeeRows(), this.organizationUnitName(), this.saturdayIsRestDay());
        const items = this.buildEmployeeRows()
            .map((dto, i) => ({ dto, row: i + 1 }))
            .filter((item) => !this.isEmptyRow(item.dto));

        const combined = new GoLiveV2LoadEmployeesOutput();
        combined.rows = [];

        this.loading.set(true);
        this.stopRequested.set(false);
        this.progress.set({
            total: items.length,
            done: 0,
            loaded: 0,
            failed: 0,
            current: null,
            startedAt: Date.now(),
            finishedAt: null,
            outcome: 'running',
            errorRow: null,
            recent: [],
        });
        this.startTicker();
        this.startKeepAwake();

        let outcome: LoadProgress['outcome'] = 'finished';
        let errorRow: number | null = null;

        try {
            for (const item of items) {
                if (this.stopRequested()) {
                    outcome = 'stopped';
                    break;
                }

                this.progress.update((p) => ({
                    ...p,
                    current: { row: item.row, code: item.dto.code ?? '', name: item.dto.fullName ?? '' },
                }));

                const input = new GoLiveV2LoadEmployeesInput();
                input.organizationUnitName = this.organizationUnitName().trim();
                input.saturdayIsRestDay = this.saturdayIsRestDay();
                input.payrollAreaName = this.payrollAreaName().trim();
                input.employees = [item.dto];

                let out: GoLiveV2LoadEmployeesOutput;
                try {
                    out = await firstValueFrom(this.proxy.loadEmployees(input));
                } catch {
                    // The server's own error dialog has already said what went wrong.
                    outcome = 'error';
                    errorRow = item.row;
                    break;
                }

                this.merge(combined, out, item.row);
                const result = out.rows?.[0];
                this.progress.update((p) => ({
                    ...p,
                    done: p.done + 1,
                    loaded: p.loaded + (result?.succeeded ? 1 : 0),
                    failed: p.failed + (result && !result.succeeded ? 1 : 0),
                    recent: result ? [result, ...p.recent].slice(0, 5) : p.recent,
                }));
            }
        } finally {
            this.stopTicker();
            if (!this.running()) {
                this.stopKeepAwake();
            }
            this.loading.set(false);
            this.progress.update((p) => ({ ...p, current: null, finishedAt: Date.now(), outcome, errorRow }));
        }

        if (combined.rows.length > 0) {
            // Whatever the last run said is out of date now that there are people to pay.
            this.latest.set(null);
            this.lastSetUp.set(null);
            this.lastLoad.set(combined);
            if (combined.rows.some((r) => r.succeeded)) {
                this.restartFromStepTwo();
                // Loaded to the end: the grid as sent is now what is loaded, so Load shuts until it changes.
                // A stopped or broken load keeps Load open to carry on.
                if (outcome === 'finished') {
                    this.loadedSnapshot.set(sending);
                }
            }
            this.refreshBankFileSetup();
            this.attempts.update((list) => [{ at: new Date(), load: combined }, ...list]);
        }
    }

    stopLoading(): void {
        this.stopRequested.set(true);
    }

    closeProgress(): void {
        this.progress.set(null);
    }

    progressPercent(p: LoadProgress): number {
        return p.total === 0 ? 100 : Math.round((p.done / p.total) * 100);
    }

    elapsedText(p: LoadProgress): string {
        return this.duration((p.finishedAt ?? this.now()) - p.startedAt);
    }

    /** The average so far. The first employee also sets up the company, so the estimate settles after a few. */
    averageText(p: LoadProgress): string {
        return p.done === 0 ? '—' : this.l('GoLiveV2ProgressSeconds', (this.elapsedMs(p) / p.done / 1000).toFixed(1));
    }

    timeLeftText(p: LoadProgress): string {
        if (p.done === 0) {
            return '—';
        }
        return this.duration((this.elapsedMs(p) / p.done) * (p.total - p.done));
    }

    progressEndText(p: LoadProgress): string {
        switch (p.outcome) {
            case 'stopped':
                return this.l('GoLiveV2ProgressStopped', p.done, p.total);
            case 'error':
                return this.l('GoLiveV2ProgressServerError', p.errorRow);
            default:
                return this.l('GoLiveV2ProgressFinished', this.elapsedText(p));
        }
    }

    onOrganizationUnitChange(event: Event): void {
        this.organizationUnitName.set((event.target as HTMLInputElement).value);
    }

    /** What a load did, one sentence per record it created or used, including the Kuwait defaults. */
    describeLoad(load: GoLiveV2LoadEmployeesOutput): string[] {
        const lines = [
            this.l(
                load.organizationUnitCreated ? 'GoLiveV2LoadCreatedUnit' : 'GoLiveV2LoadUsedUnit',
                load.organizationUnitName
            ),
            this.l(load.payrollAreaCreated ? 'GoLiveV2LoadCreatedArea' : 'GoLiveV2SetUpUsedArea', load.payrollAreaName),
            this.l(
                load.employeeGroupCreated || load.employeeSubGroupCreated
                    ? 'GoLiveV2LoadCreatedGrade'
                    : 'GoLiveV2LoadUsedGrade',
                load.employeeGroupName,
                load.employeeSubGroupName
            ),
        ];

        if (load.sickLeaveRuleCreated) {
            lines.push(this.l('GoLiveV2LoadSickLeaveRule', 'Kuwait Art.69 Sick Leave'));
        }

        if (load.workingTimeCreated) {
            lines.push(this.l('GoLiveV2LoadWorkingTime'));
        }

        if (load.placeholdersCreated > 0) {
            lines.push(this.l('GoLiveV2LoadCreatedPlaceholders', load.placeholdersCreated));
        }

        lines.push(this.l('GoLiveV2LoadEmployeesCreated', load.employeesCreated));

        if (load.employeesCreated > 0) {
            lines.push(this.l('GoLiveV2LoadSalariesCreated'));
        }

        if (load.employeesAlreadyLoaded > 0) {
            lines.push(this.l('GoLiveV2LoadAlreadyLoaded', load.employeesAlreadyLoaded));
        }

        return lines;
    }

    failedRows(load: GoLiveV2LoadEmployeesOutput): GoLiveV2LoadEmployeesOutput['rows'] {
        return (load.rows ?? []).filter((r) => !r.succeeded);
    }

    loadButtonLabel(): string {
        const p = this.progress();
        return this.loading() && p
            ? this.l('GoLiveV2LoadingCount', p.done + 1 > p.total ? p.total : p.done + 1, p.total)
            : this.l('GoLiveV2LoadButton', this.filledEmployeeCount());
    }

    /** Scrolls up to step 1 -- a plain "#" link would be taken by the router as a new page. */
    goToLoadEmployees(): void {
        document.getElementById('v2-load-employees')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }

    // ---------------------------------------------------------------- step 2

    /**
     * Starts the run on the server and follows it there. The month set-up and the payroll-month check are
     * answered at once, before any engine starts; the engines then run in the background, so a locked screen,
     * a reload or a new login no longer loses the run -- the window just picks it up again.
     */
    runPayroll(): void {
        if (this.running()) {
            // Already going: the button brings the window back.
            this.runWindowHidden.set(false);
            return;
        }

        this.running.set(true);
        this.stopRequested.set(false);
        this.runWindowHidden.set(false);

        const end = this.monthEnd();
        this.runProgress.set({
            month: end ? end.toFormat('MMMM yyyy') : '',
            startedAt: Date.now(),
            finishedAt: null,
            server: null,
        });
        this.startTicker();
        this.startKeepAwake();

        this.proxy.startPayrollRun(this.buildRunInput()).subscribe({
            next: (progress) => this.followRun(progress, true),
            error: () => {
                this.endRunWatch();
                this.runProgress.set(null);
            },
        });
    }

    /** Which engine is working now: the payroll engine once it has started, the time engine before that. */
    runPhase(p: RunProgress): 'starting' | 'time' | 'payroll' {
        const s = p.server;
        if (!s) {
            return 'starting';
        }
        if (s.payrollDone > 0 || (s.totalEmployees > 0 && s.timeDone >= s.totalEmployees)) {
            return 'payroll';
        }
        return s.timeDone > 0 ? 'time' : 'starting';
    }

    /** Employees the engine now working has finished. */
    runDone(p: RunProgress): number {
        const s = p.server;
        if (!s) {
            return 0;
        }
        return this.runPhase(p) === 'payroll' ? s.payrollDone : s.timeDone;
    }

    runFailed(p: RunProgress): number {
        const s = p.server;
        if (!s) {
            return 0;
        }
        return this.runPhase(p) === 'payroll' ? s.payrollFailed : s.timeFailed;
    }

    runTotal(p: RunProgress): number {
        return p.server?.totalEmployees ?? 0;
    }

    runPercent(p: RunProgress): number {
        const total = this.runTotal(p);
        return total === 0 ? 0 : Math.min(100, Math.round((this.runDone(p) / total) * 100));
    }

    runElapsedText(p: RunProgress): string {
        return this.duration((p.finishedAt ?? this.now()) - p.startedAt);
    }

    runAverageText(p: RunProgress): string {
        const ms = p.server?.averageMilliseconds ?? 0;
        return ms > 0 ? this.l('GoLiveV2ProgressSeconds', (ms / 1000).toFixed(1)) : '—';
    }

    /** What both engines still have to do, at the pace so far. */
    runTimeLeftText(p: RunProgress): string {
        const s = p.server;
        if (!s || !s.averageMilliseconds || s.totalEmployees === 0) {
            return '—';
        }
        const left = Math.max(0, s.totalEmployees - s.timeDone) + Math.max(0, s.totalEmployees - s.payrollDone);
        return this.duration(left * s.averageMilliseconds);
    }

    /** How the run ended, in one sentence, for the window's footer. */
    runEndText(p: RunProgress): string {
        const s = p.server;
        switch (s?.status) {
            case GoLiveV2RunStatus.Stopped:
                return this.l('GoLiveV2RunStoppedNote', this.runElapsedText(p));
            case GoLiveV2RunStatus.Failed:
                return this.l('GoLiveV2RunFailedNote', s.result?.message ?? '');
            default:
                return this.l('GoLiveV2RunFinished', this.runElapsedText(p));
        }
    }

    /** Asks the run to stop after the batch in hand; what ran before stays saved. */
    stopRun(): void {
        if (!this.running()) {
            return;
        }
        this.stopRequested.set(true);
        this.proxy.stopPayrollRun().subscribe();
    }

    hideRunProgress(): void {
        this.runWindowHidden.set(true);
    }

    closeRunProgress(): void {
        this.markRunSeen(this.runProgress()?.server ?? null);
        this.runProgress.set(null);
    }

    // ---------------------------------------------------------------- step 3

    /** The month and payroll area chosen under Run payroll -- the results are that run's. */
    showResults(): void {
        // Only the latest request counts: a month changed mid-load must not be overwritten by the one before.
        const request = ++this.resultsRequest;
        const input = this.resultsInput();
        if (!input) {
            this.results.set(null);
            this.loadingResults.set(false);
            return;
        }
        this.loadingResults.set(true);
        this.proxy
            .getPayrollResults(input)
            .pipe(
                finalize(() => {
                    if (request === this.resultsRequest) {
                        this.loadingResults.set(false);
                    }
                })
            )
            .subscribe((results) => {
                if (request === this.resultsRequest) {
                    this.results.set(results);
                }
            });
    }

    // ---------------------------------------------------------------- step 4

    onPayingBankChange(event: Event): void {
        this.choosePayingBank(Number((event.target as HTMLSelectElement).value));
    }

    onBankLayoutChange(event: Event): void {
        this.bankLayout.set((event.target as HTMLSelectElement).value);
    }

    onBankFieldInput(field: 'account' | 'companyCode' | 'mosalId', event: Event): void {
        const value = (event.target as HTMLInputElement).value;
        ({
            account: this.companyAccountNo,
            companyCode: this.companyCode,
            mosalId: this.companyMosalId,
        })[field].set(value);
    }


    layoutLabel(layout: GoLiveV2BankFileLayoutDto): string {
        return this.l(layout.isText ? 'GoLiveV2LayoutText' : 'GoLiveV2LayoutExcel', layout.formatCode);
    }

    leftOutReasonKey(reason: GoLiveV2BankFileLeftOutReason): string {
        switch (reason) {
            case GoLiveV2BankFileLeftOutReason.NotPaid:
                return 'GoLiveV2LeftOutNotPaid';
            case GoLiveV2BankFileLeftOutReason.NoAccountNumber:
                return 'GoLiveV2LeftOutNoAccountNumber';
            default:
                return 'GoLiveV2LeftOutNoBank';
        }
    }

    /** Made, downloaded, and the setup read again: the layout and account are saved on the bank now. */
    makeBankFile(): void {
        const end = this.monthEnd();
        const bankId = this.payingBankId();
        if (!end || bankId === null || !this.canMakeBankFile()) {
            return;
        }

        const input = new GoLiveV2GenerateBankFileInput();
        input.payDate = end.plus({ hours: 12 });
        input.payrollAreaName = this.payrollAreaName().trim();
        input.paymentBankId = bankId;
        input.formatCode = this.bankLayout();
        input.companyAccountNo = this.companyAccountNo().trim() || undefined;
        input.companyCode = this.companyCode().trim() || undefined;
        input.mosalId = Number(this.companyMosalId().trim()) || undefined;

        this.generatingBankFile.set(true);
        this.bankFile.set(null);
        this.proxy
            .generateBankFile(input)
            .pipe(finalize(() => this.generatingBankFile.set(false)))
            .subscribe((file) => {
                // Made: the third step is done, and the file goes straight to disk in the format the
                // bank takes -- a spreadsheet, a fixed-width text file, or a zip of one per payee bank.
                this.bankFile.set(file);
                const contentType = file.contentType ?? 'application/octet-stream';
                this.saveBlob(file.fileName ?? 'BankFile', this.base64Blob(file.base64Content ?? '', contentType));
                this.refreshBankFileSetup();
            });
    }

    private refreshBankFileSetup(): void {
        const request = ++this.bankSetupRequest;
        const end = this.monthEnd();
        const area = this.payrollAreaName().trim();
        if (!end || !area) {
            this.bankSetup.set(null);
            return;
        }

        const input = new GoLiveV2BankFileSetupInput();
        input.payDate = end.plus({ hours: 12 });
        input.payrollAreaName = area;
        this.proxy.getBankFileSetup(input).subscribe((setup) => {
            if (request === this.bankSetupRequest) {
                this.bankSetup.set(setup);
                this.companyMosalId.set(setup.mosalId ? String(setup.mosalId) : this.companyMosalId());
                this.choosePayingBank(this.payingBankId() ?? setup.payingBanks?.[0]?.bankId ?? null);
            }
        });
    }

    /** A bank's saved choices fill the fields; one not offered (any more) falls back to the first bank. */
    private choosePayingBank(bankId: number | null): void {
        const banks = this.bankSetup()?.payingBanks ?? [];
        const bank = banks.find((b) => b.bankId === bankId) ?? banks[0] ?? null;
        this.payingBankId.set(bank?.bankId ?? null);
        this.bankLayout.set(bank?.selectedLayout ?? '');
        this.companyAccountNo.set(bank?.companyAccountNo ?? '');
        this.companyCode.set(bank?.companyCode ?? '');
    }

    onResultsSearch(event: Event): void {
        this.resultsSearch.set((event.target as HTMLInputElement).value);
        this.resultsPage.set(1);
    }

    onResultsPageSize(event: Event): void {
        this.resultsPageSize.set(Number((event.target as HTMLSelectElement).value));
        this.resultsPage.set(1);
    }

    goToResultsPage(page: number): void {
        this.resultsPage.set(Math.max(1, Math.min(page, this.resultsPageCount())));
    }

    viewRegister(row: GoLiveV2PayrollResultRowDto): void {
        const end = this.monthEnd();
        if (!end) {
            return;
        }
        const input = new GoLiveV2SalaryRegisterInput();
        input.payDate = end.plus({ hours: 12 });
        input.employeeId = row.employeeId;
        this.registerFor.set(row);
        this.register.set(null);
        this.proxy.getSalaryRegister(input).subscribe((register) => this.register.set(register));
    }

    closeRegister(): void {
        this.registerFor.set(null);
        this.register.set(null);
    }

    /**
     * Everyone's payslip for the month, as one PDF: the ported legacy payslip report itself, rendered by
     * the backend for this month and payroll area rather than through the report screen's parameter panel.
     * Opened in a new tab, and saved instead if the browser blocks the tab.
     */
    showPayslips(): void {
        const end = this.monthEnd();
        if (!end || this.exporting()) {
            return;
        }

        const input = new GoLiveV2PayslipsInput();
        input.payDate = end.plus({ hours: 12 });
        input.payrollAreaName = this.payrollAreaName().trim();

        this.exporting.set('payslips');
        this.payslipsProxy
            .getPayslipsPdf(input)
            .pipe(finalize(() => this.exporting.set(null)))
            .subscribe((pdf) => {
                const fileName = pdf.fileName ?? 'Payslips.pdf';
                const blob = this.base64Blob(pdf.base64Content ?? '', 'application/pdf');
                this.openDocument.set({
                    title: this.l('GoLiveV2Payslips'),
                    kind: 'pdf',
                    pdfUrl: URL.createObjectURL(blob),
                    download: () => this.saveBlob(fileName, blob),
                });
            });
    }

    /** The ported legacy Payroll Sheet, for the month and payroll area. */
    openPayrollSheet(): void {
        const input = this.resultsInput();
        if (!input || this.exporting()) {
            return;
        }

        this.exporting.set('sheet');
        this.proxy
            .getPayrollSheetGrid(input)
            .pipe(finalize(() => this.exporting.set(null)))
            .subscribe((grid) => this.showGrid(grid, () => this.downloadPayrollSheet()));
    }

    /** The ported legacy Payroll Sheet as the Excel file itself. */
    downloadPayrollSheet(): void {
        const end = this.monthEnd();
        if (!end) {
            return;
        }

        const input = new GetPayrollSheetReportInput();
        input.month = end.month;
        input.year = end.year;
        input.payrollRunType = PayrollRunType.Regular;
        input.payrollAreaId = this.results()?.payrollAreaId ?? undefined;
        input.skipCount = 0;
        input.maxResultCount = 100000;

        this.payrollSheetProxy.getPayrollSheetReportToExcel(input).subscribe((file) => this.fileDownload.downloadTempFile(file));
    }

    /** Every register line of the month, one row per employee and wage type. */
    openSalaryRegister(): void {
        const input = this.resultsInput();
        if (!input || this.exporting()) {
            return;
        }
        this.exporting.set('register');
        this.proxy
            .getSalaryRegisterGrid(input)
            .pipe(finalize(() => this.exporting.set(null)))
            .subscribe((grid) => this.showGrid(grid, () => this.downloadSalaryRegister()));
    }

    /** Every register line of the month as the Excel file itself. */
    downloadSalaryRegister(): void {
        const input = this.resultsInput();
        if (input) {
            this.proxy.getSalaryRegisterExcel(input).subscribe((file) => this.fileDownload.downloadTempFile(file));
        }
    }

    groupKey(group: BillingType): string {
        return 'GoLiveV2RegisterGroup' + BillingType[group];
    }

    groupTotal(lines: GoLiveV2SalaryRegisterLineDto[]): number {
        return lines.reduce((sum, l) => sum + (l.amount ?? 0), 0);
    }

    resultsMonth(): string {
        const end = this.monthEnd();
        return end ? end.toFormat('MMMM yyyy') : '';
    }

    /** Answers the control record and pay calendar refusals with the same month and payroll area the run was given. */
    setUpPayrollMonth(): void {
        if (this.settingUp() || !this.canSetUp()) {
            return;
        }

        const input = new GoLiveV2SetUpPayrollMonthInput();
        input.payDate = this.monthEnd().plus({ hours: 12 });
        input.payrollAreaName = this.payrollAreaName().trim();

        this.settingUp.set(true);

        this.proxy
            .setUpPayrollMonth(input)
            .pipe(finalize(() => this.settingUp.set(false)))
            .subscribe((setUp) => {
                // The refusal it answered is now out of date, so it gives way to what was done about it.
                this.latest.set(null);
                this.lastSetUp.set(setUp);
                this.attempts.update((list) => [{ at: new Date(), setUp }, ...list]);
            });
    }

    onPayMonthChange(event: Event): void {
        this.payMonth.set((event.target as HTMLInputElement).value);
    }

    /** Where the run stopped, as a sentence the customer recognises. */
    outcomeKey(result: GoLiveV2RunPayrollOutput): string {
        switch (result.stoppedAt) {
            case GoLiveV2StoppedAt.Input:
                return 'GoLiveV2StoppedAtInput';
            case GoLiveV2StoppedAt.Engine:
                return 'GoLiveV2StoppedAtEngine';
            case GoLiveV2StoppedAt.Employees:
                return 'GoLiveV2StoppedAtEmployees';
            case GoLiveV2StoppedAt.NoEmployees:
                return 'GoLiveV2StoppedAtNoEmployees';
            case GoLiveV2StoppedAt.TimeEngine:
                return 'GoLiveV2StoppedAtTimeEngine';
            case GoLiveV2StoppedAt.MonthSetUp:
                return 'GoLiveV2StoppedAtMonthSetUp';
            case GoLiveV2StoppedAt.ByUser:
                return 'GoLiveV2StoppedAtByUser';
            default:
                return 'GoLiveV2Succeeded';
        }
    }

    /** Whose words the refusal is: the time engine's when it stopped first, otherwise the payroll engine's. */
    saidLabel(result: GoLiveV2RunPayrollOutput): string {
        return result.stoppedAt === GoLiveV2StoppedAt.TimeEngine ? 'GoLiveV2TimeEngineSaid' : 'GoLiveV2EngineSaid';
    }

    payPeriod(result: GoLiveV2RunPayrollOutput): string | null {
        return result.payInDate ? this.l('GoLiveV2PaidUpTo', result.payInDate.toFormat('dd/MM/yyyy')) : null;
    }

    /** "Set up September 2026 for Default Payroll Area" -- the button says exactly what it will do. */
    setUpLabel(): string {
        const end = this.monthEnd();
        return this.l('GoLiveV2SetUpMonthButton', end ? end.toFormat('MMMM yyyy') : '', this.payrollAreaName().trim());
    }

    /** What a set-up did, one sentence per record, including the defaults it chose. */
    describeSetUp(setUp: GoLiveV2SetUpPayrollMonthOutput): string[] {
        const area = setUp.payrollAreaCreated
            ? this.l(
                  'GoLiveV2SetUpCreatedArea',
                  setUp.payrollAreaName,
                  setUp.currencyCode,
                  setUp.payYear,
                  setUp.cutOffDay
              )
            : this.l('GoLiveV2SetUpUsedArea', setUp.payrollAreaName);

        const from = setUp.monthStart.toFormat('dd/MM/yyyy');
        const to = setUp.monthEnd.toFormat('dd/MM/yyyy');
        const record = setUp.controlRecordCreated
            ? this.l('GoLiveV2SetUpCreatedControlRecord', from, to)
            : this.l('GoLiveV2SetUpKeptControlRecord', from, to);

        const calendar = setUp.calendarMonthsCreated
            ? this.l('GoLiveV2SetUpCreatedCalendar', setUp.calendarYear, setUp.calendarMonthsCreated)
            : this.l('GoLiveV2SetUpKeptCalendar', setUp.calendarYear);

        return [area, record, calendar];
    }

    // ---------------------------------------------------------------- internals

    /** One employee's output folded into the whole load's, with the row number the grid shows. */
    private merge(into: GoLiveV2LoadEmployeesOutput, out: GoLiveV2LoadEmployeesOutput, row: number): void {
        into.organizationUnitName = out.organizationUnitName;
        into.payrollAreaName = out.payrollAreaName;
        into.employeeGroupName = out.employeeGroupName;
        into.employeeSubGroupName = out.employeeSubGroupName;
        into.organizationUnitCreated ||= out.organizationUnitCreated;
        into.payrollAreaCreated ||= out.payrollAreaCreated;
        into.employeeGroupCreated ||= out.employeeGroupCreated;
        into.employeeSubGroupCreated ||= out.employeeSubGroupCreated;
        into.sickLeaveRuleCreated ||= out.sickLeaveRuleCreated;
        into.workingTimeCreated ||= out.workingTimeCreated;
        into.gratuitySlabsCreated = (into.gratuitySlabsCreated ?? 0) + out.gratuitySlabsCreated;
        into.leaveEntitlementsCreated = (into.leaveEntitlementsCreated ?? 0) + out.leaveEntitlementsCreated;
        into.placeholdersCreated = (into.placeholdersCreated ?? 0) + out.placeholdersCreated;
        into.employeesCreated = (into.employeesCreated ?? 0) + out.employeesCreated;
        into.employeesAlreadyLoaded = (into.employeesAlreadyLoaded ?? 0) + out.employeesAlreadyLoaded;

        for (const result of out.rows ?? []) {
            result.rowNumber = row;
            into.rows.push(result);
        }
    }

    private isEmptyRow(dto: GoLiveV2EmployeeRowDto): boolean {
        return !dto.code && !dto.fullName && !dto.civilId && !dto.joiningDate && dto.basicSalary === undefined && dto.annualLeaveOpeningBalance === undefined;
    }

    private elapsedMs(p: LoadProgress): number {
        return (p.finishedAt ?? this.now()) - p.startedAt;
    }

    /** mm:ss, or h:mm:ss past the hour. */
    private duration(ms: number): string {
        const total = Math.max(0, Math.round(ms / 1000));
        const h = Math.floor(total / 3600);
        const m = Math.floor((total % 3600) / 60);
        const s = total % 60;
        const mmss = `${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`;
        return h > 0 ? `${h}:${mmss}` : mmss;
    }

    private resultsInput(): GoLiveV2PayrollResultsInput | null {
        const end = this.monthEnd();
        const area = this.payrollAreaName().trim();
        if (!end || !area) {
            return null;
        }
        const input = new GoLiveV2PayrollResultsInput();
        input.payDate = end.plus({ hours: 12 });
        input.payrollAreaName = area;
        return input;
    }

    private startRunPoll(): void {
        this.stopRunPoll();
        this.runPoll = setInterval(() => this.pollRunProgress(), 2000);
    }

    private stopRunPoll(): void {
        if (this.runPoll !== null) {
            clearInterval(this.runPoll);
            this.runPoll = null;
        }
    }

    private pollRunProgress(): void {
        this.proxy.getPayrollRunProgress(this.payrollAreaName().trim()).subscribe({
            next: (progress) => this.followRun(progress, true),
            // A missed poll is not worth a dialog: the next one, two seconds later, will do.
            error: () => undefined,
        });
    }

    private isGoing(status: GoLiveV2RunStatus | undefined): boolean {
        return status === GoLiveV2RunStatus.Queued || status === GoLiveV2RunStatus.Running;
    }

    /**
     * Brings the screen up to where the server's run stands. Going: the window shows its progress and polling
     * carries on. Over: polling stops and the result lands where a finished run's always has -- the result card,
     * the results grid, the bank file setup, and step 3 if everyone could be paid.
     *
     * @param onScreen whether this run was being watched from this page, rather than found on arrival.
     */
    private followRun(progress: GoLiveV2PayrollRunProgressDto, onScreen: boolean): void {
        if (this.isGoing(progress.status)) {
            this.running.set(true);
            this.stopRequested.set(progress.stopRequested);
            this.runProgress.update((p) => ({
                month: progress.payInDate ? progress.payInDate.toFormat('MMMM yyyy') : (p?.month ?? ''),
                startedAt: progress.startedAt ? progress.startedAt.toMillis() : (p?.startedAt ?? Date.now()),
                finishedAt: null,
                server: progress,
            }));
            if (this.runPoll === null) {
                this.startTicker();
                this.startKeepAwake();
                this.startRunPoll();
            }
            return;
        }

        const wasRunning = this.running();
        this.endRunWatch();

        const result = progress.result;
        if (progress.status === GoLiveV2RunStatus.None || !result || (!onScreen && !wasRunning && !this.isUnseen(progress))) {
            this.runProgress.set(null);
            return;
        }

        this.lastSetUp.set(null);
        this.lastLoad.set(null);
        this.latest.set(result);
        this.attempts.update((list) => [{ at: new Date(), result }, ...list]);
        this.showResults();
        this.refreshBankFileSetup();
        if (result.succeeded) {
            this.unlockedStep.update((step) => Math.max(step, 3));
        }

        // Refused before any engine started -- the payroll month, say: the answer is the result card, not a window.
        if (progress.timeDone + progress.payrollDone === 0) {
            this.markRunSeen(progress);
            this.runProgress.set(null);
            return;
        }

        this.runProgress.update((p) => ({
            month: progress.payInDate ? progress.payInDate.toFormat('MMMM yyyy') : (p?.month ?? ''),
            startedAt: progress.startedAt ? progress.startedAt.toMillis() : (p?.startedAt ?? Date.now()),
            finishedAt: progress.finishedAt ? progress.finishedAt.toMillis() : Date.now(),
            server: progress,
        }));
        this.runWindowHidden.set(false);
    }

    /**
     * On arrival: a run still going opens its window on step 2, and one that finished while nobody was looking
     * -- the screen locked, the tab closed -- shows how it ended, once.
     */
    private findRunInProgress(): void {
        this.proxy.getPayrollRunProgress(this.payrollAreaName().trim()).subscribe({
            next: (progress) => {
                const found = this.isGoing(progress.status) || (progress.result && this.isUnseen(progress));
                if (!found) {
                    return;
                }
                if (progress.payInDate) {
                    this.payMonth.set(progress.payInDate.toFormat('yyyy-MM'));
                }
                this.unlockedStep.update((step) => Math.max(step, 2));
                this.activeStep.set(2);
                this.followRun(progress, false);
            },
            error: () => undefined,
        });
    }

    private endRunWatch(): void {
        this.stopRunPoll();
        this.stopTicker();
        this.running.set(false);
        if (!this.loading()) {
            this.stopKeepAwake();
        }
    }

    private runKey(progress: GoLiveV2PayrollRunProgressDto | null): string | null {
        return progress?.startedAt ? `${abp.session.tenantId ?? 0}:${progress.startedAt.toISO()}` : null;
    }

    private isUnseen(progress: GoLiveV2PayrollRunProgressDto): boolean {
        const key = this.runKey(progress);
        if (!key) {
            return false;
        }
        try {
            return localStorage.getItem(SeenRunStorageKey) !== key;
        } catch {
            return false;
        }
    }

    private markRunSeen(progress: GoLiveV2PayrollRunProgressDto | null): void {
        const key = this.runKey(progress);
        if (!key || this.isGoing(progress?.status)) {
            return;
        }
        try {
            localStorage.setItem(SeenRunStorageKey, key);
        } catch {
            // Not remembered: at worst the result is shown again next visit.
        }
    }

    /**
     * The idle lock counts mouse and keyboard only, so a long run watched without touching anything would lock
     * the screen part-way. While a run or a load is going this counts as activity, the same way a mouse move does.
     */
    private startKeepAwake(): void {
        if (this.keepAwake !== null) {
            return;
        }
        window.dispatchEvent(new Event('mousemove'));
        this.keepAwake = setInterval(() => window.dispatchEvent(new Event('mousemove')), KeepAwakeMs);
    }

    private stopKeepAwake(): void {
        if (this.keepAwake !== null) {
            clearInterval(this.keepAwake);
            this.keepAwake = null;
        }
    }

    private startTicker(): void {
        this.stopTicker();
        this.now.set(Date.now());
        this.ticker = setInterval(() => this.now.set(Date.now()), 1000);
    }

    private stopTicker(): void {
        if (this.ticker !== null) {
            clearInterval(this.ticker);
            this.ticker = null;
        }
    }

    /**
     * Opens the step as it was left: the company, and every employee already loaded into the payroll area, in the
     * grid's columns. Only into an untouched step -- anything typed since the page opened is never overwritten.
     */
    private showPreviouslyLoaded(): void {
        this.proxy.getLoadedEmployees(this.payrollAreaName()).subscribe((loaded) => {
            if (!this.organizationUnitName().trim() && loaded.organizationUnitName) {
                this.organizationUnitName.set(loaded.organizationUnitName);
            }

            if (this.filledEmployeeCount() === 0 && loaded.employees?.length) {
                this.employeeRows.set(loaded.employees.map((e) => this.toGridRow(e)));
            }

            if (loaded.employees?.length) {
                this.saturdayIsRestDay.set(loaded.saturdayIsRestDay);
                this.loadedSnapshot.set(
                    this.snapshotOf(
                        loaded.employees.map((e) => this.toGridRow(e)),
                        loaded.organizationUnitName ?? '',
                        loaded.saturdayIsRestDay
                    )
                );
                this.unlockedStep.update((step) => Math.max(step, 2));
            }
        });
    }

    private toGridRow(e: GoLiveV2EmployeeRowDto): PasteGridRow {
        return {
            code: e.code ?? null,
            fullName: e.fullName ?? null,
            joiningDate: e.joiningDate ? e.joiningDate.toISODate() : null,
            civilId: e.civilId ?? null,
            basicSalary: e.basicSalary ?? null,
            accommodationAllowance: e.accommodationAllowance ?? null,
            mobileAllowance: e.mobileAllowance ?? null,
            transportAllowance: e.transportAllowance ?? null,
            foodAllowance: e.foodAllowance ?? null,
            housingAllowance: e.housingAllowance ?? null,
            otherAllowance: e.otherAllowance ?? null,
            annualLeaveOpeningBalance: e.annualLeaveOpeningBalance ?? null,
            bankName: e.bankName ?? null,
            iban: e.iban ?? null,
            accountNumber: e.accountNumber ?? null,
        };
    }

    /**
     * Every row up to the last filled one, blanks included, so the row numbers the server reports back are the
     * ones the grid shows. Joining dates are sent at midday for the same reason the pay date is: a date at local
     * midnight can land on the previous day on a server in another time zone.
     */
    private buildEmployeeRows(): GoLiveV2EmployeeRowDto[] {
        const rows = this.employeeRows();
        let last = -1;
        rows.forEach((row, i) => {
            if (!isRowEmpty(row)) {
                last = i;
            }
        });

        return rows.slice(0, last + 1).map((row) => this.toEmployeeDto(row));
    }

    private toEmployeeDto(row: PasteGridRow): GoLiveV2EmployeeRowDto {
        const dto = new GoLiveV2EmployeeRowDto();
        dto.code = this.text(row['code']);
        dto.fullName = this.text(row['fullName']);
        dto.civilId = this.text(row['civilId']);
        dto.basicSalary = this.amount(row['basicSalary']);
        dto.accommodationAllowance = this.amount(row['accommodationAllowance']);
        dto.mobileAllowance = this.amount(row['mobileAllowance']);
        dto.transportAllowance = this.amount(row['transportAllowance']);
        dto.foodAllowance = this.amount(row['foodAllowance']);
        dto.housingAllowance = this.amount(row['housingAllowance']);
        dto.otherAllowance = this.amount(row['otherAllowance']);
        dto.annualLeaveOpeningBalance = this.amount(row['annualLeaveOpeningBalance']);
        dto.bankName = this.text(row['bankName']);
        dto.iban = this.text(row['iban']);
        dto.accountNumber = this.text(row['accountNumber']);

        const joined = this.text(row['joiningDate']);
        if (joined) {
            const date = DateTime.fromISO(joined);
            if (date.isValid) {
                dto.joiningDate = date.plus({ hours: 12 });
            }
        }

        return dto;
    }

    /**
     * The grid and company by value, as the server would take them: text trimmed, amounts as numbers, blank
     * rows dropped, employees in code order -- so re-sorting the grid, or typing 500 where 500.000 was, is not
     * a change.
     */
    private snapshotOf(rows: PasteGridRow[], company: string, saturdayIsRestDay: boolean): string {
        const employees = rows
            .filter((row) => !isRowEmpty(row))
            .map((row) => {
                const dto = this.toEmployeeDto(row);
                return [
                    dto.code ?? '',
                    dto.fullName ?? '',
                    dto.joiningDate ? dto.joiningDate.toISODate() : (this.text(row['joiningDate']) ?? ''),
                    dto.civilId ?? '',
                    dto.basicSalary ?? null,
                    dto.accommodationAllowance ?? null,
                    dto.mobileAllowance ?? null,
                    dto.transportAllowance ?? null,
                    dto.foodAllowance ?? null,
                    dto.housingAllowance ?? null,
                    dto.otherAllowance ?? null,
                    dto.annualLeaveOpeningBalance ?? null,
                    dto.bankName ?? '',
                    (dto.iban ?? '').replace(/\s+/g, '').toUpperCase(),
                    dto.accountNumber ?? '',
                ];
            })
            .sort((a, b) => String(a[0]).localeCompare(String(b[0])));

        return JSON.stringify({ company: company.trim().toLowerCase(), saturdayIsRestDay, employees });
    }

    /** A blank cell is "not paid", sent as nothing -- never as 0, which the server would read the same way anyway. */
    private amount(value: unknown): number | undefined {
        const text = this.text(value);
        const parsed = text === undefined ? NaN : Number(text);
        return Number.isFinite(parsed) ? parsed : undefined;
    }

    private text(value: unknown): string | undefined {
        const text = value === null || value === undefined ? '' : String(value).trim();
        return text ? text : undefined;
    }

    /**
     * Sends midday on the month's last day rather than midnight on its first. A date is serialised with the
     * browser's offset, so Kuwait midnight on the 1st is still the 31st of the previous month on a server
     * running in UTC -- and the payroll would run for the wrong month. Midday on the last day cannot cross
     * a month boundary in any time zone. The server takes the month end regardless.
     */
    private buildRunInput(): GoLiveV2RunPayrollInput {
        const input = new GoLiveV2RunPayrollInput();
        const end = this.monthEnd();

        if (end) {
            input.payDate = end.plus({ hours: 12 });
        }

        const payrollArea = this.payrollAreaName().trim();
        if (payrollArea) {
            input.payrollAreaName = payrollArea;
        }

        return input;
    }
}

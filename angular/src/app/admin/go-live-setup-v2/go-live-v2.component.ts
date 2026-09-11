import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { DateTime } from 'luxon';
import { firstValueFrom } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import {
    GoLiveV2EmployeeRowDto,
    GoLiveV2EmployeeRowResultDto,
    GoLiveV2LoadEmployeesInput,
    GoLiveV2LoadEmployeesOutput,
    GoLiveV2NextStep,
    GoLiveV2RunPayrollInput,
    GoLiveV2RunPayrollOutput,
    GoLiveV2ServiceProxy,
    GoLiveV2SetUpPayrollMonthInput,
    GoLiveV2SetUpPayrollMonthOutput,
    GoLiveV2StoppedAt,
} from '@shared/service-proxies/service-proxies';
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
    imports: [SubHeaderComponent, LocalizePipe, DatePipe, PasteGridComponent],
})
export class GoLiveV2Component extends AppComponentBase implements OnInit, OnDestroy {
    private readonly proxy = inject(GoLiveV2ServiceProxy);
    private ticker: ReturnType<typeof setInterval> | null = null;

    readonly StoppedAt = GoLiveV2StoppedAt;
    readonly NextStep = GoLiveV2NextStep;

    // ---------------------------------------------------------------- step 1: load employee data

    /** The company, created as the top organisation unit. Asked for, never guessed. */
    readonly organizationUnitName = signal('');

    readonly employeeRows = signal<PasteGridRow[]>([]);
    readonly gridIssues = signal<PasteGridIssue[]>([]);
    readonly loading = signal(false);
    readonly lastLoad = signal<GoLiveV2LoadEmployeesOutput | null>(null);
    readonly progress = signal<LoadProgress | null>(null);
    readonly stopRequested = signal(false);

    /** Ticks once a second while a load runs, so elapsed and time-left count down on their own. */
    readonly now = signal(Date.now());

    readonly employeeColumns: PasteGridColumn[] = [
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
    ];

    readonly filledEmployeeCount = computed(() => this.employeeRows().filter((r) => !isRowEmpty(r)).length);

    readonly canLoad = computed(
        () =>
            this.organizationUnitName().trim().length > 0 &&
            this.filledEmployeeCount() > 0 &&
            this.gridIssues().length === 0 &&
            this.payrollAreaName().trim().length > 0
    );

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

    /** The tenant's payroll areas, with the default always offered: it is created the first time it is used. */
    readonly payrollAreaOptions = signal<string[]>([DefaultPayrollArea]);

    readonly running = signal(false);
    readonly settingUp = signal(false);
    readonly latest = signal<GoLiveV2RunPayrollOutput | null>(null);
    readonly lastSetUp = signal<GoLiveV2SetUpPayrollMonthOutput | null>(null);
    readonly attempts = signal<Attempt[]>([]);

    /** A set-up needs the same two things a run does. */
    readonly canSetUp = computed(() => !!this.monthEnd() && this.payrollAreaName().trim().length > 0);

    ngOnInit(): void {
        this.refreshPayrollAreas();
        this.showPreviouslyLoaded();
    }

    ngOnDestroy(): void {
        this.stopTicker();
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
            this.loading.set(false);
            this.progress.update((p) => ({ ...p, current: null, finishedAt: Date.now(), outcome, errorRow }));
        }

        if (combined.rows.length > 0) {
            // Whatever the last run said is out of date now that there are people to pay.
            this.latest.set(null);
            this.lastSetUp.set(null);
            this.lastLoad.set(combined);
            this.attempts.update((list) => [{ at: new Date(), load: combined }, ...list]);
        }
        this.refreshPayrollAreas();
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

    runPayroll(): void {
        if (this.running()) {
            return;
        }

        this.running.set(true);

        this.proxy
            .runPayroll(this.buildRunInput())
            .pipe(finalize(() => this.running.set(false)))
            .subscribe((result) => {
                this.lastSetUp.set(null);
                this.lastLoad.set(null);
                this.latest.set(result);
                this.attempts.update((list) => [{ at: new Date(), result }, ...list]);
            });
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
                this.refreshPayrollAreas();
            });
    }

    onPayMonthChange(event: Event): void {
        this.payMonth.set((event.target as HTMLInputElement).value);
    }

    onPayrollAreaChange(event: Event): void {
        this.payrollAreaName.set((event.target as HTMLSelectElement).value);
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
        return !dto.code && !dto.fullName && !dto.civilId && !dto.joiningDate && dto.basicSalary === undefined;
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
        };
    }

    private refreshPayrollAreas(): void {
        this.proxy.getPayrollAreaNames().subscribe((names) => {
            const list = names.items ?? [];
            this.payrollAreaOptions.set(list.includes(DefaultPayrollArea) ? list : [DefaultPayrollArea, ...list]);
        });
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

        return rows.slice(0, last + 1).map((row) => {
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

            const joined = this.text(row['joiningDate']);
            if (joined) {
                const date = DateTime.fromISO(joined);
                if (date.isValid) {
                    dto.joiningDate = date.plus({ hours: 12 });
                }
            }

            return dto;
        });
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

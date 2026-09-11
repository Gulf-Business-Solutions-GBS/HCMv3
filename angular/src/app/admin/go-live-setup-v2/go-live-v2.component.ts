import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { DateTime } from 'luxon';
import { finalize } from 'rxjs/operators';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import {
    GoLiveV2EmployeeRowDto,
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
export class GoLiveV2Component extends AppComponentBase implements OnInit {
    private readonly proxy = inject(GoLiveV2ServiceProxy);

    readonly StoppedAt = GoLiveV2StoppedAt;
    readonly NextStep = GoLiveV2NextStep;

    // ---------------------------------------------------------------- step 1: load employee data

    /** The company, created as the top organisation unit. Asked for, never guessed. */
    readonly organizationUnitName = signal('');

    readonly employeeRows = signal<PasteGridRow[]>([]);
    readonly gridIssues = signal<PasteGridIssue[]>([]);
    readonly loading = signal(false);
    readonly lastLoad = signal<GoLiveV2LoadEmployeesOutput | null>(null);

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
    }

    // ---------------------------------------------------------------- step 1

    loadEmployees(): void {
        if (this.loading() || !this.canLoad()) {
            return;
        }

        const input = new GoLiveV2LoadEmployeesInput();
        input.organizationUnitName = this.organizationUnitName().trim();
        input.payrollAreaName = this.payrollAreaName().trim();
        input.employees = this.buildEmployeeRows();

        this.loading.set(true);

        this.proxy
            .loadEmployees(input)
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((load) => {
                // Whatever the last run said is out of date now that there are people to pay.
                this.latest.set(null);
                this.lastSetUp.set(null);
                this.lastLoad.set(load);
                this.attempts.update((list) => [{ at: new Date(), load }, ...list]);
                this.refreshPayrollAreas();
            });
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
        return this.l('GoLiveV2LoadButton', this.filledEmployeeCount());
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

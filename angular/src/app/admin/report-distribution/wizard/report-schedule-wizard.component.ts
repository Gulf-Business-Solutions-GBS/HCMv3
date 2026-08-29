import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { DateTime } from 'luxon';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import {
    ReportSchedulesServiceProxy,
    CreateOrEditReportScheduleDto,
    ReportScheduleFilterDto,
    ReportScheduleRecipientDto,
    ReportScheduleDestinationDto,
    ScheduledReportKind,
    ScheduledReportFormat,
    ReportBurstDimension,
    ReportRecipientRule,
    ReportDestinationMethod,
    ReportScheduleOccurrenceType,
    ReportRecurringPattern,
    SchedulableReportDto,
    SchedulableReportParameterDto,
    EntityDto,
} from '@shared/service-proxies/service-proxies';

/**
 * Four-step Report Distribution wizard: Definition, Filter, Destination, Occurrence.
 *
 * The step order follows the customer's reference screens. Two departures from them, both
 * deliberate:
 *
 *  1. RECIPIENTS ARE RULES, NOT ADDRESSES. The reference screen has a free-text "send to" box. This
 *     wizard asks for a rule -- a role, the org unit's head, the line manager, or an explicit
 *     address -- because a schedule outlives the org chart. A stored address keeps mailing payroll
 *     data to a secretary who moved departments two years ago; a rule follows the org chart.
 *
 *  2. NO FTP PASSWORD FIELD. The reference screen collects one. Storing it would put a live
 *     credential in every database backup and audit log, so this asks for the NAME of a secret held
 *     in the encrypted settings store instead.
 */
@Component({
    templateUrl: './report-schedule-wizard.component.html',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, BusyIfDirective, FormsModule, LocalizePipe, PermissionPipe],
})
export class ReportScheduleWizardComponent extends AppComponentBase implements OnInit {
    private _service = inject(ReportSchedulesServiceProxy);
    private _route = inject(ActivatedRoute);
    private _router = inject(Router);

    readonly isLoading = signal<boolean>(false);
    readonly currentStep = signal<number>(1);
    readonly isEdit = signal<boolean>(false);

    schedule = new CreateOrEditReportScheduleDto();

    // Enums exposed for template binding. ReportKind and Format are no longer among them: both are
    // now decided rather than asked for.
    readonly Burst = ReportBurstDimension;
    readonly Rule = ReportRecipientRule;
    readonly Destination = ReportDestinationMethod;
    readonly Occurrence = ReportScheduleOccurrenceType;
    readonly Pattern = ReportRecurringPattern;

    readonly stepTitles = ['ScheduleDefinition', 'Filter', 'Destination', 'ScheduleOccurrence'];

    /**
     * Whether any recipient rule asks to bypass the recipient's own data scope. Drives the warning
     * banner on the Destination step -- this is the one setting in the module that can hand somebody
     * data they are not otherwise entitled to, so it should never be quietly checked.
     */
    readonly hasScopeBypass = computed(() =>
        (this.schedule.recipients ?? []).some((r) => r.bypassRecipientScope)
    );

    /** Every report that can be scheduled, discovered on the server. */
    readonly availableReports = signal<SchedulableReportDto[]>([]);

    /** Grouped for the dropdown's optgroups, so ~138 reports stay navigable. */
    readonly reportsByCategory = computed(() => {
        const groups = new Map<string, SchedulableReportDto[]>();

        for (const report of this.availableReports()) {
            const key = report.category || 'Reports';
            if (!groups.has(key)) {
                groups.set(key, []);
            }
            groups.get(key).push(report);
        }

        return Array.from(groups.entries())
            .map(([category, reports]) => ({ category, reports }))
            .sort((a, b) => a.category.localeCompare(b.category));
    });

    /** Parameters of the currently selected report, used to build the Filter step. */
    readonly selectedReportParameters = computed(() => {
        const selected = this.availableReports().find((r) => r.reportKey === this.schedule.reportKey);
        return selected?.parameters ?? [];
    });

    ngOnInit(): void {
        this.loadAvailableReports();

        const id = this._route.snapshot.params['id'];

        if (id) {
            this.isEdit.set(true);
            this.load(+id);
        } else {
            this.initNew();
        }
    }

    private loadAvailableReports(): void {
        this._service.getSchedulableReports().subscribe((result) => {
            this.availableReports.set(result.items ?? []);
        });
    }

    /**
     * Seeds the Filter rows from the newly chosen report's own parameters.
     *
     * Only on a real change, and only when nothing has been entered yet: silently discarding filters
     * somebody has already filled in would be worse than leaving a stale row behind.
     */
    onReportChanged(): void {
        // The family comes FROM the report rather than being asked for separately, so it cannot
        // disagree with the report actually chosen and needs no wizard change when a new family
        // becomes renderable.
        const selected = this.availableReports().find((r) => r.reportKey === this.schedule.reportKey);
        if (selected) {
            this.schedule.reportKind = selected.reportKind;
        }

        if (this.schedule.filters.length > 0) {
            return;
        }

        for (const parameter of this.selectedReportParameters()) {
            const filter = new ReportScheduleFilterDto();
            filter.parameterName = parameter.name;
            this.schedule.filters.push(filter);
        }
    }

    /** The declared type of a filter row's parameter, so the template can pick an input control. */
    parameterFor(filter: ReportScheduleFilterDto): SchedulableReportParameterDto | undefined {
        return this.selectedReportParameters().find((p) => p.name === filter.parameterName);
    }

    private initNew(): void {
        this.schedule = new CreateOrEditReportScheduleDto();
        this.schedule.reportKind = ScheduledReportKind.GridReport;
        this.schedule.format = ScheduledReportFormat.Excel;
        this.schedule.burstDimension = ReportBurstDimension.None;
        this.schedule.occurrenceType = ReportScheduleOccurrenceType.Once;
        this.schedule.isActive = true;
        this.schedule.notifyOnStart = true;
        this.schedule.notifyOnCompletion = true;
        this.schedule.filters = [];
        this.schedule.recipients = [];
        this.schedule.destinations = [];
    }

    private load(id: number): void {
        this.isLoading.set(true);
        this._service
            .getForEdit(id)
            .pipe(finalize(() => this.isLoading.set(false)))
            .subscribe((result) => {
                this.schedule = result.reportSchedule;
                this.schedule.filters = this.schedule.filters ?? [];
                this.schedule.recipients = this.schedule.recipients ?? [];
                this.schedule.destinations = this.schedule.destinations ?? [];

                // Excel is the only format the renderer produces. Normalising here stops an edit
                // screen from displaying a stored PDF/CSV choice that has never been honoured.
                this.schedule.format = ScheduledReportFormat.Excel;
            });
    }

    goToStep(step: number): void {
        if (step < 1 || step > 4) {
            return;
        }

        this.currentStep.set(step);
    }

    next(): void {
        this.goToStep(this.currentStep() + 1);
    }

    previous(): void {
        this.goToStep(this.currentStep() - 1);
    }

    // ---- filters -------------------------------------------------------------------------------

    /**
     * Rebuilds the filter rows from the selected report's parameters, keeping any values already
     * entered for parameters that still exist.
     *
     * Needed when the report changes after filters were entered, and when editing a schedule saved
     * before a parameter was added to its report -- otherwise the new parameter is invisible and
     * silently runs at its default forever.
     */
    resetFiltersFromReport(): void {
        const existing = new Map(this.schedule.filters.map((f) => [f.parameterName, f]));

        this.schedule.filters = this.selectedReportParameters().map((parameter) => {
            const previous = existing.get(parameter.name);
            if (previous) {
                return previous;
            }

            const filter = new ReportScheduleFilterDto();
            filter.parameterName = parameter.name;
            return filter;
        });
    }

    removeFilter(index: number): void {
        this.schedule.filters.splice(index, 1);
    }

    // ---- recipients ----------------------------------------------------------------------------

    addRecipient(): void {
        const recipient = new ReportScheduleRecipientDto();
        recipient.rule = ReportRecipientRule.RoleScoped;
        // Defaults to scoping the role to the slice's own unit: "the Department Secretary" almost
        // always means THIS department's, and the wrong default here mails every department's data
        // to every department's secretary.
        recipient.scopeToBurstUnit = true;
        recipient.bypassRecipientScope = false;
        this.schedule.recipients.push(recipient);
    }

    removeRecipient(index: number): void {
        this.schedule.recipients.splice(index, 1);
    }

    /** Bypassing scope is only meaningful for an explicit external address. */
    canBypassScope(recipient: ReportScheduleRecipientDto): boolean {
        return recipient.rule === ReportRecipientRule.Explicit;
    }

    onRecipientRuleChanged(recipient: ReportScheduleRecipientDto): void {
        if (recipient.rule !== ReportRecipientRule.Explicit) {
            // Clearing it on rule change keeps the server-side refusal from firing on a value the
            // user can no longer see.
            recipient.bypassRecipientScope = false;
        }
    }

    // ---- destinations --------------------------------------------------------------------------

    addDestination(): void {
        const destination = new ReportScheduleDestinationDto();
        destination.method = ReportDestinationMethod.Email;
        destination.emailSubjectTemplate = '{ReportName} - {SliceName}';
        this.schedule.destinations.push(destination);
    }

    removeDestination(index: number): void {
        this.schedule.destinations.splice(index, 1);
    }

    // ---- dates ---------------------------------------------------------------------------------
    //
    // The proxies type these as luxon DateTime objects, and <input type="datetime-local"> wants a plain
    // 'YYYY-MM-DDTHH:mm' string. Converting in both directions here keeps the template free of the
    // any-casts that otherwise hide real type errors.

    firstOccurrenceText(): string {
        return this.toLocalInput(this.schedule.firstOccurrence, true);
    }

    setFirstOccurrence(value: string): void {
        this.schedule.firstOccurrence = value ? (DateTime.fromISO(value) as any) : (undefined as any);
    }

    endingOnText(): string {
        return this.toLocalInput(this.schedule.endingOn, false);
    }

    setEndingOn(value: string): void {
        this.schedule.endingOn = value ? (DateTime.fromISO(value) as any) : (undefined as any);
    }

    private toLocalInput(value: any, withTime: boolean): string {
        if (!value) {
            return '';
        }

        const dt = value instanceof DateTime ? value : DateTime.fromISO(String(value));
        return dt.isValid ? dt.toFormat(withTime ? "yyyy-MM-dd'T'HH:mm" : 'yyyy-MM-dd') : '';
    }

    // ---- save ----------------------------------------------------------------------------------

    save(): void {
        this.isLoading.set(true);
        this._service
            .createOrEdit(this.schedule)
            .pipe(finalize(() => this.isLoading.set(false)))
            .subscribe(() => {
                this.notify.success(this.l('SavedSuccessfully'));
                this._router.navigate(['/app/admin/report-distribution']);
            });
    }

    cancel(): void {
        this._router.navigate(['/app/admin/report-distribution']);
    }
}

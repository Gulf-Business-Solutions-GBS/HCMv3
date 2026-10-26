import {
    ChangeDetectionStrategy,
    Component,
    computed,
    effect,
    inject,
    input,
    output,
    signal,
    untracked,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import {
    LeavePlanPreviewDto,
    MyLeavePlanServiceProxy,
    PreviewLeavePlanInput,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { EssRequestFormMode } from '../my-exit-permit/exit-permit-form.component';

/**
 * The leave plan as the form holds it. Dates are ISO yyyy-MM-dd, which is what the date inputs read and write and
 * what the workflow projection sends, so the approver's read-only copy needs no parsing.
 */
export interface LeavePlanFormValue {
    /** "1042 - Ahmad Al Sayed". Shown, never chosen: self service is always the signed-in employee. */
    employeeDisplay: string;

    startDate: string;
    endDate: string;
    comments: string;

    /** Read-only modes only: the figures as the request carried them. Create mode asks the server instead. */
    plannedDays?: string;
    projectedLeaveBalance?: string;
}

export function emptyLeavePlanForm(employeeDisplay = ''): LeavePlanFormValue {
    return { employeeDisplay, startDate: '', endDate: '', comments: '' };
}

/**
 * The leave plan request form (legacy ESS <c>Views/LeavePlanner/_RequestForm.cshtml</c>) -- the same form in all
 * three of its jobs, as the exit permit's is: the requester fills it in, and Details and the approver see this very
 * markup with the inputs disabled.
 *
 * <p>Legacy put Start Date, End Date, "Planned Calendar Days" and "Projected Leave Balance" in one row. Here the two
 * figures sit on the right as Leave Info, as on the leave form, recalculated whenever a date changes (legacy
 * <c>LeavePlannerRequestForm.js</c> <c>OnLeaveDateChange</c>). Leave Days is added beside them: what the same dates
 * would charge as annual leave, so the plan can be read against the balance.</p>
 */
@Component({
    selector: 'leave-plan-form',
    templateUrl: './leave-plan-form.component.html',
    imports: [DateInputDirective, CommonModule, FormsModule, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LeavePlanFormComponent extends AppComponentBase {
    private readonly _service = inject(MyLeavePlanServiceProxy);

    readonly mode = input<EssRequestFormMode>('create');

    /** The host's own object in create mode, written into through ngModel. */
    readonly value = input.required<LeavePlanFormValue>();

    readonly saving = input(false);

    /** Save as draft: files the plan without sending it to an approver. */
    readonly saved = output<void>();
    readonly submitted = output<void>();
    readonly cancelled = output<void>();

    readonly readOnly = computed(() => this.mode() !== 'create');

    /** The server's figures for the dates on the form; create mode only. */
    readonly preview = signal<LeavePlanPreviewDto | undefined>(undefined);

    constructor() {
        super();

        // A form reopened on a draft already has dates: ask for its figures straight away, as legacy's
        // getInitial... did, rather than showing dashes until somebody touches a date.
        effect(() => {
            const value = this.value();
            if (!this.readOnly()) {
                untracked(() => this.refreshPreview(value));
            }
        });
    }

    /** Leave Info, whichever mode: the live preview while filling in, the request's own figures once sent. */
    readonly info = computed(() => {
        const round = (n: number | undefined | null) =>
            n === undefined || n === null ? '' : `${Math.round(n * 1000) / 1000}`;

        if (this.readOnly()) {
            return {
                plannedDays: this.value().plannedDays ?? '',
                projected: this.value().projectedLeaveBalance ?? '',
                leaveDays: '',
            };
        }

        const preview = this.preview();
        return {
            plannedDays: preview && preview.plannedDays > 0 ? round(preview.plannedDays) : '',
            projected: round(preview?.projectedLeaveBalance),
            leaveDays: round(preview?.leaveDays),
        };
    });

    onDateChange(): void {
        this.refreshPreview(this.value());
    }

    private refreshPreview(value: LeavePlanFormValue): void {
        const input = new PreviewLeavePlanInput();
        input.startDate = value.startDate ? DateTime.fromISO(value.startDate) : undefined;
        input.endDate = value.endDate ? DateTime.fromISO(value.endDate) : undefined;

        if (!input.startDate && !input.endDate) {
            this.preview.set(undefined);
            return;
        }

        this._service.previewMyLeavePlan(input).subscribe({
            next: (result) => this.preview.set(result),
            // The figures are information; a failure to fetch them must not block the form.
            error: () => this.preview.set(undefined),
        });
    }

    /** A stored date shown the way the rest of the system shows one, rearranged as text so no time zone moves it. */
    asDayMonthYear(iso: string | undefined): string {
        const [year, month, day] = (iso ?? '').split('-');
        return year && month && day ? `${day.substring(0, 2)}/${month}/${year}` : '';
    }
}

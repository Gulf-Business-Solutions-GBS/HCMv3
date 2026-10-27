import { DateInputDirective } from '@shared/common/dates/date-input.directive';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { TravelReason } from '@shared/service-proxies/service-proxies';

/** Which of the three jobs this form is doing. */
export type EssRequestFormMode = 'create' | 'view' | 'approve';

/**
 * The exit permit as the form holds it. Dates are ISO <c>yyyy-MM-dd</c> because that is what
 * <c>&lt;input type="date"&gt;</c> reads and writes, in create mode and read-only alike.
 */
export interface ExitPermitFormValue {
    /** "1042 - Ahmad Al Sayed". Shown, never chosen -- see the note on the field itself. */
    employeeDisplay: string;

    externalRequestNumber: string;
    travelReason: TravelReason;

    /** Used only when the form is read-only, where the reason is displayed rather than picked. */
    travelReasonLabel?: string;

    startDate: string;
    exitDate: string;
    comments: string;
}

/**
 * The exit permit request form -- the SAME form in all three of its jobs.
 *
 * <p><b>Why this component exists.</b> The approver used to get a generic dialog of label/value rows while
 * the requester filled in a form, and the user's verdict was blunt: "he cannot see the same details field
 * wise, i want same layout same screen at the time of request create". Not a tidier list -- the actual
 * screen. So the form moved out of <c>MyExitPermitComponent</c> into a component that three callers share,
 * and the only thing that varies is whether its inputs accept typing.</p>
 *
 * <p>That is also the legacy ESS model, which had one <c>RequestForm</c> view serving create, view and
 * approve with an <c>isApprover</c> flag deciding the action bar. It is worth copying because the property
 * it guarantees is the one the user is asking for: there is no second layout that can drift from the first,
 * because there is no second layout.</p>
 *
 * <p><b>Read-only means disabled, not rearranged.</b> Every field keeps its column and its width; a
 * disabled input sits exactly where the live one did. Reflowing into a two-column grid would have thrown
 * away the whole point.</p>
 *
 * <p><b>Bootstrap 5 traps already paid for on this screen -- do not reintroduce them.</b> There is no
 * <c>.form-row</c> and no <c>.form-group</c> in this theme, so the columns need a real <c>.row</c> parent or
 * every <c>col-*</c> stacks into a narrow block. And <c>.form-control</c> sets <c>appearance: none</c>,
 * which strips a native select's caret: the reason picker must be <c>.form-select</c>.</p>
 */
@Component({
    selector: 'exit-permit-form',
    templateUrl: './exit-permit-form.component.html',
    imports: [DateInputDirective, CommonModule, FormsModule, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExitPermitFormComponent extends AppComponentBase {
    readonly mode = input<EssRequestFormMode>('create');

    /**
     * The form's data. In create mode the host passes its own object and this component writes into it
     * through ngModel -- same reference, so the host sees every keystroke without an output per field.
     */
    readonly value = input.required<ExitPermitFormValue>();

    /** Disabled while the host is saving, so a double Submit cannot raise two permits. */
    readonly saving = input(false);

    /**
     * A stored date shown the way the rest of the system shows one. Read-only, so it is plain text rather than a
     * field, and rearranged as text rather than through a date so that no time zone can move it a day.
     */
    asDayMonthYear(iso: string | undefined): string {
        const [year, month, day] = (iso ?? '').split('-');
        return year && month && day ? `${day.substring(0, 2)}/${month}/${year}` : '';
    }

    /** Save as draft: files the permit without sending it to an approver. */
    readonly saved = output<void>();

    readonly submitted = output<void>();
    readonly cancelled = output<void>();

    /** Listed rather than derived, so the order on screen is a decision rather than an accident. */
    readonly travelReasons: TravelReason[] = [
        TravelReason.PlannedLeave,
        TravelReason.EmergencyLeave,
        TravelReason.PersonalTravel,
    ];

    readonly readOnly = computed(() => this.mode() !== 'create');

    /**
     * What to show in the reason box when it cannot be used.
     *
     * <p>Prefers the label the server sent: in read-only mode the value came from the request's projection,
     * which already resolved the enum in the viewer's language, and re-deriving it from a numeric enum the
     * client may not have been given is how a blank box appears.</p>
     */
    readonly readOnlyReasonLabel = computed(
        () => this.value().travelReasonLabel || this.travelReasonLabel(this.value().travelReason)
    );

    travelReasonLabel(reason: TravelReason | undefined): string {
        if (reason === undefined || reason === null) {
            return '';
        }

        return this.l('TravelReason_' + TravelReason[reason]);
    }
}

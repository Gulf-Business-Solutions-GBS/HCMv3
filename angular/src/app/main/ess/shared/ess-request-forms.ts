import { Type } from '@angular/core';
import {
    EssRequestFormMode,
    ExitPermitFormComponent,
    ExitPermitFormValue,
} from '../my-exit-permit/exit-permit-form.component';
import { HalfdayType, LeavePaymentType, TravelReason, WorkflowRequestAction } from '@shared/service-proxies/service-proxies';
import { LeaveFormValue, LeaveRequestFormComponent } from '../my-leave/leave-request-form.component';

/**
 * What the modal knows about the request while it builds the form's value: the projected fields, keyed by
 * their stable names, plus the identity the request itself carries rather than its transaction row.
 */
export interface EssRequestFormContext {
    /** Every projected field, keyed by <c>WorkflowDetailFieldDto.name</c>. */
    values: Record<string, string>;

    /** "1042 - Ahmad Al Sayed", from the request's requester rather than from the transaction. */
    employeeDisplay: string;
}

/** One transaction type's own form, and how to fill it from a request's projection. */
export interface EssRequestFormRegistration {
    component: Type<unknown>;

    /** Builds the component's <c>value</c> input. Missing fields become empty, never undefined. */
    toValue(context: EssRequestFormContext): unknown;
}

/**
 * The transaction types whose own request form can stand in for the generic field list.
 *
 * <p><b>Why a registry rather than a branch in the modal.</b> There are thirty transaction types and one
 * approver screen, and it has to stay one. A type that has adapted its form registers here and its
 * approvers get the real thing; a type that has not is not broken by that -- it falls through to the
 * projected label/value list, which is still every field its handler publishes. So the two mechanisms are
 * not competing designs: the projection is the floor that made all thirty types usable at once, and this is
 * how a type climbs off it. Adding the next form is one entry here plus a mode input on that form.</p>
 *
 * <p>Keyed by the catalog code -- <c>WorkflowTransactionType.Code</c>, which is what
 * <c>WorkflowRequest.RequestType</c> holds.</p>
 */
export const ESS_REQUEST_FORMS: Record<string, EssRequestFormRegistration> = {
    EmployeeAbsence: {
        component: LeaveRequestFormComponent,
        toValue: (context): LeaveFormValue => {
            const v = context.values;
            const dmy = (iso: string | undefined) => {
                const [year, month, day] = (iso ?? '').split('-');
                return year && month && day ? `${day.substring(0, 2)}/${month}/${year}` : '';
            };
            const person = (name: string | undefined) => (name ? { id: 0, label: name } : null);

            // What is being asked for: the leave itself, or an amendment, cancellation or return against it.
            const action =
                WorkflowRequestAction[(v['RequestAction'] ?? 'New') as keyof typeof WorkflowRequestAction] ?? WorkflowRequestAction.New;

            return {
                action,
                employeeDisplay: context.employeeDisplay,
                absenceTypeId: null,
                leaveTypeLabel: v['LeaveType'] ?? '',

                // Already ISO yyyy-MM-dd from the projection. For an amendment these are the PROPOSED dates;
                // the dates on file today are in currentLeave below.
                startDate: v['StartDate'] ?? '',
                endDate: v['EndDate'] ?? '',
                halfdayType: HalfdayType.None,
                halfdayLabel: v['HalfdayType'] ?? '',
                startTime: v['StartTime'] ?? '',
                endTime: v['EndTime'] ?? '',
                leavePaymentType: LeavePaymentType.PayWithPayroll,
                leavePaymentLabel: v['LeavePaymentType'] ?? '',
                paymentDate: v['PaymentDate'] ?? '',
                isTravelling: v['IsTravelling'] === 'true',
                primaryDelegate: person(v['PrimaryDelegation']),
                secondaryDelegate: person(v['SecondaryDelegation']),
                approverDelegate: person(v['ApproverDelegation']),
                policyAccepted: false,
                comments: v['Reason'] ?? v['Comments'] ?? '',
                returnDate: v['ReturnDate'] ?? '',
                paidStartDate: v['PaidLeaveStartDate'] ?? '',
                paidEndDate: v['PaidLeaveEndDate'] ?? '',
                unpaidStartDate: v['UnpaidLeaveStartDate'] ?? '',
                unpaidEndDate: v['UnpaidLeaveEndDate'] ?? '',
                currentLeave: v['CurrentStartDate']
                    ? `${v['CurrentLeaveType'] ?? ''} · ${dmy(v['CurrentStartDate'])} – ${dmy(v['CurrentEndDate'])} · ${v['CurrentLeaveDays'] ?? ''}`
                    : undefined,
                calendarDays: v['CalendarDays'] ?? '',
                leaveDays: v['LeaveDays'] ?? '',
                currentBalance: v['CurrentBalance'] ?? '',
                bookableBalance: v['BookableBalance'] ?? '',
                updatedBalance: v['UpdatedBalance'] ?? '',
            };
        },
    },
    EmployeeExitPermit: {
        component: ExitPermitFormComponent,
        toValue: (context): ExitPermitFormValue => ({
            employeeDisplay: context.employeeDisplay,
            externalRequestNumber: context.values['ExternalRequestNumber'] ?? '',

            // The picker is disabled in every mode this registry serves, so the numeric reason is never
            // read -- the label the server already localized is what gets shown.
            travelReason: TravelReason.PlannedLeave,
            travelReasonLabel: context.values['TravelReason'] ?? '',

            // Already ISO yyyy-MM-dd: the projection sends dates in the transport form <input type="date">
            // expects, precisely so this needs no parsing and no timezone guesswork.
            startDate: context.values['StartDate'] ?? '',
            exitDate: context.values['ExitDate'] ?? '',
            comments: context.values['Comments'] ?? '',
        }),
    },
};

/** The registration for a request type, or undefined when it has none yet. */
export function findRequestForm(requestType: string | undefined): EssRequestFormRegistration | undefined {
    return requestType ? ESS_REQUEST_FORMS[requestType] : undefined;
}

export type { EssRequestFormMode };

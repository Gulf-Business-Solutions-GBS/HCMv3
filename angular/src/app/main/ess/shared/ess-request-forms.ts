import { Type } from '@angular/core';
import {
    EssRequestFormMode,
    ExitPermitFormComponent,
    ExitPermitFormValue,
} from '../my-exit-permit/exit-permit-form.component';
import {
    HalfdayType,
    LeavePaymentType,
    TravelReason,
    WorkflowDetailFieldDto,
    WorkflowDetailFieldKind,
    WorkflowRequestAction,
} from '@shared/service-proxies/service-proxies';
import { PersonalDataChangesComponent, PersonalDataChangesValue } from '../my-personal-data/personal-data-changes.component';
import { LeaveFormValue, LeaveRequestFormComponent } from '../my-leave/leave-request-form.component';
import { DocumentRequestFormComponent, DocumentRequestFormValue } from '../my-document/document-request-form.component';
import { LetterRequestFormComponent, LetterRequestFormValue } from '../my-letter/letter-request-form.component';
import {
    PassportWithdrawalFormComponent,
    PassportWithdrawalFormValue,
} from '../my-passport-withdrawal/passport-withdrawal-form.component';

/**
 * What the modal knows about the request while it builds the form's value: the projected fields, keyed by
 * their stable names, plus the identity the request itself carries rather than its transaction row.
 */
export interface EssRequestFormContext {
    /** Every projected field, keyed by <c>WorkflowDetailFieldDto.name</c>. */
    values: Record<string, string>;

    /** "1042 - Ahmad Al Sayed", from the request's requester rather than from the transaction. */
    employeeDisplay: string;

    /** The transaction row the request governs, which is what its files are attached to. */
    targetEntityId?: number;

    /**
     * The projected fields themselves, in the handler's order, for a form that needs more of a field than
     * its value: its label, and what it held before the request.
     */
    fields?: WorkflowDetailFieldDto[];
}

/** A projected value as a reader sees it: a date as dd/MM/yyyy, anything else as sent. */
function displayValue(raw: string | undefined, kind: WorkflowDetailFieldKind | undefined): string {
    if (!raw) {
        return '';
    }

    if (kind === WorkflowDetailFieldKind.Date) {
        const [year, month, day] = raw.split('-');

        return year && month && day ? `${day.substring(0, 2)}/${month}/${year}` : raw;
    }

    return raw;
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
    EmployeeDocumentMvmt: {
        component: PassportWithdrawalFormComponent,
        toValue: (context): PassportWithdrawalFormValue => {
            const v = context.values;

            return {
                // Which of the two requests this is: the withdrawal itself, or the document handed back.
                action: v['RequestAction'] === 'Return' ? WorkflowRequestAction.Return : WorkflowRequestAction.New,
                employeeDisplay: v['Employee'] || context.employeeDisplay,

                // The pickers are disabled in every mode this registry serves, so the ids are never read --
                // the names the server already put in the reader's language are what gets shown.
                employeeDocumentId: null,
                documentLabel: v['DocumentType'] ?? '',
                documentNumber: v['DocumentNumber'] ?? '',

                // Already ISO yyyy-MM-dd from the projection.
                issueDate: v['IssueDate'] ?? '',
                expiryDate: v['ExpiryDate'] ?? '',
                withdrawDate: v['WithdrawDate'] ?? '',
                returnDate: v['ReturnDate'] ?? '',
                guarantorId: null,
                guarantorLabel: v['Guarantor'] ?? '',
                withdrawalReason: v['WithdrawalReason'] ?? '',
                comments: v['Comments'] ?? '',

                // Sent only once they exist: the hand-over when it has happened, the return on a return
                // request or once the document has come back.
                handedOverOn: v['HandedOverOn'] ?? '',
                actualReturnDate: v['ActualReturnDate'] ?? '',
                returnRemarks: v['ReturnRemarks'] ?? '',
            };
        },
    },
    EmployeeLetterRequest: {
        component: LetterRequestFormComponent,
        toValue: (context): LetterRequestFormValue => ({
            id: context.targetEntityId,
            employeeDisplay: context.values['Employee'] || context.employeeDisplay,

            // The picker is disabled in every mode this registry serves, so the id is never read.
            documentTypeId: null,
            documentTypeLabel: context.values['DocumentType'] ?? '',

            // Already ISO yyyy-MM-dd from the projection.
            effectiveDate: context.values['EffectiveDate'] ?? '',
            addressToEn: context.values['AddressToEn'] ?? '',
            addressToAr: context.values['AddressToAr'] ?? '',
            comments: context.values['Comments'] ?? '',
        }),
    },
    EmployeeDocument: {
        component: DocumentRequestFormComponent,
        toValue: (context): DocumentRequestFormValue => ({
            id: context.targetEntityId,
            employeeDisplay: context.values['Employee'] || context.employeeDisplay,

            // The picker is disabled in every mode this registry serves, so the id is never read.
            documentTypeId: null,
            documentTypeLabel: context.values['DocumentType'] ?? '',
            documentNum: context.values['DocumentNumber'] ?? '',

            // Already ISO yyyy-MM-dd from the projection.
            issueDate: context.values['IssueDate'] ?? '',
            expiryDate: context.values['ExpiryDate'] ?? '',
            comments: context.values['Comments'] ?? '',
            description: context.values['Description'] ?? '',
        }),
    },
    EmployeePersonalDataChange: {
        // Not the form the requester filled in, on purpose: that has forty fields and a request changes a
        // few. The approver is shown those few, what each held before beside what is proposed.
        component: PersonalDataChangesComponent,
        toValue: (context): PersonalDataChangesValue => ({
            id: context.targetEntityId,
            employeeDisplay: context.employeeDisplay,
            changes: (context.fields ?? [])
                .filter((field) => field.isChange)
                .map((field) => ({
                    label: field.label ?? '',
                    oldValue: displayValue(field.previousValue, field.kind),
                    newValue: displayValue(field.value, field.kind),
                })),
        }),
    },
};

/** The registration for a request type, or undefined when it has none yet. */
export function findRequestForm(requestType: string | undefined): EssRequestFormRegistration | undefined {
    return requestType ? ESS_REQUEST_FORMS[requestType] : undefined;
}

export type { EssRequestFormMode };

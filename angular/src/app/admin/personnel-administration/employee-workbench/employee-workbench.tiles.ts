import { Injector, OutputEmitterRef, Type } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import {
    EmployeeAbsenceQuotasServiceProxy,
    EmployeeAbsencesServiceProxy,
    EmployeeBasicPaysServiceProxy,
    EmployeeDocumentsServiceProxy,
    EmployeeFnFPaymentsServiceProxy,
    EmployeeGratuitiesServiceProxy,
    EmployeeLoansServiceProxy,
    EmployeeManagersServiceProxy,
    EmployeeOrgAssignmentsServiceProxy,
    EmployeePaymentsServiceProxy,
    EmployeeSeparationsServiceProxy,
    EmployeeWorkbenchServiceProxy,
    GetAllEmployeeAbsenceQuotasInput,
    GetAllEmployeeAbsencesInput,
    GetAllEmployeeBasicPaysInput,
    GetAllEmployeeDocumentsInput,
    GetAllEmployeeFnFPaymentsInput,
    GetAllEmployeeGratuitiesInput,
    GetAllEmployeeLoansInput,
    GetAllEmployeeManagersInput,
    GetAllEmployeeOrgAssignmentsInput,
    GetAllEmployeePaymentsInput,
    GetAllEmployeeSeparationsInput,
    GetAllGratuityAccrualSummariesInput,
    GetAllSalaryRegistersInput,
    GratuityAccrualSummariesServiceProxy,
    PayrollRunType,
    SalaryRegistersServiceProxy,
} from '@shared/service-proxies/service-proxies';

/**
 * The Employee Workbench's six tiles, described as data.
 *
 * WHY A CATALOG. Every sub-link is the same thing -- a short list of one employee's records, a New button,
 * an edit button per row -- differing only in which service it reads, which columns it shows and which
 * form it opens. Describing those differences as data keeps one grid component and makes adding a
 * sub-link a matter of adding an entry here.
 *
 * THE FORMS ARE THE EMPLOYEE RECORD'S OWN. `modal` loads the very create/edit form the Employee Detail
 * screen uses, so every validation, date-chaining rule and permission check applies here unchanged. The
 * workbench never saves anything itself. Loaded with a dynamic import, like the Employee Detail sections,
 * so opening the workbench does not pull in fifteen forms nobody has clicked yet.
 */

/** How many rows each sub-link shows: the agreed "last 5". */
export const WORKBENCH_ROW_COUNT = 5;

/** A document expiring within this many days is highlighted as due soon. */
export const DOCUMENT_EXPIRY_WARNING_DAYS = 60;

export type WorkbenchCellType = 'text' | 'date' | 'time' | 'month' | 'number' | 'amount' | 'yesNo';

export interface WorkbenchColumn {
    labelKey: string;
    type?: WorkbenchCellType;

    /** Property of the row to show. Ignored when `value` is given. */
    field?: string;

    /** Computed cell. `l` localizes, `isArabic` picks between an English and an Arabic name. */
    value?: (row: any, context: WorkbenchCellContext) => unknown;
}

export interface WorkbenchCellContext {
    l: (key: string) => string;
    isArabic: boolean;
}

/** The shape every Employee Detail create/edit form already has. */
export interface WorkbenchForm {
    show(employeeId: number, id?: number): void;
    readonly modalSave: OutputEmitterRef<void>;
}

export type WorkbenchRowTone = 'danger' | 'warning' | null;

export interface WorkbenchSubView {
    id: string;
    labelKey: string;

    /** Needed to see this sub-link at all -- the same permission that guards the record's own screen. */
    permission: string;

    load: (injector: Injector, employeeId: number) => Observable<any[]>;
    columns: WorkbenchColumn[];

    /** The Employee Detail form for this record. Omitted for read-only sub-links. */
    form?: () => Promise<Type<WorkbenchForm>>;
    createPermission?: string;
    editPermission?: string;

    /** Label for the per-row edit button when "Edit" would undersell it -- e.g. recording a return. */
    editLabelKey?: string;

    /**
     * Ex-employees' records stay visible, but new ones are not offered: what is left to do for a leaver is
     * settling them, so only the settlement sub-links keep their New button.
     */
    allowCreateForExEmployee?: boolean;

    /** The Employee Detail section holding the full history, for the "open full history" link. */
    detailSectionId?: string;

    /** A per-row button that is neither edit nor create -- today only the payslip PDF. */
    rowAction?: 'payslipPdf';

    tone?: (row: any) => WorkbenchRowTone;
}

export interface WorkbenchTile {
    id: string;
    labelKey: string;

    /** Metronic flaticon class, the family the application menu uses. */
    icon: string;

    subViews: WorkbenchSubView[];
}

/** Newest first is each list service's own default order, so no sorting is sent. */
const lastRows = { maxResultCount: WORKBENCH_ROW_COUNT, skipCount: 0, sorting: undefined as string | undefined };

const byLanguage = (context: WorkbenchCellContext, en: string | undefined, ar: string | undefined) =>
    context.isArabic ? ar || en : en || ar;

const yearMonth = (year: number, month: number) => `${year}-${String(month).padStart(2, '0')}`;

export const WORKBENCH_TILES: WorkbenchTile[] = [
    {
        id: 'profile',
        labelKey: 'WorkbenchTile_Profile',
        icon: 'flaticon-user',
        subViews: [
            {
                id: 'org-assignments',
                labelKey: 'WorkbenchView_OrgAssignments',
                permission: 'Pages.PersonnelAdministration.EmployeeOrgAssignments',
                createPermission: 'Pages.PersonnelAdministration.EmployeeOrgAssignments.Create',
                editPermission: 'Pages.PersonnelAdministration.EmployeeOrgAssignments.Edit',
                detailSectionId: 'org-assignments',
                load: (injector, employeeId) =>
                    injector
                        .get(EmployeeOrgAssignmentsServiceProxy)
                        .getAll(new GetAllEmployeeOrgAssignmentsInput({ employeeId, ...lastRows }))
                        .pipe(map((r) => r.items)),
                columns: [
                    { labelKey: 'StartDate', field: 'startDate', type: 'date' },
                    { labelKey: 'EndDate', field: 'endDate', type: 'date' },
                    { labelKey: 'JobTitle', field: 'jobTitleName' },
                    { labelKey: 'OrganizationUnit', field: 'organizationUnitName' },
                    { labelKey: 'WorkLocation', field: 'workLocationName' },
                ],
                form: () =>
                    import('../employee-detail/org-assignments/create-or-edit-employee-org-assignment-modal.component').then(
                        (m) => m.CreateOrEditEmployeeOrgAssignmentModalComponent,
                    ),
            },
            {
                id: 'basic-pays',
                labelKey: 'WorkbenchView_BasicPayChanges',
                permission: 'Pages.PersonnelAdministration.EmployeeBasicPays',
                createPermission: 'Pages.PersonnelAdministration.EmployeeBasicPays.Create',
                editPermission: 'Pages.PersonnelAdministration.EmployeeBasicPays.Edit',
                detailSectionId: 'basic-pays',
                load: (injector, employeeId) =>
                    injector
                        .get(EmployeeBasicPaysServiceProxy)
                        .getAll(new GetAllEmployeeBasicPaysInput({ employeeId, ...lastRows }))
                        .pipe(map((r) => r.items)),
                columns: [
                    { labelKey: 'StartDate', field: 'startDate', type: 'date' },
                    { labelKey: 'EndDate', field: 'endDate', type: 'date' },
                    { labelKey: 'Amount', field: 'amount', type: 'amount' },
                ],
                form: () =>
                    import('../employee-detail/basic-pays/create-or-edit-employee-basic-pay-modal.component').then(
                        (m) => m.CreateOrEditEmployeeBasicPayModalComponent,
                    ),
            },
            {
                id: 'managers',
                labelKey: 'WorkbenchView_ManagerChanges',
                permission: 'Pages.PersonnelAdministration.EmployeeManagers',
                createPermission: 'Pages.PersonnelAdministration.EmployeeManagers.Create',
                editPermission: 'Pages.PersonnelAdministration.EmployeeManagers.Edit',
                detailSectionId: 'managers',
                load: (injector, employeeId) =>
                    injector
                        .get(EmployeeManagersServiceProxy)
                        .getAll(new GetAllEmployeeManagersInput({ employeeId, ...lastRows }))
                        .pipe(map((r) => r.items)),
                columns: [
                    { labelKey: 'StartDate', field: 'startDate', type: 'date' },
                    { labelKey: 'EndDate', field: 'endDate', type: 'date' },
                    { labelKey: 'Manager', value: (row) => [row.managerCode, row.managerFullNameEn].filter((x) => !!x).join(' - ') },
                ],
                form: () =>
                    import('../employee-detail/managers/create-or-edit-employee-manager-modal.component').then(
                        (m) => m.CreateOrEditEmployeeManagerModalComponent,
                    ),
            },
        ],
    },
    {
        id: 'leave',
        labelKey: 'WorkbenchTile_Leave',
        icon: 'flaticon-calendar-3',
        subViews: [
            {
                id: 'balances',
                labelKey: 'WorkbenchView_LeaveBalances',
                permission: 'Pages.PersonnelAdministration.EmployeeAbsenceQuotas',
                createPermission: 'Pages.PersonnelAdministration.EmployeeAbsenceQuotas.Create',
                editPermission: 'Pages.PersonnelAdministration.EmployeeAbsenceQuotas.Edit',
                detailSectionId: 'absence-quotas',
                load: (injector, employeeId) =>
                    injector
                        .get(EmployeeAbsenceQuotasServiceProxy)
                        .getAll(new GetAllEmployeeAbsenceQuotasInput({ employeeId, ...lastRows }))
                        .pipe(map((r) => r.items)),
                columns: [
                    { labelKey: 'AbsenceType', field: 'absenceTypeName' },
                    { labelKey: 'EntitlementFrom', field: 'entitlementFrom', type: 'date' },
                    { labelKey: 'EntitlementTo', field: 'entitlementTo', type: 'date' },
                    { labelKey: 'QuotaAccrued', field: 'quotaAccrued', type: 'number' },
                    { labelKey: 'QuotaUsed', field: 'quotaUsed', type: 'number' },
                    { labelKey: 'QuotaBalance', field: 'quotaBalance', type: 'number' },
                ],
                form: () =>
                    import('../employee-detail/absence-quotas/create-or-edit-employee-absence-quota-modal.component').then(
                        (m) => m.CreateOrEditEmployeeAbsenceQuotaModalComponent,
                    ),
            },
            {
                id: 'leaves',
                labelKey: 'WorkbenchView_LastLeaves',
                permission: 'Pages.PersonnelAdministration.EmployeeAbsences',
                createPermission: 'Pages.PersonnelAdministration.EmployeeAbsences.Create',
                editPermission: 'Pages.PersonnelAdministration.EmployeeAbsences.Edit',
                detailSectionId: 'absences',
                load: (injector, employeeId) =>
                    injector
                        .get(EmployeeAbsencesServiceProxy)
                        .getAll(new GetAllEmployeeAbsencesInput({ employeeId, ...lastRows }))
                        .pipe(map((r) => r.items)),
                columns: [
                    { labelKey: 'AbsenceType', field: 'absenceTypeName' },
                    { labelKey: 'StartDate', field: 'startDate', type: 'date' },
                    { labelKey: 'EndDate', field: 'endDate', type: 'date' },
                    { labelKey: 'TotalLeaveDays', field: 'totalLeaveDays', type: 'number' },
                    { labelKey: 'IsReturned', field: 'isReturned', type: 'yesNo' },
                ],
                form: () =>
                    import('../employee-detail/absences/create-or-edit-employee-absence-modal.component').then(
                        (m) => m.CreateOrEditEmployeeAbsenceModalComponent,
                    ),
            },
            {
                // Read from the workbench service, which applies the payroll engine's own not-returned rule.
                // The row opens the leave itself: the return is recorded on the leave, not as a new record.
                id: 'not-returned',
                labelKey: 'WorkbenchView_NotReturned',
                permission: 'Pages.PersonnelAdministration.EmployeeAbsences',
                editPermission: 'Pages.PersonnelAdministration.EmployeeAbsences.Edit',
                editLabelKey: 'RecordReturn',
                detailSectionId: 'absences',
                load: (injector, employeeId) =>
                    injector
                        .get(EmployeeWorkbenchServiceProxy)
                        .getNotReturnedLeaves(employeeId)
                        .pipe(map((r) => r.items)),
                columns: [
                    { labelKey: 'AbsenceType', value: (row, c) => byLanguage(c, row.absenceTypeNameEn, row.absenceTypeNameAr) },
                    { labelKey: 'StartDate', field: 'startDate', type: 'date' },
                    { labelKey: 'EndDate', field: 'endDate', type: 'date' },
                    { labelKey: 'DaysOverdue', field: 'daysOverdue', type: 'number' },
                ],
                form: () =>
                    import('../employee-detail/absences/create-or-edit-employee-absence-modal.component').then(
                        (m) => m.CreateOrEditEmployeeAbsenceModalComponent,
                    ),
                tone: () => 'danger',
            },
        ],
    },
    {
        // View only, as agreed: manual punches stay on the Time Events screen.
        id: 'attendance',
        labelKey: 'WorkbenchTile_Attendance',
        icon: 'flaticon-time',
        subViews: [
            {
                id: 'last-7-days',
                labelKey: 'WorkbenchView_Last7Days',
                permission: 'Pages.TimeManagement.TimeRegisters',
                load: (injector, employeeId) =>
                    injector
                        .get(EmployeeWorkbenchServiceProxy)
                        .getRecentAttendance(employeeId)
                        .pipe(map((r) => r.items)),
                columns: [
                    { labelKey: 'DayDate', field: 'dayDate', type: 'date' },
                    { labelKey: 'ActualIn', field: 'actualIn', type: 'time' },
                    { labelKey: 'ActualOut', field: 'actualOut', type: 'time' },
                    { labelKey: 'Status', value: (row, c) => attendanceStatus(row, c) },
                ],
                tone: (row) => (row.isMissingIn || row.isMissingOut || row.isAbsent ? 'warning' : null),
            },
            {
                id: 'missing-punches',
                labelKey: 'WorkbenchView_MissingPunches',
                permission: 'Pages.TimeManagement.TimeRegisters',
                load: (injector, employeeId) =>
                    injector
                        .get(EmployeeWorkbenchServiceProxy)
                        .getRecentAttendance(employeeId)
                        .pipe(map((r) => r.items.filter((d) => d.isMissingIn || d.isMissingOut))),
                columns: [
                    { labelKey: 'DayDate', field: 'dayDate', type: 'date' },
                    { labelKey: 'ActualIn', field: 'actualIn', type: 'time' },
                    { labelKey: 'ActualOut', field: 'actualOut', type: 'time' },
                    { labelKey: 'Status', value: (row, c) => c.l(row.isMissingIn ? 'MissingIn' : 'MissingOut') },
                ],
                tone: () => 'warning',
            },
        ],
    },
    {
        id: 'payroll',
        labelKey: 'WorkbenchTile_Payroll',
        icon: 'flaticon-coins',
        subViews: [
            {
                id: 'payslips',
                labelKey: 'WorkbenchView_Payslips',
                permission: 'Pages.PayrollManagement.SalaryRegisters',
                rowAction: 'payslipPdf',
                load: (injector, employeeId) =>
                    injector
                        .get(SalaryRegistersServiceProxy)
                        .getAll(new GetAllSalaryRegistersInput({ employeeId, ...lastRows }))
                        .pipe(map((r) => r.items)),
                columns: [
                    { labelKey: 'Month', field: 'inPeriod', type: 'month' },
                    {
                        labelKey: 'PayrollRunType',
                        value: (row, c) => c.l(row.payrollRunType === PayrollRunType.OffCycle ? 'OffCycle' : 'Regular'),
                    },
                    { labelKey: 'Locked', field: 'locked', type: 'yesNo' },
                ],
            },
            {
                id: 'earnings-deductions',
                labelKey: 'WorkbenchView_EarningsDeductions',
                permission: 'Pages.PersonnelAdministration.EmployeePayments',
                createPermission: 'Pages.PersonnelAdministration.EmployeePayments.Create',
                editPermission: 'Pages.PersonnelAdministration.EmployeePayments.Edit',
                allowCreateForExEmployee: true,
                detailSectionId: 'payments',
                load: (injector, employeeId) =>
                    injector
                        .get(EmployeePaymentsServiceProxy)
                        .getAll(new GetAllEmployeePaymentsInput({ employeeId, ...lastRows }))
                        .pipe(map((r) => r.items)),
                columns: [
                    { labelKey: 'PaymentDate', field: 'paymentDate', type: 'date' },
                    { labelKey: 'WageType', field: 'wageTypeNameEn' },
                    { labelKey: 'Amount', field: 'amount', type: 'amount' },
                    { labelKey: 'Comments', field: 'comments' },
                ],
                form: () =>
                    import('../employee-detail/payments/create-or-edit-employee-payment-modal.component').then(
                        (m) => m.CreateOrEditEmployeePaymentModalComponent,
                    ),
            },
            {
                id: 'loans',
                labelKey: 'WorkbenchView_Loans',
                permission: 'Pages.PersonnelAdministration.EmployeeLoans',
                createPermission: 'Pages.PersonnelAdministration.EmployeeLoans.Create',
                editPermission: 'Pages.PersonnelAdministration.EmployeeLoans.Edit',
                detailSectionId: 'loans',
                load: (injector, employeeId) =>
                    injector
                        .get(EmployeeLoansServiceProxy)
                        .getAll(new GetAllEmployeeLoansInput({ employeeId, ...lastRows }))
                        .pipe(map((r) => r.items)),
                columns: [
                    { labelKey: 'LoanType', field: 'loanTypeName' },
                    { labelKey: 'StartDate', field: 'startDate', type: 'date' },
                    { labelKey: 'ActualAmount', field: 'actualAmount', type: 'amount' },
                    { labelKey: 'BalanceDueAmount', field: 'balanceDueAmount', type: 'amount' },
                ],
                form: () =>
                    import('../employee-detail/loans/create-or-edit-employee-loan-modal.component').then(
                        (m) => m.CreateOrEditEmployeeLoanModalComponent,
                    ),
            },
        ],
    },
    {
        id: 'end-of-service',
        labelKey: 'WorkbenchTile_EndOfService',
        icon: 'flaticon-logout',
        subViews: [
            {
                id: 'indemnity',
                labelKey: 'WorkbenchView_Indemnity',
                permission: 'Pages.PayrollManagement.GratuityAccrualSummaries',
                load: (injector, employeeId) =>
                    injector
                        .get(GratuityAccrualSummariesServiceProxy)
                        .getAll(new GetAllGratuityAccrualSummariesInput({ employeeId, ...lastRows }))
                        .pipe(map((r) => r.items)),
                columns: [
                    { labelKey: 'Month', value: (row) => yearMonth(row.year, row.month) },
                    { labelKey: 'GratuityDaysAccrued', field: 'gratuityDaysAccrued', type: 'number' },
                    { labelKey: 'GratuityDaysBalance', field: 'gratuityDaysBalance', type: 'number' },
                    { labelKey: 'GratuityAmount', field: 'gratuityAmount', type: 'amount' },
                ],
            },
            {
                id: 'indemnity-paid',
                labelKey: 'WorkbenchView_IndemnityPaid',
                permission: 'Pages.PersonnelAdministration.EmployeeGratuities',
                createPermission: 'Pages.PersonnelAdministration.EmployeeGratuities.Create',
                editPermission: 'Pages.PersonnelAdministration.EmployeeGratuities.Edit',
                allowCreateForExEmployee: true,
                detailSectionId: 'gratuities',
                load: (injector, employeeId) =>
                    injector
                        .get(EmployeeGratuitiesServiceProxy)
                        .getAll(new GetAllEmployeeGratuitiesInput({ employeeId, ...lastRows }))
                        .pipe(map((r) => r.items)),
                columns: [
                    { labelKey: 'GratuityPaidDate', field: 'gratuityPaidDate', type: 'date' },
                    { labelKey: 'GratuityPaidDays', field: 'gratuityPaidDays', type: 'number' },
                    { labelKey: 'Comments', field: 'comments' },
                ],
                form: () =>
                    import('../employee-detail/gratuities/create-or-edit-employee-gratuity-modal.component').then(
                        (m) => m.CreateOrEditEmployeeGratuityModalComponent,
                    ),
            },
            {
                id: 'exit',
                labelKey: 'WorkbenchView_Exit',
                permission: 'Pages.PersonnelAdministration.EmployeeSeparations',
                createPermission: 'Pages.PersonnelAdministration.EmployeeSeparations.Create',
                editPermission: 'Pages.PersonnelAdministration.EmployeeSeparations.Edit',
                detailSectionId: 'separations',
                load: (injector, employeeId) =>
                    injector
                        .get(EmployeeSeparationsServiceProxy)
                        .getAll(new GetAllEmployeeSeparationsInput({ employeeId, ...lastRows }))
                        .pipe(map((r) => r.items)),
                columns: [
                    { labelKey: 'SubmissionDate', field: 'submissionDate', type: 'date' },
                    { labelKey: 'LastDayInService', field: 'lastDayInService', type: 'date' },
                    { labelKey: 'SeparationReason', field: 'separationReasonName' },
                    { labelKey: 'RevokeDate', field: 'revokeDate', type: 'date' },
                ],
                form: () =>
                    import('../employee-detail/separations/create-or-edit-employee-separation-modal.component').then(
                        (m) => m.CreateOrEditEmployeeSeparationModalComponent,
                    ),
            },
            {
                id: 'full-and-final',
                labelKey: 'WorkbenchView_FullAndFinal',
                permission: 'Pages.PersonnelAdministration.EmployeeFnFPayments',
                createPermission: 'Pages.PersonnelAdministration.EmployeeFnFPayments.Create',
                editPermission: 'Pages.PersonnelAdministration.EmployeeFnFPayments.Edit',
                allowCreateForExEmployee: true,
                detailSectionId: 'fnf-payments',
                load: (injector, employeeId) =>
                    injector
                        .get(EmployeeFnFPaymentsServiceProxy)
                        .getAll(new GetAllEmployeeFnFPaymentsInput({ employeeId, ...lastRows }))
                        .pipe(map((r) => r.items)),
                columns: [
                    { labelKey: 'CompensationDate', field: 'compensationDate', type: 'date' },
                    { labelKey: 'LeaveCompensateDays', field: 'leaveCompensateDays', type: 'number' },
                    { labelKey: 'GratuityCompensateDays', field: 'gratuityCompensateDays', type: 'number' },
                    { labelKey: 'IndemnityCompensateAmount', field: 'indemnityCompensateAmount', type: 'amount' },
                ],
                form: () =>
                    import('../employee-detail/fnf-payments/create-or-edit-employee-fnf-payment-modal.component').then(
                        (m) => m.CreateOrEditEmployeeFnFPaymentModalComponent,
                    ),
            },
        ],
    },
    {
        id: 'documents',
        labelKey: 'WorkbenchTile_Documents',
        icon: 'flaticon-doc',
        subViews: [
            {
                // Soonest expiry first, leaving out what has already expired (that is the next sub-link).
                // The list service has no expiry filter, so a wider page is read and narrowed here.
                id: 'expiring',
                labelKey: 'WorkbenchView_ExpiringDocuments',
                permission: 'Pages.PersonnelAdministration.EmployeeDocuments',
                createPermission: 'Pages.PersonnelAdministration.EmployeeDocuments.Create',
                editPermission: 'Pages.PersonnelAdministration.EmployeeDocuments.Edit',
                detailSectionId: 'documents',
                load: (injector, employeeId) =>
                    loadDocuments(injector, employeeId, 'ExpiryDate ASC').pipe(
                        map((rows) => rows.filter((d) => d.daysLeft >= 0).slice(0, WORKBENCH_ROW_COUNT)),
                    ),
                columns: documentColumns(),
                form: documentForm,
                tone: (row) => (row.daysLeft <= DOCUMENT_EXPIRY_WARNING_DAYS ? 'warning' : null),
            },
            {
                id: 'expired',
                labelKey: 'WorkbenchView_ExpiredDocuments',
                permission: 'Pages.PersonnelAdministration.EmployeeDocuments',
                editPermission: 'Pages.PersonnelAdministration.EmployeeDocuments.Edit',
                detailSectionId: 'documents',
                load: (injector, employeeId) =>
                    loadDocuments(injector, employeeId, 'ExpiryDate DESC').pipe(
                        map((rows) => rows.filter((d) => d.daysLeft < 0).slice(0, WORKBENCH_ROW_COUNT)),
                    ),
                columns: documentColumns(),
                form: documentForm,
                tone: () => 'danger',
            },
            {
                id: 'all-documents',
                labelKey: 'WorkbenchView_AllDocuments',
                permission: 'Pages.PersonnelAdministration.EmployeeDocuments',
                createPermission: 'Pages.PersonnelAdministration.EmployeeDocuments.Create',
                editPermission: 'Pages.PersonnelAdministration.EmployeeDocuments.Edit',
                detailSectionId: 'documents',
                load: (injector, employeeId) =>
                    injector
                        .get(EmployeeDocumentsServiceProxy)
                        .getAll(new GetAllEmployeeDocumentsInput({ employeeId, ...lastRows }))
                        .pipe(map((r) => r.items)),
                columns: documentColumns(),
                form: documentForm,
            },
        ],
    },
];

/** Enough documents to find the five nearest an expiry; nobody carries fifty documents. */
const DOCUMENT_SCAN_COUNT = 50;

function loadDocuments(injector: Injector, employeeId: number, sorting: string): Observable<any[]> {
    return injector
        .get(EmployeeDocumentsServiceProxy)
        .getAll(new GetAllEmployeeDocumentsInput({ employeeId, sorting, maxResultCount: DOCUMENT_SCAN_COUNT, skipCount: 0 }))
        .pipe(map((r) => r.items));
}

function documentColumns(): WorkbenchColumn[] {
    return [
        { labelKey: 'DocumentType', field: 'documentTypeName' },
        { labelKey: 'DocumentNum', field: 'documentNum' },
        { labelKey: 'ExpiryDate', field: 'expiryDate', type: 'date' },
        { labelKey: 'DaysLeft', field: 'daysLeft', type: 'number' },
    ];
}

function documentForm(): Promise<Type<WorkbenchForm>> {
    return import('../employee-detail/documents/create-or-edit-employee-document-modal.component').then(
        (m) => m.CreateOrEditEmployeeDocumentModalComponent,
    );
}

function attendanceStatus(row: any, c: WorkbenchCellContext): string {
    if (row.isMissingIn) {
        return c.l('MissingIn');
    }
    if (row.isMissingOut) {
        return c.l('MissingOut');
    }
    if (row.isOnLeave) {
        return c.l('OnLeave');
    }
    if (row.isAbsent) {
        return c.l('Absent');
    }
    return '';
}

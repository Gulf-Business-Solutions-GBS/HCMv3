import { Type } from '@angular/core';

/**
 * The 65 sections of an employee record, grouped for the left rail.
 *
 * WHY A CATALOG AND NOT 65 TABS IN THE TEMPLATE. The previous screen listed every section as a
 * `<tab>`, which meant the template both DESCRIBED the navigation and WAS the navigation: the labels
 * could not be grouped, filtered or counted without editing markup, and every one of the 65 child
 * grids was instantiated on load whether or not anyone looked at it. Describing the sections as data
 * lets the rail group and search them, and lets exactly one section be built at a time.
 *
 * `load` is a dynamic import rather than a direct class reference on purpose: an employee record has
 * 65 grids and a person opens two or three. Referencing the classes statically would pull all 65 into
 * the route's bundle just to show "Dates".
 */
export interface EmployeeSection {
    /** Stable id. Used in the URL fragment, so renaming one changes people's bookmarks. */
    id: string;

    /**
     * Localization key. Deliberately namespaced `EmpSection_*` rather than reusing generic keys like
     * `Actions` or `Dates`: these labels read inside a rail that already says whose record it is, so
     * they must be the SHORT form ("Managers", not "Employee Managers"). A shared generic key would be
     * retranslated for some other screen one day and silently change this one. The namespace also lets
     * each language pick its own short form -- dropping the word "Employee" is a trim in English and
     * not a trim at all in Arabic.
     */
    labelKey: string;

    load: () => Promise<Type<unknown>>;
}

export interface EmployeeSectionCategory {
    id: string;
    labelKey: string;

    /**
     * Metronic flaticon class -- the same family the main application rail uses, so the collapsed
     * employee rail reads as the same kind of control rather than as a different widget that happens
     * to sit next to it.
     */
    icon: string;

    sections: EmployeeSection[];
}

export const EMPLOYEE_SECTION_CATEGORIES: EmployeeSectionCategory[] = [
    {
        id: 'personal',
        labelKey: 'EmpCategory_Personal',
        icon: 'flaticon-user',
        sections: [
            {
                // First in the catalog, and the default section: opening a record should answer "who is
                // this" before it answers anything else. Management's complaint was that this was the
                // one thing the record did not show -- a name or nationality meant leaving for the
                // edit form and losing your place.
                id: 'personal-info',
                labelKey: 'EmpSection_PersonalInformation',
                load: () =>
                    import('./personal-info/employee-personal-info.component').then(
                        (m) => m.EmployeePersonalInfoComponent
                    ),
            },
            {
                id: 'dates',
                labelKey: 'EmpSection_Dates',
                load: () => import('./dates/employee-dates.component').then((m) => m.EmployeeDatesComponent),
            },
            {
                id: 'relationships',
                labelKey: 'EmpSection_Relationships',
                load: () =>
                    import('./relationships/employee-relationships.component').then(
                        (m) => m.EmployeeRelationshipsComponent
                    ),
            },
            {
                id: 'dependants',
                labelKey: 'EmpSection_Dependants',
                load: () =>
                    import('./dependants/employee-dependants.component').then((m) => m.EmployeeDependantsComponent),
            },
            {
                id: 'education',
                labelKey: 'EmpSection_Education',
                load: () =>
                    import('./education/employee-educations.component').then((m) => m.EmployeeEducationsComponent),
            },
            {
                id: 'certifications',
                labelKey: 'EmpSection_Certifications',
                load: () =>
                    import('./certifications/employee-certifications.component').then(
                        (m) => m.EmployeeCertificationsComponent
                    ),
            },
            {
                id: 'work-experience',
                labelKey: 'EmpSection_WorkExperience',
                load: () =>
                    import('./work-experience/employee-work-experiences.component').then(
                        (m) => m.EmployeeWorkExperiencesComponent
                    ),
            },
            {
                id: 'documents',
                labelKey: 'EmpSection_Documents',
                load: () =>
                    import('./documents/employee-documents.component').then((m) => m.EmployeeDocumentsComponent),
            },
            {
                id: 'visa-info',
                labelKey: 'EmpSection_VisaInfo',
                load: () => import('./visa-info/employee-visa-info.component').then((m) => m.EmployeeVisaInfoComponent),
            },
            {
                id: 'sponsors',
                labelKey: 'EmpSection_Sponsors',
                load: () => import('./sponsors/employee-sponsors.component').then((m) => m.EmployeeSponsorsComponent),
            },
            {
                id: 'signature',
                labelKey: 'EmpSection_Signature',
                load: () =>
                    import('./signature/employee-signature.component').then((m) => m.EmployeeSignatureComponent),
            },
        ],
    },
    {
        id: 'job',
        labelKey: 'EmpCategory_Job',
        icon: 'flaticon-suitcase',
        sections: [
            {
                id: 'org-assignments',
                labelKey: 'EmpSection_OrgAssignments',
                load: () =>
                    import('./org-assignments/employee-org-assignments.component').then(
                        (m) => m.EmployeeOrgAssignmentsComponent
                    ),
            },
            {
                id: 'contracts',
                labelKey: 'EmpSection_Contracts',
                load: () => import('./contracts/employee-contracts.component').then((m) => m.EmployeeContractsComponent),
            },
            {
                id: 'actions',
                labelKey: 'EmpSection_Actions',
                load: () => import('./actions/employee-actions.component').then((m) => m.EmployeeActionsComponent),
            },
            {
                id: 'managers',
                labelKey: 'EmpSection_Managers',
                load: () => import('./managers/employee-managers.component').then((m) => m.EmployeeManagersComponent),
            },
            {
                id: 'dotted-line-managers',
                labelKey: 'EmpSection_DottedLineManagers',
                load: () =>
                    import('./dotted-line-managers/employee-dotted-line-managers.component').then(
                        (m) => m.EmployeeDottedLineManagersComponent
                    ),
            },
            {
                id: 'delegations',
                labelKey: 'EmpSection_Delegations',
                load: () =>
                    import('./delegations/employee-delegations.component').then((m) => m.EmployeeDelegationsComponent),
            },
            {
                // Next to Delegations, which is the other answer to "who acts on approvals for this
                // person". Holding a workflow role is a fact about somebody, so it is asked here rather
                // than on a screen of its own.
                id: 'workflow-roles',
                labelKey: 'EmpSection_WorkflowRoles',
                load: () =>
                    import('./workflow-roles/employee-workflow-roles.component').then(
                        (m) => m.EmployeeWorkflowRolesComponent
                    ),
            },
            {
                id: 'extd-work-locations',
                labelKey: 'EmpSection_ExtendedWorkLocations',
                load: () =>
                    import('./extd-work-locations/employee-extd-work-locations.component').then(
                        (m) => m.EmployeeExtdWorkLocationsComponent
                    ),
            },
            {
                id: 'separations',
                labelKey: 'EmpSection_Separations',
                load: () =>
                    import('./separations/employee-separations.component').then((m) => m.EmployeeSeparationsComponent),
            },
            {
                id: 'rejoins',
                labelKey: 'EmpSection_Rejoins',
                load: () => import('./rejoins/employee-rejoins.component').then((m) => m.EmployeeRejoinsComponent),
            },
        ],
    },
    {
        id: 'time',
        labelKey: 'EmpCategory_Time',
        icon: 'flaticon-time-3',
        sections: [
            {
                id: 'duty-periods',
                labelKey: 'EmpSection_DutyPeriods',
                load: () =>
                    import('./duty-periods/employee-duty-periods.component').then(
                        (m) => m.EmployeeDutyPeriodsComponent
                    ),
            },
            {
                id: 'duty-schedules',
                labelKey: 'EmpSection_DutySchedules',
                load: () =>
                    import('./duty-schedules/employee-duty-schedules.component').then(
                        (m) => m.EmployeeDutySchedulesComponent
                    ),
            },
            {
                id: 'time-policies',
                labelKey: 'EmpSection_TimePolicies',
                load: () =>
                    import('./time-policies/employee-time-policies.component').then(
                        (m) => m.EmployeeTimePoliciesComponent
                    ),
            },
            {
                id: 'substitute-duties',
                labelKey: 'EmpSection_SubstituteDuties',
                load: () =>
                    import('./substitute-duties/employee-substitute-duties.component').then(
                        (m) => m.EmployeeSubstituteDutiesComponent
                    ),
            },
            {
                id: 'timesheets',
                labelKey: 'EmpSection_Timesheets',
                load: () =>
                    import('./timesheets/employee-timesheets.component').then((m) => m.EmployeeTimesheetsComponent),
            },
            {
                id: 'attendances',
                labelKey: 'EmpSection_Attendances',
                load: () =>
                    import('./attendances/employee-attendances.component').then((m) => m.EmployeeAttendancesComponent),
            },
            {
                id: 'attendance-quotas',
                labelKey: 'EmpSection_AttendanceQuotas',
                load: () =>
                    import('./attendance-quotas/employee-attendance-quotas.component').then(
                        (m) => m.EmployeeAttendanceQuotasComponent
                    ),
            },
            {
                id: 'overtimes',
                labelKey: 'EmpSection_Overtimes',
                load: () => import('./overtimes/employee-overtimes.component').then((m) => m.EmployeeOvertimesComponent),
            },
            {
                id: 'absences',
                labelKey: 'EmpSection_Absences',
                load: () => import('./absences/employee-absences.component').then((m) => m.EmployeeAbsencesComponent),
            },
            {
                id: 'absence-quotas',
                labelKey: 'EmpSection_AbsenceQuotas',
                load: () =>
                    import('./absence-quotas/employee-absence-quotas.component').then(
                        (m) => m.EmployeeAbsenceQuotasComponent
                    ),
            },
            {
                id: 'abs-adjustments',
                labelKey: 'EmpSection_AbsenceAdjustments',
                load: () =>
                    import('./abs-adjustments/employee-abs-adjustments.component').then(
                        (m) => m.EmployeeAbsAdjustmentsComponent
                    ),
            },
            {
                id: 'abs-quota-forfeits',
                labelKey: 'EmpSection_QuotaForfeits',
                load: () =>
                    import('./abs-quota-forfeits/employee-abs-quota-forfeits.component').then(
                        (m) => m.EmployeeAbsQuotaForfeitsComponent
                    ),
            },
        ],
    },
    {
        id: 'pay',
        labelKey: 'EmpCategory_Pay',
        icon: 'flaticon-coins',
        sections: [
            {
                id: 'basic-pays',
                labelKey: 'EmpSection_BasicPays',
                load: () =>
                    import('./basic-pays/employee-basic-pays.component').then((m) => m.EmployeeBasicPaysComponent),
            },
            {
                id: 'remunerations',
                labelKey: 'EmpSection_Remunerations',
                load: () =>
                    import('./remunerations/employee-remunerations.component').then(
                        (m) => m.EmployeeRemunerationsComponent
                    ),
            },
            {
                id: 'recurring-payments',
                labelKey: 'EmpSection_RecurringPayments',
                load: () =>
                    import('./recurring-payments/employee-recurring-payments.component').then(
                        (m) => m.EmployeeRecurringPaymentsComponent
                    ),
            },
            {
                id: 'payments',
                labelKey: 'EmpSection_Payments',
                load: () => import('./payments/employee-payments.component').then((m) => m.EmployeePaymentsComponent),
            },
            {
                id: 'non-payroll-trans',
                labelKey: 'EmpSection_NonPayrollTransactions',
                load: () =>
                    import('./non-payroll-trans/employee-non-payroll-trans.component').then(
                        (m) => m.EmployeeNonPayrollTransComponent
                    ),
            },
            {
                id: 'payroll-status',
                labelKey: 'EmpSection_PayrollStatus',
                load: () =>
                    import('./payroll-status/employee-payroll-status.component').then(
                        (m) => m.EmployeePayrollStatusComponent
                    ),
            },
            {
                id: 'on-holds',
                labelKey: 'EmpSection_OnHolds',
                load: () => import('./on-holds/employee-on-holds.component').then((m) => m.EmployeeOnHoldsComponent),
            },
            {
                id: 'wage-acc-groups',
                labelKey: 'EmpSection_WageAccountGroups',
                load: () =>
                    import('./wage-acc-groups/employee-wage-acc-groups.component').then(
                        (m) => m.EmployeeWageAccGroupsComponent
                    ),
            },
            {
                id: 'wage-accounts',
                labelKey: 'EmpSection_WageAccounts',
                load: () =>
                    import('./wage-accounts/employee-wage-accounts.component').then(
                        (m) => m.EmployeeWageAccountsComponent
                    ),
            },
            {
                id: 'banks',
                labelKey: 'EmpSection_Banks',
                load: () => import('./banks/employee-banks.component').then((m) => m.EmployeeBanksComponent),
            },
            {
                id: 'cost-distributions',
                labelKey: 'EmpSection_CostDistributions',
                load: () =>
                    import('./cost-distributions/employee-cost-distributions.component').then(
                        (m) => m.EmployeeCostDistributionsComponent
                    ),
            },
            {
                id: 'account-assignments',
                labelKey: 'EmpSection_AccountAssignments',
                load: () =>
                    import('./account-assignments/employee-account-assignments.component').then(
                        (m) => m.EmployeeAccountAssignmentsComponent
                    ),
            },
        ],
    },
    {
        id: 'benefits',
        labelKey: 'EmpCategory_Benefits',
        icon: 'flaticon-medical',
        sections: [
            {
                id: 'gratuities',
                labelKey: 'EmpSection_Gratuities',
                load: () =>
                    import('./gratuities/employee-gratuities.component').then((m) => m.EmployeeGratuitiesComponent),
            },
            {
                id: 'gratuity-quotas',
                labelKey: 'EmpSection_GratuityQuotas',
                load: () =>
                    import('./gratuity-quotas/employee-gratuity-quotas.component').then(
                        (m) => m.EmployeeGratuityQuotasComponent
                    ),
            },
            {
                id: 'loans',
                labelKey: 'EmpSection_Loans',
                load: () => import('./loans/employee-loans.component').then((m) => m.EmployeeLoansComponent),
            },
            {
                id: 'funds',
                labelKey: 'EmpSection_Funds',
                load: () => import('./funds/employee-funds.component').then((m) => m.EmployeeFundsComponent),
            },
            {
                id: 'insurances',
                labelKey: 'EmpSection_Insurances',
                load: () =>
                    import('./insurances/employee-insurances.component').then((m) => m.EmployeeInsurancesComponent),
            },
            {
                id: 'social-securities',
                labelKey: 'EmpSection_SocialSecurities',
                load: () =>
                    import('./social-securities/employee-social-securities.component').then(
                        (m) => m.EmployeeSocialSecuritiesComponent
                    ),
            },
            {
                id: 'social-security-claims',
                labelKey: 'EmpSection_SocialSecurityClaims',
                load: () =>
                    import('./social-security-claims/employee-social-security-claims.component').then(
                        (m) => m.EmployeeSocialSecurityClaimsComponent
                    ),
            },
            {
                id: 'health-reimburses',
                labelKey: 'EmpSection_HealthReimbursements',
                load: () =>
                    import('./health-reimburses/employee-health-reimburses.component').then(
                        (m) => m.EmployeeHealthReimbursesComponent
                    ),
            },
            {
                id: 'benefit-exceptions',
                labelKey: 'EmpSection_BenefitExceptions',
                load: () =>
                    import('./benefit-exceptions/employee-benefit-exceptions.component').then(
                        (m) => m.EmployeeBenefitExceptionsComponent
                    ),
            },
            {
                id: 'tickets',
                labelKey: 'EmpSection_Tickets',
                load: () => import('./tickets/employee-tickets.component').then((m) => m.EmployeeTicketsComponent),
            },
            {
                id: 'ticket-quotas',
                labelKey: 'EmpSection_TicketQuotas',
                load: () =>
                    import('./ticket-quotas/employee-ticket-quotas.component').then(
                        (m) => m.EmployeeTicketQuotasComponent
                    ),
            },
            {
                id: 'ticket-routes',
                labelKey: 'EmpSection_TicketRoutes',
                load: () =>
                    import('./ticket-routes/employee-ticket-routes.component').then(
                        (m) => m.EmployeeTicketRoutesComponent
                    ),
            },
            {
                id: 'tkt-adjustments',
                labelKey: 'EmpSection_TicketAdjustments',
                load: () =>
                    import('./tkt-adjustments/employee-tkt-adjustments.component').then(
                        (m) => m.EmployeeTktAdjustmentsComponent
                    ),
            },
            {
                id: 'fnf-payments',
                labelKey: 'EmpSection_FinalSettlement',
                load: () =>
                    import('./fnf-payments/employee-fnf-payments.component').then((m) => m.EmployeeFnFPaymentsComponent),
            },
        ],
    },
    {
        id: 'talent',
        labelKey: 'EmpCategory_Talent',
        icon: 'flaticon-medal',
        sections: [
            {
                id: 'performance-histories',
                labelKey: 'EmpSection_PerformanceHistory',
                load: () =>
                    import('./performance-histories/employee-performance-histories.component').then(
                        (m) => m.EmployeePerformanceHistoriesComponent
                    ),
            },
            {
                id: 'potential-assessments',
                labelKey: 'EmpSection_PotentialAssessments',
                load: () =>
                    import('./potential-assessments/employee-potential-assessments.component').then(
                        (m) => m.EmployeePotentialAssessmentsComponent
                    ),
            },
            {
                id: 'talent-matrix',
                labelKey: 'EmpSection_TalentMatrix',
                load: () =>
                    import('./talent-matrix/employee-talent-matrix.component').then(
                        (m) => m.EmployeeTalentMatrixComponent
                    ),
            },
            {
                id: 'talent-flags',
                labelKey: 'EmpSection_TalentFlags',
                load: () =>
                    import('./talent-flags/employee-talent-flags.component').then((m) => m.EmployeeTalentFlagsComponent),
            },
            {
                id: 'rewards',
                labelKey: 'EmpSection_Rewards',
                load: () => import('./rewards/employee-rewards.component').then((m) => m.EmployeeRewardsComponent),
            },
        ],
    },
    {
        id: 'conduct',
        labelKey: 'EmpCategory_Conduct',
        icon: 'flaticon-security',
        sections: [
            {
                id: 'disciplinaries',
                labelKey: 'EmpSection_Disciplinaries',
                load: () =>
                    import('./disciplinaries/employee-disciplinaries.component').then(
                        (m) => m.EmployeeDisciplinariesComponent
                    ),
            },
            {
                id: 'grievances',
                labelKey: 'EmpSection_Grievances',
                load: () =>
                    import('./grievances/employee-grievances.component').then((m) => m.EmployeeGrievancesComponent),
            },
            {
                id: 'tasks',
                labelKey: 'EmpSection_Tasks',
                load: () => import('./tasks/employee-tasks.component').then((m) => m.EmployeeTasksComponent),
            },
        ],
    },
    {
        id: 'assets',
        labelKey: 'EmpCategory_Assets',
        icon: 'flaticon-laptop',
        sections: [
            {
                id: 'assets',
                labelKey: 'EmpSection_Assets',
                load: () => import('./assets/employee-assets.component').then((m) => m.EmployeeAssetsComponent),
            },
            {
                id: 'asset-invoices',
                labelKey: 'EmpSection_AssetInvoices',
                load: () =>
                    import('./asset-invoices/employee-asset-invoices.component').then(
                        (m) => m.EmployeeAssetInvoicesComponent
                    ),
            },
        ],
    },
];

/** Flat view, for lookup by id and for counting. */
export const EMPLOYEE_SECTIONS: EmployeeSection[] = EMPLOYEE_SECTION_CATEGORIES.flatMap((c) => c.sections);

export const DEFAULT_EMPLOYEE_SECTION_ID = 'personal-info';

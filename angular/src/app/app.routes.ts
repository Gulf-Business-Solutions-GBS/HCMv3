import { Routes, Route } from '@angular/router';
import { Type } from '@angular/core';
import { AppRouteGuard } from './shared/common/auth/auth-route-guard';
import { ISimpleLookupServiceProxy } from './admin/general-master-info/shared/simple-lookup.types';
import {
    AssetTypesServiceProxy,
    CountriesServiceProxy,
    BankFilesServiceProxy,
    BudgetingGroupsServiceProxy,
    ClearanceRequestTypesServiceProxy,
    BenefitTypesServiceProxy,
    ContractTypesServiceProxy,
    DateTypesServiceProxy,
    DocumentPlacesServiceProxy,
    EducationsServiceProxy,
    EducationBranchesServiceProxy,
    GovernoratesServiceProxy,
    GrievanceTypesServiceProxy,
    HRServiceTypesServiceProxy,
    ITServiceTypesServiceProxy,
    LanguageInfosServiceProxy,
    MaritalStatusesServiceProxy,
    NationalityGroupsServiceProxy,
    NewsTypesServiceProxy,
    PassportTypesServiceProxy,
    PostingGroupsServiceProxy,
    QualificationsServiceProxy,
    RecruitmentTypesServiceProxy,
    RelationshipTypesServiceProxy,
    ReligionsServiceProxy,
    StatesServiceProxy,
    TaskTypesServiceProxy,
    TitlesServiceProxy,
    TrainingTypesServiceProxy,
    UniversitiesServiceProxy,
    VisaTypesServiceProxy,
    WorkVisaTypesServiceProxy,
    CostCenterGroupsServiceProxy,
    CostElementTypesServiceProxy,
    JobTitlesServiceProxy,
    TimeTypesServiceProxy,
    TransTypesServiceProxy,
    TimeTransferTypesServiceProxy,
    AccountTypesServiceProxy,
    PayrollReasonTypesServiceProxy,
    TicketClassesServiceProxy,
    TicketRoutesServiceProxy,
    AbsenceViolationRulesServiceProxy,
    PayrollPeriodsServiceProxy,
    WageAccGroupsServiceProxy,
    BusinessTypesServiceProxy,
    GovtJobCategoriesServiceProxy,
    ProjectTypesServiceProxy,
    ExpenseTypesServiceProxy,
    HotelsServiceProxy,
    TravelAirlinesServiceProxy,
    TravelCancelReasonsServiceProxy,
    TravelDestinationsServiceProxy,
    InsurancePlansServiceProxy,
    InsuranceProvidersServiceProxy,
    JobFamiliesServiceProxy,
    JobFunctionsServiceProxy,
    JobResponsibilitiesServiceProxy,
    EducationCertificatesServiceProxy,
    EducationDegreesServiceProxy,
    InterviewQuestionsServiceProxy,
    SkillsServiceProxy,
    GatePassTypesServiceProxy,
    GatePassMovementsServiceProxy,
    GatePassStatusesServiceProxy,
    PersonTypesServiceProxy,
    VehicleTypesServiceProxy,
    VehicleBrandsServiceProxy,
    VehicleModelsServiceProxy,
    ImpactOfLossesServiceProxy,
    NineBoxRatingsServiceProxy,
    RankingsServiceProxy,
    ReadinessesServiceProxy,
    ReasonForLeavingsServiceProxy,
    RiskOfLossesServiceProxy,
    SuccessionRatingsServiceProxy,
    TalentPoolsServiceProxy,
    ActionStatusesServiceProxy,
    FirstAidMedicinesServiceProxy,
    IncidentClaimStatusesServiceProxy,
    IncidentNaturesServiceProxy,
    IncidentSeveritiesServiceProxy,
    IncidentStatusesServiceProxy,
    IncidentTypesServiceProxy,
    InvestigationRootCausesServiceProxy,
    RiskActionsServiceProxy,
    RiskImpactsServiceProxy,
    RiskPossibilitiesServiceProxy,
    RiskStatusesServiceProxy,
    RiskTypesServiceProxy,
    SafetyAreaAssetsServiceProxy,
} from '@shared/service-proxies/service-proxies';

/** Builds a route entry for the shared SimpleLookupComponent (Code/NameEn/NameAr master data). */
function simpleLookupRoute(
    path: string,
    permissionSuffix: string,
    titleKey: string,
    serviceProxyType: Type<ISimpleLookupServiceProxy>,
    modulePrefix: string = 'GeneralMasterInfo'
): Route {
    const base = `Pages.${modulePrefix}.${permissionSuffix}`;
    return {
        path,
        loadComponent: () =>
            import('./admin/general-master-info/shared/simple-lookup.component').then(
                (m) => m.SimpleLookupComponent
            ),
        data: {
            permission: base,
            createPermission: `${base}.Create`,
            editPermission: `${base}.Edit`,
            deletePermission: `${base}.Delete`,
            titleKey,
            serviceProxyType,
        },
    };
}

export const appRoutes: Routes = [
    {
        path: 'app',
        loadComponent: () => import('./app.component').then((m) => m.AppComponent),
        canActivate: [AppRouteGuard],
        canActivateChild: [AppRouteGuard],
        children: [
            {
                path: '',
                children: [
                    {
                        path: 'notifications',
                        loadComponent: () =>
                            import('./shared/layout/notifications/notifications.component').then(
                                (m) => m.NotificationsComponent
                            ),
                    },
                    { path: '', redirectTo: '/app/main/dashboard', pathMatch: 'full' },
                ],
            },
            {
                path: 'main',
                data: { preload: true },
                children: [
                    {
                        path: 'dashboard',
                        loadComponent: () =>
                            import('./main/dashboard/dashboard.component').then((m) => m.DashboardComponent),
                        data: { permission: 'Pages.Tenant.Dashboard' },
                    },
                    { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
                ],
            },
            {
                path: 'admin',
                data: { preload: true },
                canLoad: [AppRouteGuard],
                children: [
                    {
                        path: 'users',
                        loadComponent: () => import('./admin/users/users.component').then((m) => m.UsersComponent),
                        data: { permission: 'Pages.Administration.Users' },
                    },
                    {
                        path: 'roles',
                        loadComponent: () => import('./admin/roles/roles.component').then((m) => m.RolesComponent),
                        data: { permission: 'Pages.Administration.Roles' },
                    },
                    {
                        path: 'access-units',
                        loadComponent: () =>
                            import('./admin/general-master-info/access-units/access-units.component').then(
                                (m) => m.AccessUnitsComponent
                            ),
                        data: { permission: 'Pages.GeneralMasterInfo.AccessUnits' },
                    },
                    {
                        path: 'cities',
                        loadComponent: () =>
                            import('./admin/general-master-info/cities/cities.component').then(
                                (m) => m.CitiesComponent
                            ),
                        data: { permission: 'Pages.GeneralMasterInfo.Cities' },
                    },
                    {
                        path: 'institutions',
                        loadComponent: () =>
                            import('./admin/general-master-info/institutions/institutions.component').then(
                                (m) => m.InstitutionsComponent
                            ),
                        data: { permission: 'Pages.GeneralMasterInfo.Institutions' },
                    },
                    {
                        path: 'nationalities',
                        loadComponent: () =>
                            import('./admin/general-master-info/nationalities/nationalities.component').then(
                                (m) => m.NationalitiesComponent
                            ),
                        data: { permission: 'Pages.GeneralMasterInfo.Nationalities' },
                    },
                    {
                        path: 'dependant-types',
                        loadComponent: () =>
                            import('./admin/general-master-info/dependant-types/dependant-types.component').then(
                                (m) => m.DependantTypesComponent
                            ),
                        data: { permission: 'Pages.GeneralMasterInfo.DependantTypes' },
                    },
                    {
                        path: 'currencies',
                        loadComponent: () =>
                            import('./admin/general-master-info/currencies/currencies.component').then(
                                (m) => m.CurrenciesComponent
                            ),
                        data: { permission: 'Pages.GeneralMasterInfo.Currencies' },
                    },
                    {
                        path: 'separation-reasons',
                        loadComponent: () =>
                            import('./admin/general-master-info/separation-reasons/separation-reasons.component').then(
                                (m) => m.SeparationReasonsComponent
                            ),
                        data: { permission: 'Pages.GeneralMasterInfo.SeparationReasons' },
                    },
                    {
                        path: 'disciplinary-deduction-types',
                        loadComponent: () =>
                            import(
                                './admin/general-master-info/disciplinary-deduction-types/disciplinary-deduction-types.component'
                            ).then((m) => m.DisciplinaryDeductionTypesComponent),
                        data: { permission: 'Pages.GeneralMasterInfo.DisciplinaryDeductionTypes' },
                    },
                    {
                        path: 'personal-actions',
                        loadComponent: () =>
                            import('./admin/general-master-info/personal-actions/personal-actions.component').then(
                                (m) => m.PersonalActionsComponent
                            ),
                        data: { permission: 'Pages.GeneralMasterInfo.PersonalActions' },
                    },
                    {
                        path: 'personal-action-reasons',
                        loadComponent: () =>
                            import(
                                './admin/general-master-info/personal-action-reasons/personal-action-reasons.component'
                            ).then((m) => m.PersonalActionReasonsComponent),
                        data: { permission: 'Pages.GeneralMasterInfo.PersonalActionReasons' },
                    },
                    {
                        path: 'banks',
                        loadComponent: () =>
                            import('./admin/general-master-info/banks/banks.component').then(
                                (m) => m.BanksComponent
                            ),
                        data: { permission: 'Pages.GeneralMasterInfo.Banks' },
                    },
                    {
                        path: 'payee-bank-details',
                        loadComponent: () =>
                            import('./admin/general-master-info/payee-bank-details/payee-bank-details.component').then(
                                (m) => m.PayeeBankDetailsComponent
                            ),
                        data: { permission: 'Pages.GeneralMasterInfo.PayeeBankDetails' },
                    },
                    {
                        path: 'budgeting-group-rate-details',
                        loadComponent: () =>
                            import(
                                './admin/general-master-info/budgeting-group-rate-details/budgeting-group-rate-details.component'
                            ).then((m) => m.BudgetingGroupRateDetailsComponent),
                        data: { permission: 'Pages.GeneralMasterInfo.BudgetingGroupRateDetails' },
                    },
                    {
                        path: 'clearance-request-fields',
                        loadComponent: () =>
                            import(
                                './admin/general-master-info/clearance-request-fields/clearance-request-fields.component'
                            ).then((m) => m.ClearanceRequestFieldsComponent),
                        data: { permission: 'Pages.GeneralMasterInfo.ClearanceRequestFields' },
                    },
                    {
                        path: 'clearance-request-field-details',
                        loadComponent: () =>
                            import(
                                './admin/general-master-info/clearance-request-field-details/clearance-request-field-details.component'
                            ).then((m) => m.ClearanceRequestFieldDetailsComponent),
                        data: { permission: 'Pages.GeneralMasterInfo.ClearanceRequestFieldDetails' },
                    },
                    {
                        path: 'disciplinary-types',
                        loadComponent: () =>
                            import(
                                './admin/general-master-info/disciplinary-types/disciplinary-types.component'
                            ).then((m) => m.DisciplinaryTypesComponent),
                        data: { permission: 'Pages.GeneralMasterInfo.DisciplinaryTypes' },
                    },
                    {
                        path: 'disciplinary-type-details',
                        loadComponent: () =>
                            import(
                                './admin/general-master-info/disciplinary-type-details/disciplinary-type-details.component'
                            ).then((m) => m.DisciplinaryTypeDetailsComponent),
                        data: { permission: 'Pages.GeneralMasterInfo.DisciplinaryTypeDetails' },
                    },
                    {
                        path: 'notification-configs',
                        loadComponent: () =>
                            import(
                                './admin/general-master-info/notification-configs/notification-configs.component'
                            ).then((m) => m.NotificationConfigsComponent),
                        data: { permission: 'Pages.GeneralMasterInfo.NotificationConfigs' },
                    },
                    {
                        path: 'notification-groups',
                        loadComponent: () =>
                            import(
                                './admin/general-master-info/notification-groups/notification-groups.component'
                            ).then((m) => m.NotificationGroupsComponent),
                        data: { permission: 'Pages.GeneralMasterInfo.NotificationGroups' },
                    },
                    {
                        path: 'document-types',
                        loadComponent: () =>
                            import('./admin/general-master-info/document-types/document-types.component').then(
                                (m) => m.DocumentTypesComponent
                            ),
                        data: { permission: 'Pages.GeneralMasterInfo.DocumentTypes' },
                    },
                    {
                        path: 'document-print-forms',
                        loadComponent: () =>
                            import(
                                './admin/general-master-info/document-print-forms/document-print-forms.component'
                            ).then((m) => m.DocumentPrintFormsComponent),
                        data: { permission: 'Pages.GeneralMasterInfo.DocumentPrintForms' },
                    },
                    {
                        path: 'loan-types',
                        loadComponent: () =>
                            import('./admin/general-master-info/loan-types/loan-types.component').then(
                                (m) => m.LoanTypesComponent
                            ),
                        data: { permission: 'Pages.GeneralMasterInfo.LoanTypes' },
                    },
                    {
                        path: 'loan-type-details',
                        loadComponent: () =>
                            import(
                                './admin/general-master-info/loan-type-details/loan-type-details.component'
                            ).then((m) => m.LoanTypeDetailsComponent),
                        data: { permission: 'Pages.GeneralMasterInfo.LoanTypeDetails' },
                    },
                    {
                        path: 'overtime-types',
                        loadComponent: () =>
                            import('./admin/general-master-info/overtime-types/overtime-types.component').then(
                                (m) => m.OvertimeTypesComponent
                            ),
                        data: { permission: 'Pages.GeneralMasterInfo.OvertimeTypes' },
                    },
                    {
                        path: 'overtime-type-details',
                        loadComponent: () =>
                            import(
                                './admin/general-master-info/overtime-type-details/overtime-type-details.component'
                            ).then((m) => m.OvertimeTypeDetailsComponent),
                        data: { permission: 'Pages.GeneralMasterInfo.OvertimeTypeDetails' },
                    },
                    {
                        path: 'assets',
                        loadComponent: () =>
                            import('./admin/general-master-info/assets/assets.component').then(
                                (m) => m.AssetsComponent
                            ),
                        data: { permission: 'Pages.GeneralMasterInfo.Assets' },
                    },
                    {
                        path: 'number-series',
                        loadComponent: () =>
                            import('./admin/general-master-info/number-series/number-series.component').then(
                                (m) => m.NumberSeriesComponent
                            ),
                        data: { permission: 'Pages.GeneralMasterInfo.NumberSeries' },
                    },
                    {
                        path: 'currency-convertors',
                        loadComponent: () =>
                            import(
                                './admin/general-master-info/currency-convertors/currency-convertors.component'
                            ).then((m) => m.CurrencyConvertorsComponent),
                        data: { permission: 'Pages.GeneralMasterInfo.CurrencyConvertors' },
                    },
                    simpleLookupRoute('asset-types', 'AssetTypes', 'AssetTypes', AssetTypesServiceProxy),
                    simpleLookupRoute('countries', 'Countries', 'Countries', CountriesServiceProxy),
                    simpleLookupRoute('budgeting-groups', 'BudgetingGroups', 'BudgetingGroups', BudgetingGroupsServiceProxy),
                    simpleLookupRoute('clearance-request-types', 'ClearanceRequestTypes', 'ClearanceRequestTypes', ClearanceRequestTypesServiceProxy),
                    simpleLookupRoute('bank-files', 'BankFiles', 'BankFiles', BankFilesServiceProxy),
                    simpleLookupRoute('benefit-types', 'BenefitTypes', 'BenefitTypes', BenefitTypesServiceProxy),
                    simpleLookupRoute('contract-types', 'ContractTypes', 'ContractTypes', ContractTypesServiceProxy),
                    simpleLookupRoute('date-types', 'DateTypes', 'DateTypes', DateTypesServiceProxy),
                    simpleLookupRoute('document-places', 'DocumentPlaces', 'DocumentPlaces', DocumentPlacesServiceProxy),
                    simpleLookupRoute('educations', 'Educations', 'Educations', EducationsServiceProxy),
                    simpleLookupRoute('education-branches', 'EducationBranches', 'EducationBranches', EducationBranchesServiceProxy),
                    simpleLookupRoute('governorates', 'Governorates', 'Governorates', GovernoratesServiceProxy),
                    simpleLookupRoute('grievance-types', 'GrievanceTypes', 'GrievanceTypes', GrievanceTypesServiceProxy),
                    simpleLookupRoute('hr-service-types', 'HRServiceTypes', 'HRServiceTypes', HRServiceTypesServiceProxy),
                    simpleLookupRoute('it-service-types', 'ITServiceTypes', 'ITServiceTypes', ITServiceTypesServiceProxy),
                    simpleLookupRoute('language-infos', 'LanguageInfos', 'LanguageInfos', LanguageInfosServiceProxy),
                    simpleLookupRoute('marital-statuses', 'MaritalStatuses', 'MaritalStatuses', MaritalStatusesServiceProxy),
                    simpleLookupRoute('nationality-groups', 'NationalityGroups', 'NationalityGroups', NationalityGroupsServiceProxy),
                    simpleLookupRoute('news-types', 'NewsTypes', 'NewsTypes', NewsTypesServiceProxy),
                    simpleLookupRoute('passport-types', 'PassportTypes', 'PassportTypes', PassportTypesServiceProxy),
                    simpleLookupRoute('posting-groups', 'PostingGroups', 'PostingGroups', PostingGroupsServiceProxy),
                    simpleLookupRoute('qualifications', 'Qualifications', 'Qualifications', QualificationsServiceProxy),
                    simpleLookupRoute('recruitment-types', 'RecruitmentTypes', 'RecruitmentTypes', RecruitmentTypesServiceProxy),
                    simpleLookupRoute('relationship-types', 'RelationshipTypes', 'RelationshipTypes', RelationshipTypesServiceProxy),
                    simpleLookupRoute('religions', 'Religions', 'Religions', ReligionsServiceProxy),
                    simpleLookupRoute('states', 'States', 'States', StatesServiceProxy),
                    simpleLookupRoute('task-types', 'TaskTypes', 'TaskTypes', TaskTypesServiceProxy),
                    simpleLookupRoute('titles', 'Titles', 'Titles', TitlesServiceProxy),
                    simpleLookupRoute('training-types', 'TrainingTypes', 'TrainingTypes', TrainingTypesServiceProxy),
                    simpleLookupRoute('universities', 'Universities', 'Universities', UniversitiesServiceProxy),
                    simpleLookupRoute('visa-types', 'VisaTypes', 'VisaTypes', VisaTypesServiceProxy),
                    simpleLookupRoute('work-visa-types', 'WorkVisaTypes', 'WorkVisaTypes', WorkVisaTypesServiceProxy),
                    simpleLookupRoute('cost-center-groups', 'CostCenterGroups', 'CostCenterGroups', CostCenterGroupsServiceProxy, 'OrganizationManagement'),
                    simpleLookupRoute('cost-element-types', 'CostElementTypes', 'CostElementTypes', CostElementTypesServiceProxy, 'OrganizationManagement'),
                    simpleLookupRoute('job-titles', 'JobTitles', 'JobTitles', JobTitlesServiceProxy, 'OrganizationManagement'),
                    {
                        path: 'cost-centers',
                        loadComponent: () =>
                            import('./admin/organization-management/cost-centers/cost-centers.component').then(
                                (m) => m.CostCentersComponent
                            ),
                        data: { permission: 'Pages.OrganizationManagement.CostCenters' },
                    },
                    {
                        path: 'cost-elements',
                        loadComponent: () =>
                            import('./admin/organization-management/cost-elements/cost-elements.component').then(
                                (m) => m.CostElementsComponent
                            ),
                        data: { permission: 'Pages.OrganizationManagement.CostElements' },
                    },
                    {
                        path: 'organization-unit-types',
                        loadComponent: () =>
                            import(
                                './admin/organization-management/organization-unit-types/organization-unit-types.component'
                            ).then((m) => m.OrganizationUnitTypesComponent),
                        data: { permission: 'Pages.OrganizationManagement.OrganizationUnitTypes' },
                    },
                    {
                        path: 'work-locations',
                        loadComponent: () =>
                            import('./admin/organization-management/work-locations/work-locations.component').then(
                                (m) => m.WorkLocationsComponent
                            ),
                        data: { permission: 'Pages.OrganizationManagement.WorkLocations' },
                    },
                    {
                        path: 'organization-bank-infos',
                        loadComponent: () =>
                            import(
                                './admin/organization-management/organization-bank-infos/organization-bank-infos.component'
                            ).then((m) => m.OrganizationBankInfosComponent),
                        data: { permission: 'Pages.OrganizationManagement.OrganizationBankInfos' },
                    },
                    {
                        path: 'org-units',
                        loadComponent: () =>
                            import('./admin/organization-management/org-units/org-units.component').then(
                                (m) => m.OrgUnitsComponent
                            ),
                        data: { permission: 'Pages.OrganizationManagement.OrgUnits' },
                    },
                    simpleLookupRoute('time-types', 'TimeTypes', 'TimeTypes', TimeTypesServiceProxy, 'TimeManagement'),
                    simpleLookupRoute('trans-types', 'TransTypes', 'TransTypes', TransTypesServiceProxy, 'TimeManagement'),
                    simpleLookupRoute('time-transfer-types', 'TimeTransferTypes', 'TimeTransferTypes', TimeTransferTypesServiceProxy, 'TimeManagement'),
                    {
                        path: 'break-shifts',
                        loadComponent: () =>
                            import('./admin/time-management/break-shifts/break-shifts.component').then(
                                (m) => m.BreakShiftsComponent
                            ),
                        data: { permission: 'Pages.TimeManagement.BreakShifts' },
                    },
                    {
                        path: 'duties',
                        loadComponent: () =>
                            import('./admin/time-management/duties/duties.component').then(
                                (m) => m.DutiesComponent
                            ),
                        data: { permission: 'Pages.TimeManagement.Duties' },
                    },
                    {
                        path: 'daily-duties',
                        loadComponent: () =>
                            import('./admin/time-management/daily-duties/daily-duties.component').then(
                                (m) => m.DailyDutiesComponent
                            ),
                        data: { permission: 'Pages.TimeManagement.DailyDuties' },
                    },
                    {
                        path: 'holiday-calendars',
                        loadComponent: () =>
                            import('./admin/time-management/holiday-calendars/holiday-calendars.component').then(
                                (m) => m.HolidayCalendarsComponent
                            ),
                        data: { permission: 'Pages.TimeManagement.HolidayCalendars' },
                    },
                    {
                        path: 'holidays',
                        loadComponent: () =>
                            import('./admin/time-management/holidays/holidays.component').then(
                                (m) => m.HolidaysComponent
                            ),
                        data: { permission: 'Pages.TimeManagement.Holidays' },
                    },
                    {
                        path: 'holiday-periods',
                        loadComponent: () =>
                            import('./admin/time-management/holiday-periods/holiday-periods.component').then(
                                (m) => m.HolidayPeriodsComponent
                            ),
                        data: { permission: 'Pages.TimeManagement.HolidayPeriods' },
                    },
                    {
                        path: 'time-policies',
                        loadComponent: () =>
                            import('./admin/time-management/time-policies/time-policies.component').then(
                                (m) => m.TimePoliciesComponent
                            ),
                        data: { permission: 'Pages.TimeManagement.TimePolicies' },
                    },
                    {
                        path: 'duty-periods',
                        loadComponent: () =>
                            import('./admin/time-management/duty-periods/duty-periods.component').then(
                                (m) => m.DutyPeriodsComponent
                            ),
                        data: { permission: 'Pages.TimeManagement.DutyPeriods' },
                    },
                    {
                        path: 'duty-period-details',
                        loadComponent: () =>
                            import('./admin/time-management/duty-period-details/duty-period-details.component').then(
                                (m) => m.DutyPeriodDetailsComponent
                            ),
                        data: { permission: 'Pages.TimeManagement.DutyPeriodDetails' },
                    },
                    {
                        path: 'roster-duty-periods',
                        loadComponent: () =>
                            import('./admin/time-management/roster-duty-periods/roster-duty-periods.component').then(
                                (m) => m.RosterDutyPeriodsComponent
                            ),
                        data: { permission: 'Pages.TimeManagement.RosterDutyPeriods' },
                    },
                    {
                        path: 'roster-duty-period-details',
                        loadComponent: () =>
                            import(
                                './admin/time-management/roster-duty-period-details/roster-duty-period-details.component'
                            ).then((m) => m.RosterDutyPeriodDetailsComponent),
                        data: { permission: 'Pages.TimeManagement.RosterDutyPeriodDetails' },
                    },
                    {
                        path: 'roster-duty-schedules',
                        loadComponent: () =>
                            import('./admin/time-management/roster-duty-schedules/roster-duty-schedules.component').then(
                                (m) => m.RosterDutySchedulesComponent
                            ),
                        data: { permission: 'Pages.TimeManagement.RosterDutySchedules' },
                    },
                    {
                        path: 'roster-groups',
                        loadComponent: () =>
                            import('./admin/time-management/roster-groups/roster-groups.component').then(
                                (m) => m.RosterGroupsComponent
                            ),
                        data: { permission: 'Pages.TimeManagement.RosterGroups' },
                    },
                    {
                        path: 'roster-group-details',
                        loadComponent: () =>
                            import('./admin/time-management/roster-group-details/roster-group-details.component').then(
                                (m) => m.RosterGroupDetailsComponent
                            ),
                        data: { permission: 'Pages.TimeManagement.RosterGroupDetails' },
                    },
                    {
                        path: 'roster-group-schedules',
                        loadComponent: () =>
                            import(
                                './admin/time-management/roster-group-schedules/roster-group-schedules.component'
                            ).then((m) => m.RosterGroupSchedulesComponent),
                        data: { permission: 'Pages.TimeManagement.RosterGroupSchedules' },
                    },
                    {
                        path: 'roster-daily-duties',
                        loadComponent: () =>
                            import('./admin/time-management/roster-daily-duties/roster-daily-duties.component').then(
                                (m) => m.RosterDailyDutiesComponent
                            ),
                        data: { permission: 'Pages.TimeManagement.RosterDailyDuties' },
                    },
                    {
                        path: 'work-location-duty-periods',
                        loadComponent: () =>
                            import(
                                './admin/time-management/work-location-duty-periods/work-location-duty-periods.component'
                            ).then((m) => m.WorkLocationDutyPeriodsComponent),
                        data: { permission: 'Pages.TimeManagement.WorkLocationDutyPeriods' },
                    },
                    {
                        path: 'time-events',
                        loadComponent: () =>
                            import('./admin/time-management/time-events/time-events.component').then(
                                (m) => m.TimeEventsComponent
                            ),
                        data: { permission: 'Pages.TimeManagement.TimeEvents' },
                    },
                    {
                        path: 'time-att-apis',
                        loadComponent: () =>
                            import('./admin/time-management/time-att-apis/time-att-apis.component').then(
                                (m) => m.TimeAttAPIsComponent
                            ),
                        data: { permission: 'Pages.TimeManagement.TimeAttAPIs' },
                    },
                    {
                        path: 'time-logs',
                        loadComponent: () =>
                            import('./admin/time-management/time-logs/time-logs.component').then(
                                (m) => m.TimeLogsComponent
                            ),
                        data: { permission: 'Pages.TimeManagement.TimeLogs' },
                    },
                    {
                        path: 'time-registers',
                        loadComponent: () =>
                            import('./admin/time-management/time-registers/time-registers.component').then(
                                (m) => m.TimeRegistersComponent
                            ),
                        data: { permission: 'Pages.TimeManagement.TimeRegisters' },
                    },
                    {
                        path: 'monthly-time-registers',
                        loadComponent: () =>
                            import(
                                './admin/time-management/monthly-time-registers/monthly-time-registers.component'
                            ).then((m) => m.MonthlyTimeRegistersComponent),
                        data: { permission: 'Pages.TimeManagement.MonthlyTimeRegisters' },
                    },
                    {
                        path: 'monthly-time-register-details',
                        loadComponent: () =>
                            import(
                                './admin/time-management/monthly-time-register-details/monthly-time-register-details.component'
                            ).then((m) => m.MonthlyTimeRegisterDetailsComponent),
                        data: { permission: 'Pages.TimeManagement.MonthlyTimeRegisterDetails' },
                    },
                    {
                        path: 'yearly-time-registers',
                        loadComponent: () =>
                            import(
                                './admin/time-management/yearly-time-registers/yearly-time-registers.component'
                            ).then((m) => m.YearlyTimeRegistersComponent),
                        data: { permission: 'Pages.TimeManagement.YearlyTimeRegisters' },
                    },
                    {
                        path: 'yearly-time-register-details',
                        loadComponent: () =>
                            import(
                                './admin/time-management/yearly-time-register-details/yearly-time-register-details.component'
                            ).then((m) => m.YearlyTimeRegisterDetailsComponent),
                        data: { permission: 'Pages.TimeManagement.YearlyTimeRegisterDetails' },
                    },
                    {
                        path: 'simulation-time-registers',
                        loadComponent: () =>
                            import(
                                './admin/time-management/simulation-time-registers/simulation-time-registers.component'
                            ).then((m) => m.SimulationTimeRegistersComponent),
                        data: { permission: 'Pages.TimeManagement.SimulationTimeRegisters' },
                    },
                    {
                        path: 'simulation-time-register-details',
                        loadComponent: () =>
                            import(
                                './admin/time-management/simulation-time-register-details/simulation-time-register-details.component'
                            ).then((m) => m.SimulationTimeRegisterDetailsComponent),
                        data: { permission: 'Pages.TimeManagement.SimulationTimeRegisterDetails' },
                    },
                    {
                        path: 'time-transfers',
                        loadComponent: () =>
                            import('./admin/time-management/time-transfers/time-transfers.component').then(
                                (m) => m.TimeTransfersComponent
                            ),
                        data: { permission: 'Pages.TimeManagement.TimeTransfers' },
                    },
                    simpleLookupRoute('account-types', 'AccountTypes', 'AccountTypes', AccountTypesServiceProxy, 'PayrollManagement'),
                    simpleLookupRoute('payroll-reason-types', 'PayrollReasonTypes', 'PayrollReasonTypes', PayrollReasonTypesServiceProxy, 'PayrollManagement'),
                    simpleLookupRoute('ticket-classes', 'TicketClasses', 'TicketClasses', TicketClassesServiceProxy, 'PayrollManagement'),
                    simpleLookupRoute('ticket-routes', 'TicketRoutes', 'TicketRoutes', TicketRoutesServiceProxy, 'PayrollManagement'),
                    simpleLookupRoute('absence-violation-rules', 'AbsenceViolationRules', 'AbsenceViolationRules', AbsenceViolationRulesServiceProxy, 'PayrollManagement'),
                    simpleLookupRoute('payroll-periods', 'PayrollPeriods', 'PayrollPeriods', PayrollPeriodsServiceProxy, 'PayrollManagement'),
                    simpleLookupRoute('wage-acc-groups', 'WageAccGroups', 'WageAccGroups', WageAccGroupsServiceProxy, 'PayrollManagement'),
                    {
                        path: 'attendance-types',
                        loadComponent: () =>
                            import('./admin/payroll-management/attendance-types/attendance-types.component').then(
                                (m) => m.AttendanceTypesComponent
                            ),
                        data: { permission: 'Pages.PayrollManagement.AttendanceTypes' },
                    },
                    {
                        path: 'ticket-pax-types',
                        loadComponent: () =>
                            import('./admin/payroll-management/ticket-pax-types/ticket-pax-types.component').then(
                                (m) => m.TicketPaxTypesComponent
                            ),
                        data: { permission: 'Pages.PayrollManagement.TicketPaxTypes' },
                    },
                    {
                        path: 'account-infos',
                        loadComponent: () =>
                            import('./admin/payroll-management/account-infos/account-infos.component').then(
                                (m) => m.AccountInfosComponent
                            ),
                        data: { permission: 'Pages.PayrollManagement.AccountInfos' },
                    },
                    {
                        path: 'absence-deduction-rules',
                        loadComponent: () =>
                            import(
                                './admin/payroll-management/absence-deduction-rules/absence-deduction-rules.component'
                            ).then((m) => m.AbsenceDeductionRulesComponent),
                        data: { permission: 'Pages.PayrollManagement.AbsenceDeductionRules' },
                    },
                    {
                        path: 'absence-ded-rule-details',
                        loadComponent: () =>
                            import(
                                './admin/payroll-management/absence-ded-rule-details/absence-ded-rule-details.component'
                            ).then((m) => m.AbsenceDedRuleDetailsComponent),
                        data: { permission: 'Pages.PayrollManagement.AbsenceDedRuleDetails' },
                    },
                    {
                        path: 'absence-violation-rule-details',
                        loadComponent: () =>
                            import(
                                './admin/payroll-management/absence-violation-rule-details/absence-violation-rule-details.component'
                            ).then((m) => m.AbsenceViolationRuleDetailsComponent),
                        data: { permission: 'Pages.PayrollManagement.AbsenceViolationRuleDetails' },
                    },
                    {
                        path: 'absence-violation-rule-exceptions',
                        loadComponent: () =>
                            import(
                                './admin/payroll-management/absence-violation-rule-exceptions/absence-violation-rule-exceptions.component'
                            ).then((m) => m.AbsenceViolationRuleExceptionsComponent),
                        data: { permission: 'Pages.PayrollManagement.AbsenceViolationRuleExceptions' },
                    },
                    {
                        path: 'absence-type-exceptions',
                        loadComponent: () =>
                            import(
                                './admin/payroll-management/absence-type-exceptions/absence-type-exceptions.component'
                            ).then((m) => m.AbsenceTypeExceptionsComponent),
                        data: { permission: 'Pages.PayrollManagement.AbsenceTypeExceptions' },
                    },
                    {
                        path: 'wage-type-exceptions',
                        loadComponent: () =>
                            import(
                                './admin/payroll-management/wage-type-exceptions/wage-type-exceptions.component'
                            ).then((m) => m.WageTypeExceptionsComponent),
                        data: { permission: 'Pages.PayrollManagement.WageTypeExceptions' },
                    },
                    {
                        path: 'absence-payments',
                        loadComponent: () =>
                            import('./admin/payroll-management/absence-payments/absence-payments.component').then(
                                (m) => m.AbsencePaymentsComponent
                            ),
                        data: { permission: 'Pages.PayrollManagement.AbsencePayments' },
                    },
                    {
                        path: 'absence-violation-action-records',
                        loadComponent: () =>
                            import(
                                './admin/payroll-management/absence-violation-action-records/absence-violation-action-records.component'
                            ).then((m) => m.AbsenceViolationActionRecordsComponent),
                        data: { permission: 'Pages.PayrollManagement.AbsenceViolationActionRecords' },
                    },
                    {
                        path: 'absence-violation-action-logs',
                        loadComponent: () =>
                            import(
                                './admin/payroll-management/absence-violation-action-logs/absence-violation-action-logs.component'
                            ).then((m) => m.AbsenceViolationActionLogsComponent),
                        data: { permission: 'Pages.PayrollManagement.AbsenceViolationActionLogs' },
                    },
                    {
                        path: 'absence-types',
                        loadComponent: () =>
                            import('./admin/payroll-management/absence-types/absence-types.component').then(
                                (m) => m.AbsenceTypesComponent
                            ),
                        data: { permission: 'Pages.PayrollManagement.AbsenceTypes' },
                    },
                    {
                        path: 'employee-groups',
                        loadComponent: () =>
                            import('./admin/payroll-management/employee-groups/employee-groups.component').then(
                                (m) => m.EmployeeGroupsComponent
                            ),
                        data: { permission: 'Pages.PayrollManagement.EmployeeGroups' },
                    },
                    {
                        path: 'employee-sub-groups',
                        loadComponent: () =>
                            import(
                                './admin/payroll-management/employee-sub-groups/employee-sub-groups.component'
                            ).then((m) => m.EmployeeSubGroupsComponent),
                        data: { permission: 'Pages.PayrollManagement.EmployeeSubGroups' },
                    },
                    {
                        path: 'employee-sub-groups/:id',
                        loadComponent: () =>
                            import(
                                './admin/payroll-management/employee-sub-group-detail/employee-sub-group-detail.component'
                            ).then((m) => m.EmployeeSubGroupDetailComponent),
                        data: { permission: 'Pages.PayrollManagement.EmployeeSubGroups' },
                    },
                    {
                        path: 'payroll-areas',
                        loadComponent: () =>
                            import('./admin/payroll-management/payroll-areas/payroll-areas.component').then(
                                (m) => m.PayrollAreasComponent
                            ),
                        data: { permission: 'Pages.PayrollManagement.PayrollAreas' },
                    },
                    {
                        path: 'payroll-calendars',
                        loadComponent: () =>
                            import('./admin/payroll-management/payroll-calendars/payroll-calendars.component').then(
                                (m) => m.PayrollCalendarsComponent
                            ),
                        data: { permission: 'Pages.PayrollManagement.PayrollCalendars' },
                    },
                    {
                        path: 'payroll-control-records',
                        loadComponent: () =>
                            import(
                                './admin/payroll-management/payroll-control-records/payroll-control-records.component'
                            ).then((m) => m.PayrollControlRecordsComponent),
                        data: { permission: 'Pages.PayrollManagement.PayrollControlRecords' },
                    },
                    {
                        path: 'wage-types',
                        loadComponent: () =>
                            import('./admin/payroll-management/wage-types/wage-types.component').then(
                                (m) => m.WageTypesComponent
                            ),
                        data: { permission: 'Pages.PayrollManagement.WageTypes' },
                    },
                    {
                        path: 'wage-acc-group-details',
                        loadComponent: () =>
                            import(
                                './admin/payroll-management/wage-acc-group-details/wage-acc-group-details.component'
                            ).then((m) => m.WageAccGroupDetailsComponent),
                        data: { permission: 'Pages.PayrollManagement.WageAccGroupDetails' },
                    },
                    {
                        path: 'wage-accounts',
                        loadComponent: () =>
                            import('./admin/payroll-management/wage-accounts/wage-accounts.component').then(
                                (m) => m.WageAccountsComponent
                            ),
                        data: { permission: 'Pages.PayrollManagement.WageAccounts' },
                    },
                    {
                        path: 'wage-type-leaves',
                        loadComponent: () =>
                            import('./admin/payroll-management/wage-type-leaves/wage-type-leaves.component').then(
                                (m) => m.WageTypeLeavesComponent
                            ),
                        data: { permission: 'Pages.PayrollManagement.WageTypeLeaves' },
                    },
                    {
                        path: 'wage-type-security-roles',
                        loadComponent: () =>
                            import(
                                './admin/payroll-management/wage-type-security-roles/wage-type-security-roles.component'
                            ).then((m) => m.WageTypeSecurityRolesComponent),
                        data: { permission: 'Pages.PayrollManagement.WageTypeSecurityRoles' },
                    },
                    {
                        path: 'wage-type-registers',
                        loadComponent: () =>
                            import(
                                './admin/payroll-management/wage-type-registers/wage-type-registers.component'
                            ).then((m) => m.WageTypeRegistersComponent),
                        data: { permission: 'Pages.PayrollManagement.WageTypeRegisters' },
                    },
                    {
                        path: 'indemnity-pay-rules',
                        loadComponent: () =>
                            import(
                                './admin/payroll-management/indemnity-pay-rules/indemnity-pay-rules.component'
                            ).then((m) => m.IndemnityPayRulesComponent),
                        data: { permission: 'Pages.PayrollManagement.IndemnityPayRules' },
                    },
                    {
                        path: 'indemnity-pay-rule-details',
                        loadComponent: () =>
                            import(
                                './admin/payroll-management/indemnity-pay-rule-details/indemnity-pay-rule-details.component'
                            ).then((m) => m.IndemnityPayRuleDetailsComponent),
                        data: { permission: 'Pages.PayrollManagement.IndemnityPayRuleDetails' },
                    },
                    {
                        path: 'org-unit-wage-acc-groups',
                        loadComponent: () =>
                            import(
                                './admin/payroll-management/org-unit-wage-acc-groups/org-unit-wage-acc-groups.component'
                            ).then((m) => m.OrgUnitWageAccGroupsComponent),
                        data: { permission: 'Pages.PayrollManagement.OrgUnitWageAccGroups' },
                    },
                    {
                        path: 'social-securities',
                        loadComponent: () =>
                            import(
                                './admin/payroll-management/social-securities/social-securities.component'
                            ).then((m) => m.SocialSecuritiesComponent),
                        data: { permission: 'Pages.PayrollManagement.SocialSecurities' },
                    },
                    {
                        path: 'social-security-details',
                        loadComponent: () =>
                            import(
                                './admin/payroll-management/social-security-details/social-security-details.component'
                            ).then((m) => m.SocialSecurityDetailsComponent),
                        data: { permission: 'Pages.PayrollManagement.SocialSecurityDetails' },
                    },
                    {
                        path: 'social-security-marital-allowances',
                        loadComponent: () =>
                            import(
                                './admin/payroll-management/social-security-marital-allowances/social-security-marital-allowances.component'
                            ).then((m) => m.SocialSecurityMaritalAllowancesComponent),
                        data: { permission: 'Pages.PayrollManagement.SocialSecurityMaritalAllowances' },
                    },
                    {
                        path: 'social-security-education-allowances',
                        loadComponent: () =>
                            import(
                                './admin/payroll-management/social-security-education-allowances/social-security-education-allowances.component'
                            ).then((m) => m.SocialSecurityEducationAllowancesComponent),
                        data: { permission: 'Pages.PayrollManagement.SocialSecurityEducationAllowances' },
                    },
                    {
                        path: 'social-security-rules',
                        loadComponent: () =>
                            import(
                                './admin/payroll-management/social-security-rules/social-security-rules.component'
                            ).then((m) => m.SocialSecurityRulesComponent),
                        data: { permission: 'Pages.PayrollManagement.SocialSecurityRules' },
                    },
                    {
                        path: 'ticket-types',
                        loadComponent: () =>
                            import('./admin/payroll-management/ticket-types/ticket-types.component').then(
                                (m) => m.TicketTypesComponent
                            ),
                        data: { permission: 'Pages.PayrollManagement.TicketTypes' },
                    },
                    {
                        path: 'ticket-fares',
                        loadComponent: () =>
                            import('./admin/payroll-management/ticket-fares/ticket-fares.component').then(
                                (m) => m.TicketFaresComponent
                            ),
                        data: { permission: 'Pages.PayrollManagement.TicketFares' },
                    },
                    {
                        path: 'ticket-accrual-summaries',
                        loadComponent: () =>
                            import(
                                './admin/payroll-management/ticket-accrual-summaries/ticket-accrual-summaries.component'
                            ).then((m) => m.TicketAccrualSummariesComponent),
                        data: { permission: 'Pages.PayrollManagement.TicketAccrualSummaries' },
                    },
                    {
                        path: 'temp-benefit-types',
                        loadComponent: () =>
                            import(
                                './admin/payroll-management/temp-benefit-types/temp-benefit-types.component'
                            ).then((m) => m.TempBenefitTypesComponent),
                        data: { permission: 'Pages.PayrollManagement.TempBenefitTypes' },
                    },
                    {
                        path: 'temp-employee-benefits',
                        loadComponent: () =>
                            import(
                                './admin/payroll-management/temp-employee-benefits/temp-employee-benefits.component'
                            ).then((m) => m.TempEmployeeBenefitsComponent),
                        data: { permission: 'Pages.PayrollManagement.TempEmployeeBenefits' },
                    },
                    {
                        path: 'payroll-logs',
                        loadComponent: () =>
                            import('./admin/payroll-management/payroll-logs/payroll-logs.component').then(
                                (m) => m.PayrollLogsComponent
                            ),
                        data: { permission: 'Pages.PayrollManagement.PayrollLogs' },
                    },
                    {
                        path: 'cost-registers',
                        loadComponent: () =>
                            import('./admin/payroll-management/cost-registers/cost-registers.component').then(
                                (m) => m.CostRegistersComponent
                            ),
                        data: { permission: 'Pages.PayrollManagement.CostRegisters' },
                    },
                    {
                        path: 'cost-register-details',
                        loadComponent: () =>
                            import(
                                './admin/payroll-management/cost-register-details/cost-register-details.component'
                            ).then((m) => m.CostRegisterDetailsComponent),
                        data: { permission: 'Pages.PayrollManagement.CostRegisterDetails' },
                    },
                    {
                        path: 'salary-registers',
                        loadComponent: () =>
                            import('./admin/payroll-management/salary-registers/salary-registers.component').then(
                                (m) => m.SalaryRegistersComponent
                            ),
                        data: { permission: 'Pages.PayrollManagement.SalaryRegisters' },
                    },
                    {
                        path: 'salary-register-details',
                        loadComponent: () =>
                            import(
                                './admin/payroll-management/salary-register-details/salary-register-details.component'
                            ).then((m) => m.SalaryRegisterDetailsComponent),
                        data: { permission: 'Pages.PayrollManagement.SalaryRegisterDetails' },
                    },
                    {
                        path: 'salary-register-days-details',
                        loadComponent: () =>
                            import(
                                './admin/payroll-management/salary-register-days-details/salary-register-days-details.component'
                            ).then((m) => m.SalaryRegisterDaysDetailsComponent),
                        data: { permission: 'Pages.PayrollManagement.SalaryRegisterDaysDetails' },
                    },
                    {
                        path: 'simulation-salary-registers',
                        loadComponent: () =>
                            import(
                                './admin/payroll-management/simulation-salary-registers/simulation-salary-registers.component'
                            ).then((m) => m.SimulationSalaryRegistersComponent),
                        data: { permission: 'Pages.PayrollManagement.SimulationSalaryRegisters' },
                    },
                    {
                        path: 'simulation-salary-register-details',
                        loadComponent: () =>
                            import(
                                './admin/payroll-management/simulation-salary-register-details/simulation-salary-register-details.component'
                            ).then((m) => m.SimulationSalaryRegisterDetailsComponent),
                        data: { permission: 'Pages.PayrollManagement.SimulationSalaryRegisterDetails' },
                    },
                    {
                        path: 'simulation-salary-register-days-details',
                        loadComponent: () =>
                            import(
                                './admin/payroll-management/simulation-salary-register-days-details/simulation-salary-register-days-details.component'
                            ).then((m) => m.SimulationSalaryRegisterDaysDetailsComponent),
                        data: { permission: 'Pages.PayrollManagement.SimulationSalaryRegisterDaysDetails' },
                    },
                    {
                        path: 'gratuity-accrual-summaries',
                        loadComponent: () =>
                            import(
                                './admin/payroll-management/gratuity-accrual-summaries/gratuity-accrual-summaries.component'
                            ).then((m) => m.GratuityAccrualSummariesComponent),
                        data: { permission: 'Pages.PayrollManagement.GratuityAccrualSummaries' },
                    },
                    {
                        path: 'leave-accrual-summaries',
                        loadComponent: () =>
                            import(
                                './admin/payroll-management/leave-accrual-summaries/leave-accrual-summaries.component'
                            ).then((m) => m.LeaveAccrualSummariesComponent),
                        data: { permission: 'Pages.PayrollManagement.LeaveAccrualSummaries' },
                    },
                    {
                        path: 'auditLogs',
                        loadComponent: () =>
                            import('./admin/audit-logs/audit-logs.component').then((m) => m.AuditLogsComponent),
                        data: { permission: 'Pages.Administration.AuditLogs' },
                    },
                    {
                        path: 'entity-changes/:entityId/:entityTypeFullName',
                        loadComponent: () =>
                            import('./admin/entity-changes/entity-changes.component').then(
                                (m) => m.EntityChangesComponent
                            ),
                        data: { permission: 'Pages.Administration.EntityChanges.FullHistory' },
                    },
                    {
                        path: 'maintenance',
                        loadComponent: () =>
                            import('./admin/maintenance/maintenance.component').then((m) => m.MaintenanceComponent),
                        data: { permission: 'Pages.Administration.Host.Maintenance' },
                    },
                    {
                        path: 'health-check-ui',
                        loadComponent: () =>
                            import('./admin/health-check-ui/health-check-ui.component').then(
                                (m) => m.HealthCheckUIComponent
                            ),
                        data: { permission: 'Pages.Administration.Host.HealthCheck' },
                    },
                    {
                        path: 'hostSettings',
                        loadComponent: () =>
                            import('./admin/settings/host-settings.component').then((m) => m.HostSettingsComponent),
                        data: { permission: 'Pages.Administration.Host.Settings' },
                    },
                    {
                        path: 'active-sessions',
                        loadComponent: () =>
                            import('./admin/active-sessions/active-sessions.component').then(
                                (m) => m.ActiveSessionsComponent
                            ),
                        data: { permission: 'Pages.Administration.ActiveSessions' },
                    },
                    {
                        path: 'editions',
                        loadComponent: () =>
                            import('./admin/editions/editions.component').then((m) => m.EditionsComponent),
                        data: { permission: 'Pages.Editions' },
                    },
                    {
                        path: 'languages',
                        loadComponent: () =>
                            import('./admin/languages/languages.component').then((m) => m.LanguagesComponent),
                        data: { permission: 'Pages.Administration.Languages' },
                    },
                    {
                        path: 'languages/:name/texts',
                        loadComponent: () =>
                            import('./admin/languages/language-texts.component').then((m) => m.LanguageTextsComponent),
                        data: { permission: 'Pages.Administration.Languages.ChangeTexts' },
                    },
                    {
                        path: 'tenants',
                        loadComponent: () =>
                            import('./admin/tenants/tenants.component').then((m) => m.TenantsComponent),
                        data: { permission: 'Pages.Tenants' },
                    },
                    {
                        path: 'employees',
                        loadComponent: () =>
                            import('./admin/personnel-administration/employees/employees.component').then(
                                (m) => m.EmployeesComponent
                            ),
                        data: { permission: 'Pages.PersonnelAdministration.Employees' },
                    },
                    {
                        path: 'employees/:id',
                        loadComponent: () =>
                            import(
                                './admin/personnel-administration/employee-detail/employee-detail.component'
                            ).then((m) => m.EmployeeDetailComponent),
                        data: { permission: 'Pages.PersonnelAdministration.Employees' },
                    },
                    {
                        path: 'employee-basic-pay-details',
                        loadComponent: () =>
                            import(
                                './admin/personnel-administration/employee-basic-pay-details/employee-basic-pay-details.component'
                            ).then((m) => m.EmployeeBasicPayDetailsComponent),
                        data: { permission: 'Pages.PersonnelAdministration.EmployeeBasicPayDetails' },
                    },
                    {
                        path: 'employee-loan-exemptions',
                        loadComponent: () =>
                            import(
                                './admin/personnel-administration/employee-loan-exemptions/employee-loan-exemptions.component'
                            ).then((m) => m.EmployeeLoanExemptionsComponent),
                        data: { permission: 'Pages.PersonnelAdministration.EmployeeLoanExemptions' },
                    },
                    {
                        path: 'employee-loan-recoveries',
                        loadComponent: () =>
                            import(
                                './admin/personnel-administration/employee-loan-recoveries/employee-loan-recoveries.component'
                            ).then((m) => m.EmployeeLoanRecoveriesComponent),
                        data: { permission: 'Pages.PersonnelAdministration.EmployeeLoanRecoveries' },
                    },
                    {
                        path: 'loan-installments',
                        loadComponent: () =>
                            import(
                                './admin/personnel-administration/loan-installments/loan-installments.component'
                            ).then((m) => m.LoanInstallmentsComponent),
                        data: { permission: 'Pages.PersonnelAdministration.LoanInstallments' },
                    },
                    {
                        path: 'employee-fund-details',
                        loadComponent: () =>
                            import(
                                './admin/personnel-administration/employee-fund-details/employee-fund-details.component'
                            ).then((m) => m.EmployeeFundDetailsComponent),
                        data: { permission: 'Pages.PersonnelAdministration.EmployeeFundDetails' },
                    },
                    {
                        path: 'employee-fund-exemptions',
                        loadComponent: () =>
                            import(
                                './admin/personnel-administration/employee-fund-exemptions/employee-fund-exemptions.component'
                            ).then((m) => m.EmployeeFundExemptionsComponent),
                        data: { permission: 'Pages.PersonnelAdministration.EmployeeFundExemptions' },
                    },
                    {
                        path: 'employee-dependant-exceptions',
                        loadComponent: () =>
                            import(
                                './admin/personnel-administration/employee-dependant-exceptions/employee-dependant-exceptions.component'
                            ).then((m) => m.EmployeeDependantExceptionsComponent),
                        data: { permission: 'Pages.PersonnelAdministration.EmployeeDependantExceptions' },
                    },
                    simpleLookupRoute('insurance-plans', 'InsurancePlans', 'InsurancePlans', InsurancePlansServiceProxy, 'BenefitsManagement'),
                    simpleLookupRoute('insurance-providers', 'InsuranceProviders', 'InsuranceProviders', InsuranceProvidersServiceProxy, 'BenefitsManagement'),
                    {
                        path: 'employee-insurance-details',
                        loadComponent: () =>
                            import(
                                './admin/benefits-management/employee-insurance-details/employee-insurance-details.component'
                            ).then((m) => m.EmployeeInsuranceDetailsComponent),
                        data: { permission: 'Pages.BenefitsManagement.EmployeeInsuranceDetails' },
                    },
                    {
                        path: 'company-insurance-details',
                        loadComponent: () =>
                            import(
                                './admin/benefits-management/company-insurance-details/company-insurance-details.component'
                            ).then((m) => m.CompanyInsuranceDetailsComponent),
                        data: { permission: 'Pages.BenefitsManagement.CompanyInsuranceDetails' },
                    },
                    simpleLookupRoute('business-types', 'BusinessTypes', 'BusinessTypes', BusinessTypesServiceProxy, 'GovtRelationsManagement'),
                    simpleLookupRoute('govt-job-categories', 'GovtJobCategories', 'GovtJobCategories', GovtJobCategoriesServiceProxy, 'GovtRelationsManagement'),
                    {
                        path: 'govt-job-titles',
                        loadComponent: () =>
                            import('./admin/govt-relations-management/govt-job-titles/govt-job-titles.component').then(
                                (m) => m.GovtJobTitlesComponent
                            ),
                        data: { permission: 'Pages.GovtRelationsManagement.GovtJobTitles' },
                    },
                    {
                        path: 'signatories',
                        loadComponent: () =>
                            import('./admin/govt-relations-management/signatories/signatories.component').then(
                                (m) => m.SignatoriesComponent
                            ),
                        data: { permission: 'Pages.GovtRelationsManagement.Signatories' },
                    },
                    {
                        path: 'organization-documents',
                        loadComponent: () =>
                            import(
                                './admin/govt-relations-management/organization-documents/organization-documents.component'
                            ).then((m) => m.OrganizationDocumentsComponent),
                        data: { permission: 'Pages.GovtRelationsManagement.OrganizationDocuments' },
                    },
                    {
                        path: 'sponsors',
                        loadComponent: () =>
                            import('./admin/govt-relations-management/sponsors/sponsors.component').then(
                                (m) => m.SponsorsComponent
                            ),
                        data: { permission: 'Pages.GovtRelationsManagement.Sponsors' },
                    },
                    {
                        path: 'sponsors/:id',
                        loadComponent: () =>
                            import(
                                './admin/govt-relations-management/sponsor-detail/sponsor-detail.component'
                            ).then((m) => m.SponsorDetailComponent),
                        data: { permission: 'Pages.GovtRelationsManagement.Sponsors' },
                    },
                    {
                        path: 'applicants',
                        loadComponent: () =>
                            import('./admin/recruitment/applicants/applicants.component').then(
                                (m) => m.ApplicantsComponent
                            ),
                        data: { permission: 'Pages.Recruitment.Applicants' },
                    },
                    {
                        path: 'applicants/:id',
                        loadComponent: () =>
                            import('./admin/recruitment/applicant-detail/applicant-detail.component').then(
                                (m) => m.ApplicantDetailComponent
                            ),
                        data: { permission: 'Pages.Recruitment.Applicants' },
                    },
                    {
                        path: 'vacancies',
                        loadComponent: () =>
                            import('./admin/recruitment/vacancies/vacancies.component').then(
                                (m) => m.VacanciesComponent
                            ),
                        data: { permission: 'Pages.Recruitment.Vacancies' },
                    },
                    {
                        path: 'application-statuses',
                        loadComponent: () =>
                            import('./admin/recruitment/application-statuses/application-statuses.component').then(
                                (m) => m.ApplicationStatusesComponent
                            ),
                        data: { permission: 'Pages.Recruitment.ApplicationStatuses' },
                    },
                    {
                        path: 'interview-details',
                        loadComponent: () =>
                            import('./admin/recruitment/interview-details/interview-details.component').then(
                                (m) => m.InterviewDetailsComponent
                            ),
                        data: { permission: 'Pages.Recruitment.InterviewDetails' },
                    },
                    {
                        path: 'onb-groups',
                        loadComponent: () =>
                            import('./admin/recruitment/onb-groups/onb-groups.component').then(
                                (m) => m.OnbGroupsComponent
                            ),
                        data: { permission: 'Pages.Recruitment.OnbGroups' },
                    },
                    {
                        path: 'onb-group-details',
                        loadComponent: () =>
                            import('./admin/recruitment/onb-group-details/onb-group-details.component').then(
                                (m) => m.OnbGroupDetailsComponent
                            ),
                        data: { permission: 'Pages.Recruitment.OnbGroupDetails' },
                    },
                    {
                        path: 'onb-tasks',
                        loadComponent: () =>
                            import('./admin/recruitment/onb-tasks/onb-tasks.component').then(
                                (m) => m.OnbTasksComponent
                            ),
                        data: { permission: 'Pages.Recruitment.OnbTasks' },
                    },
                    {
                        path: 'onb-templates',
                        loadComponent: () =>
                            import('./admin/recruitment/onb-templates/onb-templates.component').then(
                                (m) => m.OnbTemplatesComponent
                            ),
                        data: { permission: 'Pages.Recruitment.OnbTemplates' },
                    },
                    {
                        path: 'onb-template-details',
                        loadComponent: () =>
                            import('./admin/recruitment/onb-template-details/onb-template-details.component').then(
                                (m) => m.OnbTemplateDetailsComponent
                            ),
                        data: { permission: 'Pages.Recruitment.OnbTemplateDetails' },
                    },
                    {
                        path: 'onb-applicant-tasks',
                        loadComponent: () =>
                            import('./admin/recruitment/onb-applicant-tasks/onb-applicant-tasks.component').then(
                                (m) => m.OnbApplicantTasksComponent
                            ),
                        data: { permission: 'Pages.Recruitment.OnbApplicantTasks' },
                    },
                    {
                        path: 'onb-app-task-comments',
                        loadComponent: () =>
                            import('./admin/recruitment/onb-app-task-comments/onb-app-task-comments.component').then(
                                (m) => m.OnbAppTaskCommentsComponent
                            ),
                        data: { permission: 'Pages.Recruitment.OnbAppTaskComments' },
                    },
                    {
                        path: 'onb-video-links',
                        loadComponent: () =>
                            import('./admin/recruitment/onb-video-links/onb-video-links.component').then(
                                (m) => m.OnbVideoLinksComponent
                            ),
                        data: { permission: 'Pages.Recruitment.OnbVideoLinks' },
                    },
                    {
                        path: 'locations',
                        loadComponent: () =>
                            import('./admin/accommodation-management/locations/locations.component').then(
                                (m) => m.LocationsComponent
                            ),
                        data: { permission: 'Pages.AccommodationManagement.Locations' },
                    },
                    {
                        path: 'buildings',
                        loadComponent: () =>
                            import('./admin/accommodation-management/buildings/buildings.component').then(
                                (m) => m.BuildingsComponent
                            ),
                        data: { permission: 'Pages.AccommodationManagement.Buildings' },
                    },
                    {
                        path: 'building-contracts',
                        loadComponent: () =>
                            import(
                                './admin/accommodation-management/building-contracts/building-contracts.component'
                            ).then((m) => m.BuildingContractsComponent),
                        data: { permission: 'Pages.AccommodationManagement.BuildingContracts' },
                    },
                    {
                        path: 'floors',
                        loadComponent: () =>
                            import('./admin/accommodation-management/floors/floors.component').then(
                                (m) => m.FloorsComponent
                            ),
                        data: { permission: 'Pages.AccommodationManagement.Floors' },
                    },
                    {
                        path: 'flats',
                        loadComponent: () =>
                            import('./admin/accommodation-management/flats/flats.component').then(
                                (m) => m.FlatsComponent
                            ),
                        data: { permission: 'Pages.AccommodationManagement.Flats' },
                    },
                    {
                        path: 'rooms',
                        loadComponent: () =>
                            import('./admin/accommodation-management/rooms/rooms.component').then(
                                (m) => m.RoomsComponent
                            ),
                        data: { permission: 'Pages.AccommodationManagement.Rooms' },
                    },
                    {
                        path: 'beds',
                        loadComponent: () =>
                            import('./admin/accommodation-management/beds/beds.component').then(
                                (m) => m.BedsComponent
                            ),
                        data: { permission: 'Pages.AccommodationManagement.Beds' },
                    },
                    {
                        path: 'accommodation-types',
                        loadComponent: () =>
                            import(
                                './admin/accommodation-management/accommodation-types/accommodation-types.component'
                            ).then((m) => m.AccommodationTypesComponent),
                        data: { permission: 'Pages.AccommodationManagement.AccommodationTypes' },
                    },
                    {
                        path: 'accommodation-service-types',
                        loadComponent: () =>
                            import(
                                './admin/accommodation-management/accommodation-service-types/accommodation-service-types.component'
                            ).then((m) => m.AccommodationServiceTypesComponent),
                        data: { permission: 'Pages.AccommodationManagement.AccommodationServiceTypes' },
                    },
                    {
                        path: 'employee-accommodations',
                        loadComponent: () =>
                            import(
                                './admin/accommodation-management/employee-accommodations/employee-accommodations.component'
                            ).then((m) => m.EmployeeAccommodationsComponent),
                        data: { permission: 'Pages.AccommodationManagement.EmployeeAccommodations' },
                    },
                    {
                        path: 'accommodation-services',
                        loadComponent: () =>
                            import(
                                './admin/accommodation-management/accommodation-services/accommodation-services.component'
                            ).then((m) => m.AccommodationServicesComponent),
                        data: { permission: 'Pages.AccommodationManagement.AccommodationServices' },
                    },
                    {
                        path: 'surveys',
                        loadComponent: () =>
                            import('./admin/survey-management/surveys/surveys.component').then(
                                (m) => m.SurveysComponent
                            ),
                        data: { permission: 'Pages.SurveyManagement.Surveys' },
                    },
                    {
                        path: 'survey-details',
                        loadComponent: () =>
                            import('./admin/survey-management/survey-details/survey-details.component').then(
                                (m) => m.SurveyDetailsComponent
                            ),
                        data: { permission: 'Pages.SurveyManagement.SurveyDetails' },
                    },
                    {
                        path: 'employee-surveys',
                        loadComponent: () =>
                            import('./admin/survey-management/employee-surveys/employee-surveys.component').then(
                                (m) => m.EmployeeSurveysComponent
                            ),
                        data: { permission: 'Pages.SurveyManagement.EmployeeSurveys' },
                    },
                    {
                        path: 'employee-survey-details',
                        loadComponent: () =>
                            import(
                                './admin/survey-management/employee-survey-details/employee-survey-details.component'
                            ).then((m) => m.EmployeeSurveyDetailsComponent),
                        data: { permission: 'Pages.SurveyManagement.EmployeeSurveyDetails' },
                    },
                    simpleLookupRoute('project-types', 'ProjectTypes', 'ProjectTypes', ProjectTypesServiceProxy, 'ProjectManagement'),
                    {
                        path: 'projects',
                        loadComponent: () =>
                            import('./admin/project-management/projects/projects.component').then(
                                (m) => m.ProjectsComponent
                            ),
                        data: { permission: 'Pages.ProjectManagement.Projects' },
                    },
                    {
                        path: 'project-wage-types',
                        loadComponent: () =>
                            import(
                                './admin/project-management/project-wage-types/project-wage-types.component'
                            ).then((m) => m.ProjectWageTypesComponent),
                        data: { permission: 'Pages.ProjectManagement.ProjectWageTypes' },
                    },
                    {
                        path: 'employee-projects',
                        loadComponent: () =>
                            import(
                                './admin/project-management/employee-projects/employee-projects.component'
                            ).then((m) => m.EmployeeProjectsComponent),
                        data: { permission: 'Pages.ProjectManagement.EmployeeProjects' },
                    },
                    {
                        path: 'employee-project-details',
                        loadComponent: () =>
                            import(
                                './admin/project-management/employee-project-details/employee-project-details.component'
                            ).then((m) => m.EmployeeProjectDetailsComponent),
                        data: { permission: 'Pages.ProjectManagement.EmployeeProjectDetails' },
                    },
                    {
                        path: 'project-registers',
                        loadComponent: () =>
                            import(
                                './admin/project-management/project-registers/project-registers.component'
                            ).then((m) => m.ProjectRegistersComponent),
                        data: { permission: 'Pages.ProjectManagement.ProjectRegisters' },
                    },
                    {
                        path: 'project-register-details',
                        loadComponent: () =>
                            import(
                                './admin/project-management/project-register-details/project-register-details.component'
                            ).then((m) => m.ProjectRegisterDetailsComponent),
                        data: { permission: 'Pages.ProjectManagement.ProjectRegisterDetails' },
                    },
                    {
                        path: 'project-logs',
                        loadComponent: () =>
                            import('./admin/project-management/project-logs/project-logs.component').then(
                                (m) => m.ProjectLogsComponent
                            ),
                        data: { permission: 'Pages.ProjectManagement.ProjectLogs' },
                    },
                    simpleLookupRoute('expense-types', 'ExpenseTypes', 'ExpenseTypes', ExpenseTypesServiceProxy, 'TravelManagement'),
                    simpleLookupRoute('hotels', 'Hotels', 'Hotels', HotelsServiceProxy, 'TravelManagement'),
                    simpleLookupRoute('travel-airlines', 'TravelAirlines', 'TravelAirlines', TravelAirlinesServiceProxy, 'TravelManagement'),
                    simpleLookupRoute('travel-cancel-reasons', 'TravelCancelReasons', 'TravelCancelReasons', TravelCancelReasonsServiceProxy, 'TravelManagement'),
                    simpleLookupRoute('travel-destinations', 'TravelDestinations', 'TravelDestinations', TravelDestinationsServiceProxy, 'TravelManagement'),
                    {
                        path: 'travel-types',
                        loadComponent: () =>
                            import('./admin/travel-management/travel-types/travel-types.component').then(
                                (m) => m.TravelTypesComponent
                            ),
                        data: { permission: 'Pages.TravelManagement.TravelTypes' },
                    },
                    {
                        path: 'travel-per-diems',
                        loadComponent: () =>
                            import('./admin/travel-management/travel-per-diems/travel-per-diems.component').then(
                                (m) => m.TravelPerDiemsComponent
                            ),
                        data: { permission: 'Pages.TravelManagement.TravelPerDiems' },
                    },
                    {
                        path: 'travels',
                        loadComponent: () =>
                            import('./admin/travel-management/travels/travels.component').then(
                                (m) => m.TravelsComponent
                            ),
                        data: { permission: 'Pages.TravelManagement.Travels' },
                    },
                    {
                        path: 'travel-details',
                        loadComponent: () =>
                            import('./admin/travel-management/travel-details/travel-details.component').then(
                                (m) => m.TravelDetailsComponent
                            ),
                        data: { permission: 'Pages.TravelManagement.TravelDetails' },
                    },
                    {
                        path: 'travel-detail-costs',
                        loadComponent: () =>
                            import(
                                './admin/travel-management/travel-detail-costs/travel-detail-costs.component'
                            ).then((m) => m.TravelDetailCostsComponent),
                        data: { permission: 'Pages.TravelManagement.TravelDetailCosts' },
                    },
                    {
                        path: 'travel-feedbacks',
                        loadComponent: () =>
                            import('./admin/travel-management/travel-feedbacks/travel-feedbacks.component').then(
                                (m) => m.TravelFeedbacksComponent
                            ),
                        data: { permission: 'Pages.TravelManagement.TravelFeedbacks' },
                    },
                    {
                        path: 'workflow-roles',
                        loadComponent: () =>
                            import('./admin/workflow/workflow-roles/workflow-roles.component').then(
                                (m) => m.WorkflowRolesComponent
                            ),
                        data: { permission: 'Pages.Workflow.WorkflowRoles' },
                    },
                    {
                        path: 'workflow-groups',
                        loadComponent: () =>
                            import('./admin/workflow/workflow-groups/workflow-groups.component').then(
                                (m) => m.WorkflowGroupsComponent
                            ),
                        data: { permission: 'Pages.Workflow.WorkflowGroups' },
                    },
                    {
                        path: 'workflow-groups/:id',
                        loadComponent: () =>
                            import('./admin/workflow/workflow-group-detail/workflow-group-detail.component').then(
                                (m) => m.WorkflowGroupDetailComponent
                            ),
                        data: { permission: 'Pages.Workflow.WorkflowGroups' },
                    },
                    {
                        path: 'request-masters',
                        loadComponent: () =>
                            import('./admin/workflow/request-masters/request-masters.component').then(
                                (m) => m.RequestMastersComponent
                            ),
                        data: { permission: 'Pages.Workflow.RequestMasters' },
                    },
                    {
                        path: 'request-details',
                        loadComponent: () =>
                            import('./admin/workflow/request-details/request-details.component').then(
                                (m) => m.RequestDetailsComponent
                            ),
                        data: { permission: 'Pages.Workflow.RequestDetails' },
                    },
                    {
                        path: 'request-detail-parallels',
                        loadComponent: () =>
                            import(
                                './admin/workflow/request-detail-parallels/request-detail-parallels.component'
                            ).then((m) => m.RequestDetailParallelsComponent),
                        data: { permission: 'Pages.Workflow.RequestDetailParallels' },
                    },
                    {
                        path: 'workflow-requests',
                        loadComponent: () =>
                            import('./admin/workflow/workflow-requests/workflow-requests.component').then(
                                (m) => m.WorkflowRequestsComponent
                            ),
                        data: { permission: 'Pages.Workflow.WorkflowRequests' },
                    },
                    {
                        path: 'request-logs',
                        loadComponent: () =>
                            import('./admin/workflow/request-logs/request-logs.component').then(
                                (m) => m.RequestLogsComponent
                            ),
                        data: { permission: 'Pages.Workflow.RequestLogs' },
                    },
                    {
                        path: 'work-flow-approvers',
                        loadComponent: () =>
                            import('./admin/workflow/work-flow-approvers/work-flow-approvers.component').then(
                                (m) => m.WorkFlowApproversComponent
                            ),
                        data: { permission: 'Pages.Workflow.WorkFlowApprovers' },
                    },
                    {
                        path: 'workflow-remarks',
                        loadComponent: () =>
                            import('./admin/workflow/workflow-remarks/workflow-remarks.component').then(
                                (m) => m.WorkflowRemarksComponent
                            ),
                        data: { permission: 'Pages.Workflow.WorkflowRemarks' },
                    },
                    {
                        path: 'wf-read-logs',
                        loadComponent: () =>
                            import('./admin/workflow/wf-read-logs/wf-read-logs.component').then(
                                (m) => m.WFReadLogsComponent
                            ),
                        data: { permission: 'Pages.Workflow.WFReadLogs' },
                    },
                    {
                        path: 'form-types',
                        loadComponent: () =>
                            import('./admin/performance-management/form-types/form-types.component').then(
                                (m) => m.FormTypesComponent
                            ),
                        data: { permission: 'Pages.PerformanceManagement.FormTypes' },
                    },
                    {
                        path: 'kpi-categories',
                        loadComponent: () =>
                            import('./admin/performance-management/kpi-categories/kpi-categories.component').then(
                                (m) => m.KpiCategoriesComponent
                            ),
                        data: { permission: 'Pages.PerformanceManagement.KpiCategories' },
                    },
                    {
                        path: 'kpi-infos',
                        loadComponent: () =>
                            import('./admin/performance-management/kpi-infos/kpi-infos.component').then(
                                (m) => m.KpiInfosComponent
                            ),
                        data: { permission: 'Pages.PerformanceManagement.KpiInfos' },
                    },
                    {
                        path: 'pa-ratings',
                        loadComponent: () =>
                            import('./admin/performance-management/pa-ratings/pa-ratings.component').then(
                                (m) => m.PARatingsComponent
                            ),
                        data: { permission: 'Pages.PerformanceManagement.PARatings' },
                    },
                    {
                        path: 'pa-cycles',
                        loadComponent: () =>
                            import('./admin/performance-management/pa-cycles/pa-cycles.component').then(
                                (m) => m.PACyclesComponent
                            ),
                        data: { permission: 'Pages.PerformanceManagement.PACycles' },
                    },
                    {
                        path: 'pa-periods',
                        loadComponent: () =>
                            import('./admin/performance-management/pa-periods/pa-periods.component').then(
                                (m) => m.PAPeriodsComponent
                            ),
                        data: { permission: 'Pages.PerformanceManagement.PAPeriods' },
                    },
                    {
                        path: 'pa-timelines',
                        loadComponent: () =>
                            import('./admin/performance-management/pa-timelines/pa-timelines.component').then(
                                (m) => m.PATimelinesComponent
                            ),
                        data: { permission: 'Pages.PerformanceManagement.PATimelines' },
                    },
                    {
                        path: 'pa-timeline-exceptions',
                        loadComponent: () =>
                            import(
                                './admin/performance-management/pa-timeline-exceptions/pa-timeline-exceptions.component'
                            ).then((m) => m.PATimelineExceptionsComponent),
                        data: { permission: 'Pages.PerformanceManagement.PATimelineExceptions' },
                    },
                    {
                        path: 'pa-peer-nominees',
                        loadComponent: () =>
                            import(
                                './admin/performance-management/pa-peer-nominees/pa-peer-nominees.component'
                            ).then((m) => m.PAPeerNomineesComponent),
                        data: { permission: 'Pages.PerformanceManagement.PAPeerNominees' },
                    },
                    {
                        path: 'kpi-by-job-titles',
                        loadComponent: () =>
                            import(
                                './admin/performance-management/kpi-by-job-titles/kpi-by-job-titles.component'
                            ).then((m) => m.KpiByJobTitlesComponent),
                        data: { permission: 'Pages.PerformanceManagement.KpiByJobTitles' },
                    },
                    {
                        path: 'kpi-by-job-title-details',
                        loadComponent: () =>
                            import(
                                './admin/performance-management/kpi-by-job-title-details/kpi-by-job-title-details.component'
                            ).then((m) => m.KpiByJobTitleDetailsComponent),
                        data: { permission: 'Pages.PerformanceManagement.KpiByJobTitles' },
                    },
                    {
                        path: 'kpi-by-org-units',
                        loadComponent: () =>
                            import(
                                './admin/performance-management/kpi-by-org-units/kpi-by-org-units.component'
                            ).then((m) => m.KpiByOrgUnitsComponent),
                        data: { permission: 'Pages.PerformanceManagement.KpiByOrgUnits' },
                    },
                    {
                        path: 'kpi-by-org-unit-details',
                        loadComponent: () =>
                            import(
                                './admin/performance-management/kpi-by-org-unit-details/kpi-by-org-unit-details.component'
                            ).then((m) => m.KpiByOrgUnitDetailsComponent),
                        data: { permission: 'Pages.PerformanceManagement.KpiByOrgUnits' },
                    },
                    {
                        path: 'pa-forms',
                        loadComponent: () =>
                            import('./admin/performance-management/pa-forms/pa-forms.component').then(
                                (m) => m.PAFormsComponent
                            ),
                        data: { permission: 'Pages.PerformanceManagement.PAForms' },
                    },
                    {
                        path: 'pa-forms/:id',
                        loadComponent: () =>
                            import(
                                './admin/performance-management/pa-form-detail/pa-form-detail.component'
                            ).then((m) => m.PAFormDetailComponent),
                        data: { permission: 'Pages.PerformanceManagement.PAForms' },
                    },
                    {
                        path: 'pa-employees',
                        loadComponent: () =>
                            import('./admin/performance-management/pa-employees/pa-employees.component').then(
                                (m) => m.PAEmployeesComponent
                            ),
                        data: { permission: 'Pages.PerformanceManagement.PAEmployees' },
                    },
                    {
                        path: 'pa-employees/:id',
                        loadComponent: () =>
                            import(
                                './admin/performance-management/pa-employee-detail/pa-employee-detail.component'
                            ).then((m) => m.PAEmployeeDetailComponent),
                        data: { permission: 'Pages.PerformanceManagement.PAEmployees' },
                    },
                    {
                        path: 'competency-types',
                        loadComponent: () =>
                            import(
                                './admin/competency-development/competency-types/competency-types.component'
                            ).then((m) => m.CompetencyTypesComponent),
                        data: { permission: 'Pages.CompetencyDevelopment.CompetencyTypes' },
                    },
                    {
                        path: 'competencies',
                        loadComponent: () =>
                            import(
                                './admin/competency-development/competencies/competencies.component'
                            ).then((m) => m.CompetenciesComponent),
                        data: { permission: 'Pages.CompetencyDevelopment.Competencies' },
                    },
                    {
                        path: 'competency-by-job-titles',
                        loadComponent: () =>
                            import(
                                './admin/competency-development/competency-by-job-titles/competency-by-job-titles.component'
                            ).then((m) => m.CompetencyByJobTitlesComponent),
                        data: { permission: 'Pages.CompetencyDevelopment.CompetencyByJobTitles' },
                    },
                    {
                        path: 'competency-by-job-title-details',
                        loadComponent: () =>
                            import(
                                './admin/competency-development/competency-by-job-title-details/competency-by-job-title-details.component'
                            ).then((m) => m.CompetencyByJobTitleDetailsComponent),
                        data: { permission: 'Pages.CompetencyDevelopment.CompetencyByJobTitles' },
                    },
                    {
                        path: 'cd-periods',
                        loadComponent: () =>
                            import(
                                './admin/competency-development/cd-periods/cd-periods.component'
                            ).then((m) => m.CDPeriodsComponent),
                        data: { permission: 'Pages.CompetencyDevelopment.CDPeriods' },
                    },
                    {
                        path: 'cd-ratings',
                        loadComponent: () =>
                            import(
                                './admin/competency-development/cd-ratings/cd-ratings.component'
                            ).then((m) => m.CDRatingsComponent),
                        data: { permission: 'Pages.CompetencyDevelopment.CDRatings' },
                    },
                    {
                        path: 'cd-forms',
                        loadComponent: () =>
                            import(
                                './admin/competency-development/cd-forms/cd-forms.component'
                            ).then((m) => m.CDFormsComponent),
                        data: { permission: 'Pages.CompetencyDevelopment.CDForms' },
                    },
                    {
                        path: 'cd-forms/:id',
                        loadComponent: () =>
                            import(
                                './admin/competency-development/cd-form-detail/cd-form-detail.component'
                            ).then((m) => m.CDFormDetailComponent),
                        data: { permission: 'Pages.CompetencyDevelopment.CDForms' },
                    },
                    {
                        path: 'domain-types',
                        loadComponent: () =>
                            import('./admin/report-management/domain-types/domain-types.component').then(
                                (m) => m.DomainTypesComponent
                            ),
                        data: { permission: 'Pages.ReportManagement.DomainTypes' },
                    },
                    {
                        path: 'report-types',
                        loadComponent: () =>
                            import('./admin/report-management/report-types/report-types.component').then(
                                (m) => m.ReportTypesComponent
                            ),
                        data: { permission: 'Pages.ReportManagement.ReportTypes' },
                    },
                    {
                        path: 'print-reports',
                        loadComponent: () =>
                            import('./admin/report-management/print-reports/print-reports.component').then(
                                (m) => m.PrintReportsComponent
                            ),
                        data: { permission: 'Pages.ReportManagement.PrintReports' },
                    },
                    {
                        path: 'print-report-permissions',
                        loadComponent: () =>
                            import(
                                './admin/report-management/print-report-permissions/print-report-permissions.component'
                            ).then((m) => m.PrintReportPermissionsComponent),
                        data: { permission: 'Pages.ReportManagement.PrintReportPermissions' },
                    },
                    {
                        path: 'print-report-permission-details',
                        loadComponent: () =>
                            import(
                                './admin/report-management/print-report-permission-details/print-report-permission-details.component'
                            ).then((m) => m.PrintReportPermissionDetailsComponent),
                        data: { permission: 'Pages.ReportManagement.PrintReportPermissionDetails' },
                    },
                    {
                        path: 'report-masters',
                        loadComponent: () =>
                            import('./admin/report-management/report-masters/report-masters.component').then(
                                (m) => m.ReportMastersComponent
                            ),
                        data: { permission: 'Pages.ReportManagement.ReportMasters' },
                    },
                    {
                        path: 'report-details',
                        loadComponent: () =>
                            import('./admin/report-management/report-details/report-details.component').then(
                                (m) => m.ReportDetailsComponent
                            ),
                        data: { permission: 'Pages.ReportManagement.ReportDetails' },
                    },
                    {
                        path: 'report-settings',
                        loadComponent: () =>
                            import('./admin/report-management/report-settings/report-settings.component').then(
                                (m) => m.ReportSettingsComponent
                            ),
                        data: { permission: 'Pages.ReportManagement.ReportSettings' },
                    },
                    {
                        path: 'report-setting-details',
                        loadComponent: () =>
                            import(
                                './admin/report-management/report-setting-details/report-setting-details.component'
                            ).then((m) => m.ReportSettingDetailsComponent),
                        data: { permission: 'Pages.ReportManagement.ReportSettingDetails' },
                    },
                    {
                        path: 'report-infos',
                        loadComponent: () =>
                            import('./admin/report-management/report-infos/report-infos.component').then(
                                (m) => m.ReportInfosComponent
                            ),
                        data: { permission: 'Pages.ReportManagement.ReportInfos' },
                    },
                    {
                        path: 'report-info-details',
                        loadComponent: () =>
                            import(
                                './admin/report-management/report-info-details/report-info-details.component'
                            ).then((m) => m.ReportInfoDetailsComponent),
                        data: { permission: 'Pages.ReportManagement.ReportInfoDetails' },
                    },
                    {
                        path: 'organization-units',
                        loadComponent: () =>
                            import('./admin/organization-units/organization-units.component').then(
                                (m) => m.OrganizationUnitsComponent
                            ),
                        data: { permission: 'Pages.Administration.OrganizationUnits' },
                    },
                    {
                        path: 'subscription-management',
                        loadComponent: () =>
                            import('./admin/subscription-management/subscription-management.component').then(
                                (m) => m.SubscriptionManagementComponent
                            ),
                        data: { permission: 'Pages.Administration.Tenant.SubscriptionManagement' },
                    },
                    {
                        path: 'invoice/:paymentId',
                        loadComponent: () =>
                            import('./admin/subscription-management/invoice/invoice.component').then(
                                (m) => m.InvoiceComponent
                            ),
                        data: { permission: 'Pages.Administration.Tenant.SubscriptionManagement' },
                    },
                    {
                        path: 'tenantSettings',
                        loadComponent: () =>
                            import('./admin/settings/tenant-settings.component').then((m) => m.TenantSettingsComponent),
                        data: { permission: 'Pages.Administration.Tenant.Settings' },
                    },
                    {
                        path: 'hostDashboard',
                        loadComponent: () =>
                            import('./admin/dashboard/host-dashboard.component').then((m) => m.HostDashboardComponent),
                        data: { permission: 'Pages.Administration.Host.Dashboard' },
                    },
                    {
                        path: 'demo-ui-components',
                        loadComponent: () =>
                            import('./admin/demo-ui-components/demo-ui-components.component').then(
                                (m) => m.DemoUiComponentsComponent
                            ),
                        data: { permission: 'Pages.DemoUiComponents' },
                    },
                    {
                        path: 'ui-customization',
                        loadComponent: () =>
                            import('./admin/ui-customization/ui-customization.component').then(
                                (m) => m.UiCustomizationComponent
                            ),
                        data: {},
                    },
                    {
                        path: 'webhook-subscriptions',
                        loadComponent: () =>
                            import('./admin/webhook-subscription/webhook-subscription.component').then(
                                (m) => m.WebhookSubscriptionComponent
                            ),
                        data: { permission: 'Pages.Administration.WebhookSubscription' },
                    },
                    {
                        path: 'webhook-subscriptions-detail',
                        loadComponent: () =>
                            import('./admin/webhook-subscription/webhook-subscription-detail.component').then(
                                (m) => m.WebhookSubscriptionDetailComponent
                            ),
                        data: { permission: 'Pages.Administration.WebhookSubscription.Detail' },
                    },
                    {
                        path: 'webhook-event-detail',
                        loadComponent: () =>
                            import('./admin/webhook-subscription/webhook-event-detail.component').then(
                                (m) => m.WebhookEventDetailComponent
                            ),
                        data: { permission: 'Pages.Administration.WebhookSubscription.Detail' },
                    },
                    {
                        path: 'dynamic-property',
                        loadComponent: () =>
                            import('./admin/dynamic-properties/dynamic-property.component').then(
                                (m) => m.DynamicPropertyComponent
                            ),
                        data: { permission: 'Pages.Administration.DynamicEntityProperties' },
                    },
                    {
                        path: 'dynamic-entity-property/:entityFullName',
                        loadComponent: () =>
                            import('./admin/dynamic-properties/dynamic-entity-properties/dynamic-entity-property-list.component').then(
                                (m) => m.DynamicEntityPropertyListComponent
                            ),
                        data: { permission: 'Pages.Administration.DynamicEntityProperties' },
                    },
                    {
                        path: 'dynamic-entity-property-value/manage-all/:entityFullName/:rowId',
                        loadComponent: () =>
                            import('./admin/dynamic-properties/dynamic-entity-properties/value/dynamic-entity-property-value.component').then(
                                (m) => m.DynamicEntityPropertyValueComponent
                            ),
                        data: { permission: 'Pages.Administration.DynamicEntityProperties' },
                    },
                    {
                        path: 'login-attempts',
                        loadComponent: () =>
                            import('./admin/login-attempts/login-attempts.component').then(
                                (m) => m.LoginAttemptsComponent
                            ),
                        data: {},
                    },
                    {
                        path: 'mass-notifications',
                        loadComponent: () =>
                            import('./admin/mass-notifications/mass-notifications.component').then(
                                (m) => m.MassNotificationsComponent
                            ),
                        data: { permission: 'Pages.Administration.MassNotification' },
                    },
                    {
                        path: 'manpower-planning-periods',
                        loadComponent: () =>
                            import(
                                './admin/manpower-planning-management/manpower-planning-periods/manpower-planning-periods.component'
                            ).then((m) => m.ManpowerPlanningPeriodsComponent),
                        data: { permission: 'Pages.ManpowerPlanningManagement.ManpowerPlanningPeriods' },
                    },
                    {
                        path: 'positions',
                        loadComponent: () =>
                            import('./admin/manpower-planning-management/positions/positions.component').then(
                                (m) => m.PositionsComponent
                            ),
                        data: { permission: 'Pages.ManpowerPlanningManagement.Positions' },
                    },
                    {
                        path: 'position-relationships',
                        loadComponent: () =>
                            import(
                                './admin/manpower-planning-management/position-relationships/position-relationships.component'
                            ).then((m) => m.PositionRelationshipsComponent),
                        data: { permission: 'Pages.ManpowerPlanningManagement.PositionRelationships' },
                    },
                    {
                        path: 'manpower-plannings',
                        loadComponent: () =>
                            import(
                                './admin/manpower-planning-management/manpower-plannings/manpower-plannings.component'
                            ).then((m) => m.ManpowerPlanningsComponent),
                        data: { permission: 'Pages.ManpowerPlanningManagement.ManpowerPlannings' },
                    },
                    {
                        path: 'manpower-plannings/:id/details',
                        loadComponent: () =>
                            import(
                                './admin/manpower-planning-management/manpower-planning-detail/manpower-planning-detail.component'
                            ).then((m) => m.ManpowerPlanningDetailComponent),
                        data: { permission: 'Pages.ManpowerPlanningManagement.ManpowerPlanningDetails' },
                    },
                    {
                        path: 'training-courses',
                        loadComponent: () =>
                            import('./admin/training-management/training-courses/training-courses.component').then(
                                (m) => m.TrainingCoursesComponent
                            ),
                        data: { permission: 'Pages.TrainingManagement.TrainingCourses' },
                    },
                    {
                        path: 'training-schedules',
                        loadComponent: () =>
                            import('./admin/training-management/training-schedules/training-schedules.component').then(
                                (m) => m.TrainingSchedulesComponent
                            ),
                        data: { permission: 'Pages.TrainingManagement.TrainingSchedules' },
                    },
                    {
                        path: 'employee-trainings',
                        loadComponent: () =>
                            import('./admin/training-management/employee-trainings/employee-trainings.component').then(
                                (m) => m.EmployeeTrainingsComponent
                            ),
                        data: { permission: 'Pages.TrainingManagement.EmployeeTrainings' },
                    },
                    {
                        path: 'training-attendances',
                        loadComponent: () =>
                            import('./admin/training-management/training-attendances/training-attendances.component').then(
                                (m) => m.TrainingAttendancesComponent
                            ),
                        data: { permission: 'Pages.TrainingManagement.TrainingAttendances' },
                    },
                    simpleLookupRoute('job-families', 'JobFamilies', 'JobFamilies', JobFamiliesServiceProxy, 'JobProfileManagement'),
                    simpleLookupRoute('job-functions', 'JobFunctions', 'JobFunctions', JobFunctionsServiceProxy, 'JobProfileManagement'),
                    simpleLookupRoute('job-responsibilities', 'JobResponsibilities', 'JobResponsibilities', JobResponsibilitiesServiceProxy, 'JobProfileManagement'),
                    simpleLookupRoute('education-certificates', 'EducationCertificates', 'EducationCertificates', EducationCertificatesServiceProxy, 'JobProfileManagement'),
                    simpleLookupRoute('education-degrees', 'EducationDegrees', 'EducationDegrees', EducationDegreesServiceProxy, 'JobProfileManagement'),
                    simpleLookupRoute('interview-questions', 'InterviewQuestions', 'InterviewQuestions', InterviewQuestionsServiceProxy, 'JobProfileManagement'),
                    simpleLookupRoute('skills', 'Skills', 'Skills', SkillsServiceProxy, 'JobProfileManagement'),
                    {
                        path: 'job-descriptions',
                        loadComponent: () =>
                            import('./admin/job-profile-management/job-descriptions/job-descriptions.component').then(
                                (m) => m.JobDescriptionsComponent
                            ),
                        data: { permission: 'Pages.JobProfileManagement.JobDescriptions' },
                    },
                    {
                        path: 'job-descriptions/:id',
                        loadComponent: () =>
                            import(
                                './admin/job-profile-management/job-description-detail/job-description-detail.component'
                            ).then((m) => m.JobDescriptionDetailComponent),
                        data: { permission: 'Pages.JobProfileManagement.JobDescriptions' },
                    },
                    {
                        path: 'job-description-acknowledgements',
                        loadComponent: () =>
                            import(
                                './admin/job-profile-management/job-description-acknowledgements/job-description-acknowledgements.component'
                            ).then((m) => m.JobDescriptionAcknowledgementsComponent),
                        data: { permission: 'Pages.JobProfileManagement.JobDescriptionAcknowledgements' },
                    },
                    simpleLookupRoute('gate-pass-types', 'GatePassTypes', 'GatePassTypes', GatePassTypesServiceProxy, 'GatePassManagement'),
                    simpleLookupRoute('gate-pass-movements', 'GatePassMovements', 'GatePassMovements', GatePassMovementsServiceProxy, 'GatePassManagement'),
                    simpleLookupRoute('gate-pass-statuses', 'GatePassStatuses', 'GatePassStatuses', GatePassStatusesServiceProxy, 'GatePassManagement'),
                    simpleLookupRoute('person-types', 'PersonTypes', 'PersonTypes', PersonTypesServiceProxy, 'GatePassManagement'),
                    simpleLookupRoute('vehicle-types', 'VehicleTypes', 'VehicleTypes', VehicleTypesServiceProxy, 'GatePassManagement'),
                    simpleLookupRoute('vehicle-brands', 'VehicleBrands', 'VehicleBrands', VehicleBrandsServiceProxy, 'GatePassManagement'),
                    simpleLookupRoute('vehicle-models', 'VehicleModels', 'VehicleModels', VehicleModelsServiceProxy, 'GatePassManagement'),
                    {
                        path: 'gate-pass-vendors',
                        loadComponent: () =>
                            import('./admin/gate-pass-management/gate-pass-vendors/gate-pass-vendors.component').then(
                                (m) => m.GatePassVendorsComponent
                            ),
                        data: { permission: 'Pages.GatePassManagement.GatePassVendors' },
                    },
                    {
                        path: 'gate-passes',
                        loadComponent: () =>
                            import('./admin/gate-pass-management/gate-passes/gate-passes.component').then(
                                (m) => m.GatePassesComponent
                            ),
                        data: { permission: 'Pages.GatePassManagement.GatePasses' },
                    },
                    {
                        path: 'gate-passes/:id',
                        loadComponent: () =>
                            import('./admin/gate-pass-management/gate-pass-detail/gate-pass-detail.component').then(
                                (m) => m.GatePassDetailComponent
                            ),
                        data: { permission: 'Pages.GatePassManagement.GatePasses' },
                    },
                    {
                        path: 'pool-cars',
                        loadComponent: () =>
                            import('./admin/gate-pass-management/pool-cars/pool-cars.component').then(
                                (m) => m.PoolCarsComponent
                            ),
                        data: { permission: 'Pages.GatePassManagement.PoolCars' },
                    },
                    {
                        path: 'employee-pool-cars',
                        loadComponent: () =>
                            import('./admin/gate-pass-management/employee-pool-cars/employee-pool-cars.component').then(
                                (m) => m.EmployeePoolCarsComponent
                            ),
                        data: { permission: 'Pages.GatePassManagement.EmployeePoolCars' },
                    },
                    simpleLookupRoute('impact-of-losses', 'ImpactOfLosses', 'ImpactOfLosses', ImpactOfLossesServiceProxy, 'SuccessionManagement'),
                    simpleLookupRoute('nine-box-ratings', 'NineBoxRatings', 'NineBoxRatings', NineBoxRatingsServiceProxy, 'SuccessionManagement'),
                    simpleLookupRoute('rankings', 'Rankings', 'Rankings', RankingsServiceProxy, 'SuccessionManagement'),
                    simpleLookupRoute('readinesses', 'Readinesses', 'Readinesses', ReadinessesServiceProxy, 'SuccessionManagement'),
                    simpleLookupRoute('reason-for-leavings', 'ReasonForLeavings', 'ReasonForLeavings', ReasonForLeavingsServiceProxy, 'SuccessionManagement'),
                    simpleLookupRoute('risk-of-losses', 'RiskOfLosses', 'RiskOfLosses', RiskOfLossesServiceProxy, 'SuccessionManagement'),
                    simpleLookupRoute('succession-ratings', 'SuccessionRatings', 'SuccessionRatings', SuccessionRatingsServiceProxy, 'SuccessionManagement'),
                    simpleLookupRoute('talent-pools', 'TalentPools', 'TalentPools', TalentPoolsServiceProxy, 'SuccessionManagement'),
                    {
                        path: 'nine-box-mappings',
                        loadComponent: () =>
                            import('./admin/succession-management/nine-box-mappings/nine-box-mappings.component').then(
                                (m) => m.NineBoxMappingsComponent
                            ),
                        data: { permission: 'Pages.SuccessionManagement.NineBoxMappings' },
                    },
                    {
                        path: 'successor-nominees',
                        loadComponent: () =>
                            import('./admin/succession-management/successor-nominees/successor-nominees.component').then(
                                (m) => m.SuccessorNomineesComponent
                            ),
                        data: { permission: 'Pages.SuccessionManagement.SuccessorNominees' },
                    },
                    simpleLookupRoute('action-statuses', 'ActionStatuses', 'ActionStatuses', ActionStatusesServiceProxy, 'HealthAndSafetyManagement'),
                    simpleLookupRoute('first-aid-medicines', 'FirstAidMedicines', 'FirstAidMedicines', FirstAidMedicinesServiceProxy, 'HealthAndSafetyManagement'),
                    simpleLookupRoute('incident-claim-statuses', 'IncidentClaimStatuses', 'IncidentClaimStatuses', IncidentClaimStatusesServiceProxy, 'HealthAndSafetyManagement'),
                    simpleLookupRoute('incident-natures', 'IncidentNatures', 'IncidentNatures', IncidentNaturesServiceProxy, 'HealthAndSafetyManagement'),
                    simpleLookupRoute('incident-severities', 'IncidentSeverities', 'IncidentSeverities', IncidentSeveritiesServiceProxy, 'HealthAndSafetyManagement'),
                    simpleLookupRoute('incident-statuses', 'IncidentStatuses', 'IncidentStatuses', IncidentStatusesServiceProxy, 'HealthAndSafetyManagement'),
                    simpleLookupRoute('incident-types', 'IncidentTypes', 'IncidentTypes', IncidentTypesServiceProxy, 'HealthAndSafetyManagement'),
                    simpleLookupRoute('investigation-root-causes', 'InvestigationRootCauses', 'InvestigationRootCauses', InvestigationRootCausesServiceProxy, 'HealthAndSafetyManagement'),
                    simpleLookupRoute('risk-actions', 'RiskActions', 'RiskActions', RiskActionsServiceProxy, 'HealthAndSafetyManagement'),
                    simpleLookupRoute('risk-impacts', 'RiskImpacts', 'RiskImpacts', RiskImpactsServiceProxy, 'HealthAndSafetyManagement'),
                    simpleLookupRoute('risk-possibilities', 'RiskPossibilities', 'RiskPossibilities', RiskPossibilitiesServiceProxy, 'HealthAndSafetyManagement'),
                    simpleLookupRoute('risk-statuses', 'RiskStatuses', 'RiskStatuses', RiskStatusesServiceProxy, 'HealthAndSafetyManagement'),
                    simpleLookupRoute('risk-types', 'RiskTypes', 'RiskTypes', RiskTypesServiceProxy, 'HealthAndSafetyManagement'),
                    simpleLookupRoute('safety-area-assets', 'SafetyAreaAssets', 'SafetyAreaAssets', SafetyAreaAssetsServiceProxy, 'HealthAndSafetyManagement'),
                    {
                        path: 'action-registers',
                        loadComponent: () =>
                            import(
                                './admin/health-and-safety-management/action-registers/action-registers.component'
                            ).then((m) => m.ActionRegistersComponent),
                        data: { permission: 'Pages.HealthAndSafetyManagement.ActionRegisters' },
                    },
                    {
                        path: 'incident-claims',
                        loadComponent: () =>
                            import(
                                './admin/health-and-safety-management/incident-claims/incident-claims.component'
                            ).then((m) => m.IncidentClaimsComponent),
                        data: { permission: 'Pages.HealthAndSafetyManagement.IncidentClaims' },
                    },
                    {
                        path: 'incident-investigations',
                        loadComponent: () =>
                            import(
                                './admin/health-and-safety-management/incident-investigations/incident-investigations.component'
                            ).then((m) => m.IncidentInvestigationsComponent),
                        data: { permission: 'Pages.HealthAndSafetyManagement.IncidentInvestigations' },
                    },
                    {
                        path: 'incident-registers',
                        loadComponent: () =>
                            import(
                                './admin/health-and-safety-management/incident-registers/incident-registers.component'
                            ).then((m) => m.IncidentRegistersComponent),
                        data: { permission: 'Pages.HealthAndSafetyManagement.IncidentRegisters' },
                    },
                    {
                        path: 'risk-registers',
                        loadComponent: () =>
                            import(
                                './admin/health-and-safety-management/risk-registers/risk-registers.component'
                            ).then((m) => m.RiskRegistersComponent),
                        data: { permission: 'Pages.HealthAndSafetyManagement.RiskRegisters' },
                    },
                    {
                        path: 'safety-checklists',
                        loadComponent: () =>
                            import(
                                './admin/health-and-safety-management/safety-checklists/safety-checklists.component'
                            ).then((m) => m.SafetyChecklistsComponent),
                        data: { permission: 'Pages.HealthAndSafetyManagement.SafetyChecklists' },
                    },
                    {
                        path: 'first-aid-boxes',
                        loadComponent: () =>
                            import(
                                './admin/health-and-safety-management/first-aid-boxes/first-aid-boxes.component'
                            ).then((m) => m.FirstAidBoxesComponent),
                        data: { permission: 'Pages.HealthAndSafetyManagement.FirstAidBoxes' },
                    },
                    {
                        path: 'first-aid-boxes/:id',
                        loadComponent: () =>
                            import(
                                './admin/health-and-safety-management/first-aid-box-detail/first-aid-box-detail.component'
                            ).then((m) => m.FirstAidBoxDetailComponent),
                        data: { permission: 'Pages.HealthAndSafetyManagement.FirstAidBoxes' },
                    },
                    {
                        path: 'site-audits',
                        loadComponent: () =>
                            import(
                                './admin/health-and-safety-management/site-audits/site-audits.component'
                            ).then((m) => m.SiteAuditsComponent),
                        data: { permission: 'Pages.HealthAndSafetyManagement.SiteAudits' },
                    },
                    {
                        path: 'site-audits/:id',
                        loadComponent: () =>
                            import(
                                './admin/health-and-safety-management/site-audit-detail/site-audit-detail.component'
                            ).then((m) => m.SiteAuditDetailComponent),
                        data: { permission: 'Pages.HealthAndSafetyManagement.SiteAudits' },
                    },
                    { path: '', redirectTo: 'hostDashboard', pathMatch: 'full' },
                    { path: '**', redirectTo: 'hostDashboard' },
                ],
            },
            {
                path: '**',
                redirectTo: 'notifications',
            },
        ],
    },
];

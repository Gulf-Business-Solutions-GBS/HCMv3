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

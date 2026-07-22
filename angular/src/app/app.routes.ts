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
} from '@shared/service-proxies/service-proxies';

/** Builds a route entry for the shared SimpleLookupComponent (Code/NameEn/NameAr master data). */
function simpleLookupRoute(
    path: string,
    permissionSuffix: string,
    titleKey: string,
    serviceProxyType: Type<ISimpleLookupServiceProxy>
): Route {
    const base = `Pages.GeneralMasterInfo.${permissionSuffix}`;
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

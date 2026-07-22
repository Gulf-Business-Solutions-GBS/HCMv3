import { PermissionCheckerService } from 'abp-ng2-module';
import { AppSessionService } from '@shared/common/session/app-session.service';

import { Injectable, inject } from '@angular/core';
import { AppMenu } from './app-menu';
import { AppMenuItem } from './app-menu-item';

@Injectable({ providedIn: 'root' })
export class AppNavigationService {
    private _permissionCheckerService = inject(PermissionCheckerService);
    private _appSessionService = inject(AppSessionService);

    getMenu(): AppMenu {
        return new AppMenu('MainMenu', 'MainMenu', [
            new AppMenuItem(
                'Dashboard',
                'Pages.Administration.Host.Dashboard',
                'flaticon-line-graph',
                '/app/admin/hostDashboard'
            ),
            new AppMenuItem('Dashboard', 'Pages.Tenant.Dashboard', 'flaticon-line-graph', '/app/main/dashboard'),
            new AppMenuItem(
                'Saas',
                '',
                'flaticon-users',
                '',
                [],
                [
                    new AppMenuItem('Tenants', 'Pages.Tenants', 'flaticon-list-3', '/app/admin/tenants'),
                    new AppMenuItem('Editions', 'Pages.Editions', 'flaticon-app', '/app/admin/editions'),
                ]
            ),
            new AppMenuItem(
                'GeneralMasterInfo',
                '',
                'flaticon-list-3',
                '',
                [],
                [
                    new AppMenuItem(
                        'AccessUnits',
                        'Pages.GeneralMasterInfo.AccessUnits',
                        'flaticon-map-location',
                        '/app/admin/access-units'
                    ),
                    new AppMenuItem('AssetTypes', 'Pages.GeneralMasterInfo.AssetTypes', 'flaticon-list-3', '/app/admin/asset-types'),
                    new AppMenuItem('Cities', 'Pages.GeneralMasterInfo.Cities', 'flaticon-list-3', '/app/admin/cities'),
                    new AppMenuItem('Institutions', 'Pages.GeneralMasterInfo.Institutions', 'flaticon-list-3', '/app/admin/institutions'),
                    new AppMenuItem('Nationalities', 'Pages.GeneralMasterInfo.Nationalities', 'flaticon-list-3', '/app/admin/nationalities'),
                    new AppMenuItem('DependantTypes', 'Pages.GeneralMasterInfo.DependantTypes', 'flaticon-list-3', '/app/admin/dependant-types'),
                    new AppMenuItem('Currencies', 'Pages.GeneralMasterInfo.Currencies', 'flaticon-list-3', '/app/admin/currencies'),
                    new AppMenuItem('SeparationReasons', 'Pages.GeneralMasterInfo.SeparationReasons', 'flaticon-list-3', '/app/admin/separation-reasons'),
                    new AppMenuItem('DisciplinaryDeductionTypes', 'Pages.GeneralMasterInfo.DisciplinaryDeductionTypes', 'flaticon-list-3', '/app/admin/disciplinary-deduction-types'),
                    new AppMenuItem('PersonalActions', 'Pages.GeneralMasterInfo.PersonalActions', 'flaticon-list-3', '/app/admin/personal-actions'),
                    new AppMenuItem('PersonalActionReasons', 'Pages.GeneralMasterInfo.PersonalActionReasons', 'flaticon-list-3', '/app/admin/personal-action-reasons'),
                    new AppMenuItem('Countries', 'Pages.GeneralMasterInfo.Countries', 'flaticon-map-location', '/app/admin/countries'),
                    new AppMenuItem('BankFiles', 'Pages.GeneralMasterInfo.BankFiles', 'flaticon-list-3', '/app/admin/bank-files'),
                    new AppMenuItem('BenefitTypes', 'Pages.GeneralMasterInfo.BenefitTypes', 'flaticon-list-3', '/app/admin/benefit-types'),
                    new AppMenuItem('ContractTypes', 'Pages.GeneralMasterInfo.ContractTypes', 'flaticon-list-3', '/app/admin/contract-types'),
                    new AppMenuItem('DateTypes', 'Pages.GeneralMasterInfo.DateTypes', 'flaticon-list-3', '/app/admin/date-types'),
                    new AppMenuItem('DocumentPlaces', 'Pages.GeneralMasterInfo.DocumentPlaces', 'flaticon-list-3', '/app/admin/document-places'),
                    new AppMenuItem('Educations', 'Pages.GeneralMasterInfo.Educations', 'flaticon-list-3', '/app/admin/educations'),
                    new AppMenuItem('EducationBranches', 'Pages.GeneralMasterInfo.EducationBranches', 'flaticon-list-3', '/app/admin/education-branches'),
                    new AppMenuItem('Governorates', 'Pages.GeneralMasterInfo.Governorates', 'flaticon-list-3', '/app/admin/governorates'),
                    new AppMenuItem('GrievanceTypes', 'Pages.GeneralMasterInfo.GrievanceTypes', 'flaticon-list-3', '/app/admin/grievance-types'),
                    new AppMenuItem('HRServiceTypes', 'Pages.GeneralMasterInfo.HRServiceTypes', 'flaticon-list-3', '/app/admin/hr-service-types'),
                    new AppMenuItem('ITServiceTypes', 'Pages.GeneralMasterInfo.ITServiceTypes', 'flaticon-list-3', '/app/admin/it-service-types'),
                    new AppMenuItem('LanguageInfos', 'Pages.GeneralMasterInfo.LanguageInfos', 'flaticon-list-3', '/app/admin/language-infos'),
                    new AppMenuItem('MaritalStatuses', 'Pages.GeneralMasterInfo.MaritalStatuses', 'flaticon-list-3', '/app/admin/marital-statuses'),
                    new AppMenuItem('NationalityGroups', 'Pages.GeneralMasterInfo.NationalityGroups', 'flaticon-list-3', '/app/admin/nationality-groups'),
                    new AppMenuItem('NewsTypes', 'Pages.GeneralMasterInfo.NewsTypes', 'flaticon-list-3', '/app/admin/news-types'),
                    new AppMenuItem('PassportTypes', 'Pages.GeneralMasterInfo.PassportTypes', 'flaticon-list-3', '/app/admin/passport-types'),
                    new AppMenuItem('PostingGroups', 'Pages.GeneralMasterInfo.PostingGroups', 'flaticon-list-3', '/app/admin/posting-groups'),
                    new AppMenuItem('Qualifications', 'Pages.GeneralMasterInfo.Qualifications', 'flaticon-list-3', '/app/admin/qualifications'),
                    new AppMenuItem('RecruitmentTypes', 'Pages.GeneralMasterInfo.RecruitmentTypes', 'flaticon-list-3', '/app/admin/recruitment-types'),
                    new AppMenuItem('RelationshipTypes', 'Pages.GeneralMasterInfo.RelationshipTypes', 'flaticon-list-3', '/app/admin/relationship-types'),
                    new AppMenuItem('Religions', 'Pages.GeneralMasterInfo.Religions', 'flaticon-list-3', '/app/admin/religions'),
                    new AppMenuItem('States', 'Pages.GeneralMasterInfo.States', 'flaticon-list-3', '/app/admin/states'),
                    new AppMenuItem('TaskTypes', 'Pages.GeneralMasterInfo.TaskTypes', 'flaticon-list-3', '/app/admin/task-types'),
                    new AppMenuItem('Titles', 'Pages.GeneralMasterInfo.Titles', 'flaticon-list-3', '/app/admin/titles'),
                    new AppMenuItem('TrainingTypes', 'Pages.GeneralMasterInfo.TrainingTypes', 'flaticon-list-3', '/app/admin/training-types'),
                    new AppMenuItem('Universities', 'Pages.GeneralMasterInfo.Universities', 'flaticon-list-3', '/app/admin/universities'),
                    new AppMenuItem('VisaTypes', 'Pages.GeneralMasterInfo.VisaTypes', 'flaticon-list-3', '/app/admin/visa-types'),
                    new AppMenuItem('WorkVisaTypes', 'Pages.GeneralMasterInfo.WorkVisaTypes', 'flaticon-list-3', '/app/admin/work-visa-types'),
                    new AppMenuItem('BudgetingGroups', 'Pages.GeneralMasterInfo.BudgetingGroups', 'flaticon-list-3', '/app/admin/budgeting-groups'),
                    new AppMenuItem('ClearanceRequestTypes', 'Pages.GeneralMasterInfo.ClearanceRequestTypes', 'flaticon-list-3', '/app/admin/clearance-request-types'),
                    new AppMenuItem('Banks', 'Pages.GeneralMasterInfo.Banks', 'flaticon-list-3', '/app/admin/banks'),
                    new AppMenuItem('PayeeBankDetails', 'Pages.GeneralMasterInfo.PayeeBankDetails', 'flaticon-list-3', '/app/admin/payee-bank-details'),
                    new AppMenuItem('BudgetingGroupRateDetails', 'Pages.GeneralMasterInfo.BudgetingGroupRateDetails', 'flaticon-list-3', '/app/admin/budgeting-group-rate-details'),
                    new AppMenuItem('ClearanceRequestFields', 'Pages.GeneralMasterInfo.ClearanceRequestFields', 'flaticon-list-3', '/app/admin/clearance-request-fields'),
                    new AppMenuItem('ClearanceRequestFieldDetails', 'Pages.GeneralMasterInfo.ClearanceRequestFieldDetails', 'flaticon-list-3', '/app/admin/clearance-request-field-details'),
                    new AppMenuItem('DisciplinaryTypes', 'Pages.GeneralMasterInfo.DisciplinaryTypes', 'flaticon-list-3', '/app/admin/disciplinary-types'),
                    new AppMenuItem('DisciplinaryTypeDetails', 'Pages.GeneralMasterInfo.DisciplinaryTypeDetails', 'flaticon-list-3', '/app/admin/disciplinary-type-details'),
                    new AppMenuItem('NotificationConfigs', 'Pages.GeneralMasterInfo.NotificationConfigs', 'flaticon-list-3', '/app/admin/notification-configs'),
                    new AppMenuItem('NotificationGroups', 'Pages.GeneralMasterInfo.NotificationGroups', 'flaticon-list-3', '/app/admin/notification-groups'),
                    new AppMenuItem('DocumentTypes', 'Pages.GeneralMasterInfo.DocumentTypes', 'flaticon-list-3', '/app/admin/document-types'),
                    new AppMenuItem('DocumentPrintForms', 'Pages.GeneralMasterInfo.DocumentPrintForms', 'flaticon-list-3', '/app/admin/document-print-forms'),
                    new AppMenuItem('LoanTypes', 'Pages.GeneralMasterInfo.LoanTypes', 'flaticon-list-3', '/app/admin/loan-types'),
                    new AppMenuItem('LoanTypeDetails', 'Pages.GeneralMasterInfo.LoanTypeDetails', 'flaticon-list-3', '/app/admin/loan-type-details'),
                    new AppMenuItem('OvertimeTypes', 'Pages.GeneralMasterInfo.OvertimeTypes', 'flaticon-list-3', '/app/admin/overtime-types'),
                    new AppMenuItem('OvertimeTypeDetails', 'Pages.GeneralMasterInfo.OvertimeTypeDetails', 'flaticon-list-3', '/app/admin/overtime-type-details'),
                    new AppMenuItem('Assets', 'Pages.GeneralMasterInfo.Assets', 'flaticon-list-3', '/app/admin/assets'),
                    new AppMenuItem('NumberSeries', 'Pages.GeneralMasterInfo.NumberSeries', 'flaticon-list-3', '/app/admin/number-series'),
                    new AppMenuItem('CurrencyConvertors', 'Pages.GeneralMasterInfo.CurrencyConvertors', 'flaticon-list-3', '/app/admin/currency-convertors'),
                ]
            ),
            new AppMenuItem(
                'OrganizationManagement',
                '',
                'flaticon-list-3',
                '',
                [],
                [
                    new AppMenuItem('CostCenterGroups', 'Pages.OrganizationManagement.CostCenterGroups', 'flaticon-list-3', '/app/admin/cost-center-groups'),
                    new AppMenuItem('CostElementTypes', 'Pages.OrganizationManagement.CostElementTypes', 'flaticon-list-3', '/app/admin/cost-element-types'),
                    new AppMenuItem('JobTitles', 'Pages.OrganizationManagement.JobTitles', 'flaticon-list-3', '/app/admin/job-titles'),
                    new AppMenuItem('CostCenters', 'Pages.OrganizationManagement.CostCenters', 'flaticon-list-3', '/app/admin/cost-centers'),
                    new AppMenuItem('CostElements', 'Pages.OrganizationManagement.CostElements', 'flaticon-list-3', '/app/admin/cost-elements'),
                    new AppMenuItem('OrganizationUnitTypes', 'Pages.OrganizationManagement.OrganizationUnitTypes', 'flaticon-list-3', '/app/admin/organization-unit-types'),
                    new AppMenuItem('WorkLocations', 'Pages.OrganizationManagement.WorkLocations', 'flaticon-list-3', '/app/admin/work-locations'),
                    new AppMenuItem('OrganizationBankInfo', 'Pages.OrganizationManagement.OrganizationBankInfos', 'flaticon-list-3', '/app/admin/organization-bank-infos'),
                    new AppMenuItem('OrgUnits', 'Pages.OrganizationManagement.OrgUnits', 'flaticon-list-3', '/app/admin/org-units'),
                ]
            ),
            new AppMenuItem(
                'Administration',
                '',
                'flaticon-interface-8',
                '',
                [],
                [
                    new AppMenuItem(
                        'OrganizationUnits',
                        'Pages.Administration.OrganizationUnits',
                        'flaticon-map',
                        '/app/admin/organization-units'
                    ),
                    new AppMenuItem('Roles', 'Pages.Administration.Roles', 'flaticon-suitcase', '/app/admin/roles'),
                    new AppMenuItem('Users', 'Pages.Administration.Users', 'flaticon-users', '/app/admin/users'),
                    new AppMenuItem(
                        'Languages',
                        'Pages.Administration.Languages',
                        'flaticon-tabs',
                        '/app/admin/languages',
                        ['/app/admin/languages/{name}/texts']
                    ),
                    new AppMenuItem(
                        'AuditLogs',
                        'Pages.Administration.AuditLogs',
                        'flaticon-folder-1',
                        '/app/admin/auditLogs'
                    ),
                    new AppMenuItem(
                        'Maintenance',
                        'Pages.Administration.Host.Maintenance',
                        'flaticon-lock',
                        '/app/admin/maintenance'
                    ),
                    new AppMenuItem(
                        'HealthCheck',
                        'Pages.Administration.Host.HealthCheck',
                        'flaticon2-cardiogram',
                        '/app/admin/health-check-ui'
                    ),
                    new AppMenuItem(
                        'Subscription',
                        'Pages.Administration.Tenant.SubscriptionManagement',
                        'flaticon-refresh',
                        '/app/admin/subscription-management'
                    ),
                    new AppMenuItem(
                        'VisualSettings',
                        'Pages.Administration.UiCustomization',
                        'flaticon-medical',
                        '/app/admin/ui-customization'
                    ),
                    new AppMenuItem(
                        'WebhookSubscriptions',
                        'Pages.Administration.WebhookSubscription',
                        'flaticon2-world',
                        '/app/admin/webhook-subscriptions'
                    ),
                    new AppMenuItem(
                        'DynamicProperties',
                        'Pages.Administration.DynamicProperties',
                        'flaticon-interface-8',
                        '/app/admin/dynamic-property'
                    ),
                    new AppMenuItem(
                        'Notifications',
                        '',
                        'flaticon-alarm',
                        '',
                        [],
                        [
                            new AppMenuItem('Inbox', '', 'flaticon-mail-1', '/app/notifications'),
                            new AppMenuItem(
                                'MassNotifications',
                                'Pages.Administration.MassNotification',
                                'flaticon-paper-plane',
                                '/app/admin/mass-notifications'
                            ),
                        ]
                    ),
                    new AppMenuItem(
                        'Settings',
                        'Pages.Administration.Host.Settings',
                        'flaticon-settings',
                        '/app/admin/hostSettings'
                    ),
                    new AppMenuItem(
                        'Settings',
                        'Pages.Administration.Tenant.Settings',
                        'flaticon-settings',
                        '/app/admin/tenantSettings'
                    ),
                    new AppMenuItem(
                        'ActiveSessions',
                        'Pages.Administration.ActiveSessions',
                        'flaticon-security',
                        '/app/admin/active-sessions',
                        [],
                        [],
                        false,
                        {},
                        () => this._appSessionService.isSessionManagementEnabled
                    ),
                ]
            ),
            new AppMenuItem(
                'DemoUiComponents',
                'Pages.DemoUiComponents',
                'flaticon-shapes',
                '/app/admin/demo-ui-components'
            ),
        ]);
    }

    checkChildMenuItemPermission(menuItem): boolean {
        for (let i = 0; i < menuItem.items.length; i++) {
            let subMenuItem = menuItem.items[i];

            if (subMenuItem.permissionName === '' || subMenuItem.permissionName === null) {
                if (subMenuItem.route) {
                    return true;
                }
            } else if (this._permissionCheckerService.isGranted(subMenuItem.permissionName)) {
                if (!subMenuItem.hasFeatureDependency()) {
                    return true;
                }

                if (subMenuItem.featureDependencySatisfied()) {
                    return true;
                }
            }

            if (subMenuItem.items && subMenuItem.items.length) {
                let isAnyChildItemActive = this.checkChildMenuItemPermission(subMenuItem);
                if (isAnyChildItemActive) {
                    return true;
                }
            }
        }

        return false;
    }

    showMenuItem(menuItem: AppMenuItem): boolean {
        if (
            menuItem.permissionName === 'Pages.Administration.Tenant.SubscriptionManagement' &&
            this._appSessionService.tenant &&
            !this._appSessionService.tenant.edition
        ) {
            return false;
        }

        let hideMenuItem = false;

        if (menuItem.requiresAuthentication && !this._appSessionService.user) {
            hideMenuItem = true;
        }

        if (menuItem.permissionName && !this._permissionCheckerService.isGranted(menuItem.permissionName)) {
            hideMenuItem = true;
        }

        if (this._appSessionService.tenant || !abp.multiTenancy.ignoreFeatureCheckForHostUsers) {
            if (menuItem.hasFeatureDependency() && !menuItem.featureDependencySatisfied()) {
                hideMenuItem = true;
            }
        }

        if (!hideMenuItem && menuItem.items && menuItem.items.length) {
            return this.checkChildMenuItemPermission(menuItem);
        }

        return !hideMenuItem;
    }

    /**
     * Returns all menu items recursively
     */
    getAllMenuItems(): AppMenuItem[] {
        let menu = this.getMenu();
        let allMenuItems: AppMenuItem[] = [];
        menu.items.forEach((menuItem) => {
            allMenuItems = allMenuItems.concat(this.getAllMenuItemsRecursive(menuItem));
        });

        return allMenuItems;
    }

    private getAllMenuItemsRecursive(menuItem: AppMenuItem): AppMenuItem[] {
        if (!menuItem.items) {
            return [menuItem];
        }

        let menuItems = [menuItem];
        menuItem.items.forEach((subMenu) => {
            menuItems = menuItems.concat(this.getAllMenuItemsRecursive(subMenu));
        });

        return menuItems;
    }
}

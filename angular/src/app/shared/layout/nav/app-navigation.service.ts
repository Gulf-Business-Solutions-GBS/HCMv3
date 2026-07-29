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
            new AppMenuItem('ReportPreview', 'Pages.Reports.Preview', 'flaticon-file-2', '/app/admin/report-preview'),
            new AppMenuItem('PayslipReport', 'Pages.Reports.Payslip.Email', 'flaticon-envelope', '/app/admin/payslip-report'),
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
                'TimeManagement',
                '',
                'flaticon-time',
                '',
                [],
                [
                    new AppMenuItem('TimeTypes', 'Pages.TimeManagement.TimeTypes', 'flaticon-list-3', '/app/admin/time-types'),
                    new AppMenuItem('TransTypes', 'Pages.TimeManagement.TransTypes', 'flaticon-list-3', '/app/admin/trans-types'),
                    new AppMenuItem('TimeTransferTypes', 'Pages.TimeManagement.TimeTransferTypes', 'flaticon-list-3', '/app/admin/time-transfer-types'),
                    new AppMenuItem('BreakShifts', 'Pages.TimeManagement.BreakShifts', 'flaticon-list-3', '/app/admin/break-shifts'),
                    new AppMenuItem('Duties', 'Pages.TimeManagement.Duties', 'flaticon-list-3', '/app/admin/duties'),
                    new AppMenuItem('DailyDuties', 'Pages.TimeManagement.DailyDuties', 'flaticon-list-3', '/app/admin/daily-duties'),
                    new AppMenuItem('HolidayCalendars', 'Pages.TimeManagement.HolidayCalendars', 'flaticon-list-3', '/app/admin/holiday-calendars'),
                    new AppMenuItem('Holidays', 'Pages.TimeManagement.Holidays', 'flaticon-list-3', '/app/admin/holidays'),
                    new AppMenuItem('HolidayPeriods', 'Pages.TimeManagement.HolidayPeriods', 'flaticon-list-3', '/app/admin/holiday-periods'),
                    new AppMenuItem('TimePolicies', 'Pages.TimeManagement.TimePolicies', 'flaticon-list-3', '/app/admin/time-policies'),
                    new AppMenuItem('DutyPeriods', 'Pages.TimeManagement.DutyPeriods', 'flaticon-list-3', '/app/admin/duty-periods'),
                    new AppMenuItem('DutyPeriodDetails', 'Pages.TimeManagement.DutyPeriodDetails', 'flaticon-list-3', '/app/admin/duty-period-details'),
                    new AppMenuItem('RosterDutyPeriods', 'Pages.TimeManagement.RosterDutyPeriods', 'flaticon-list-3', '/app/admin/roster-duty-periods'),
                    new AppMenuItem('RosterDutyPeriodDetails', 'Pages.TimeManagement.RosterDutyPeriodDetails', 'flaticon-list-3', '/app/admin/roster-duty-period-details'),
                    new AppMenuItem('RosterDutySchedules', 'Pages.TimeManagement.RosterDutySchedules', 'flaticon-list-3', '/app/admin/roster-duty-schedules'),
                    new AppMenuItem('RosterGroups', 'Pages.TimeManagement.RosterGroups', 'flaticon-list-3', '/app/admin/roster-groups'),
                    new AppMenuItem('RosterGroupDetails', 'Pages.TimeManagement.RosterGroupDetails', 'flaticon-list-3', '/app/admin/roster-group-details'),
                    new AppMenuItem('RosterGroupSchedules', 'Pages.TimeManagement.RosterGroupSchedules', 'flaticon-list-3', '/app/admin/roster-group-schedules'),
                    new AppMenuItem('RosterDailyDuties', 'Pages.TimeManagement.RosterDailyDuties', 'flaticon-list-3', '/app/admin/roster-daily-duties'),
                    new AppMenuItem('RosterScheduleViewer', 'Pages.TimeManagement.RosterScheduleViewer', 'flaticon-calendar-3', '/app/admin/roster-schedule-viewer'),
                    new AppMenuItem('WorkLocationDutyPeriods', 'Pages.TimeManagement.WorkLocationDutyPeriods', 'flaticon-list-3', '/app/admin/work-location-duty-periods'),
                    new AppMenuItem('TimeEvents', 'Pages.TimeManagement.TimeEvents', 'flaticon-list-3', '/app/admin/time-events'),
                    new AppMenuItem('TimeAttAPIs', 'Pages.TimeManagement.TimeAttAPIs', 'flaticon-list-3', '/app/admin/time-att-apis'),
                    new AppMenuItem('TimeLogs', 'Pages.TimeManagement.TimeLogs', 'flaticon-list-3', '/app/admin/time-logs'),
                    new AppMenuItem('TimeRegisters', 'Pages.TimeManagement.TimeRegisters', 'flaticon-list-3', '/app/admin/time-registers'),
                    new AppMenuItem('MonthlyTimeRegisters', 'Pages.TimeManagement.MonthlyTimeRegisters', 'flaticon-list-3', '/app/admin/monthly-time-registers'),
                    new AppMenuItem('MonthlyTimeRegisterDetails', 'Pages.TimeManagement.MonthlyTimeRegisterDetails', 'flaticon-list-3', '/app/admin/monthly-time-register-details'),
                    new AppMenuItem('YearlyTimeRegisters', 'Pages.TimeManagement.YearlyTimeRegisters', 'flaticon-list-3', '/app/admin/yearly-time-registers'),
                    new AppMenuItem('YearlyTimeRegisterDetails', 'Pages.TimeManagement.YearlyTimeRegisterDetails', 'flaticon-list-3', '/app/admin/yearly-time-register-details'),
                    new AppMenuItem('SimulationTimeRegisters', 'Pages.TimeManagement.SimulationTimeRegisters', 'flaticon-list-3', '/app/admin/simulation-time-registers'),
                    new AppMenuItem('SimulationTimeRegisterDetails', 'Pages.TimeManagement.SimulationTimeRegisterDetails', 'flaticon-list-3', '/app/admin/simulation-time-register-details'),
                    new AppMenuItem('TimeTransfers', 'Pages.TimeManagement.TimeTransfers', 'flaticon-list-3', '/app/admin/time-transfers'),
                    new AppMenuItem('TimeEngine', 'Pages.TimeManagement.TimeEngine', 'flaticon-settings', '/app/admin/time-engine'),
                ]
            ),
            new AppMenuItem(
                'PayrollManagement',
                '',
                'flaticon-coins',
                '',
                [],
                [
                    new AppMenuItem('PayrollEngine', 'Pages.PayrollManagement.PayrollEngine', 'flaticon-settings', '/app/admin/payroll-engine'),
                    new AppMenuItem('AttendanceTypes', 'Pages.PayrollManagement.AttendanceTypes', 'flaticon-list-3', '/app/admin/attendance-types'),
                    new AppMenuItem('AccountTypes', 'Pages.PayrollManagement.AccountTypes', 'flaticon-list-3', '/app/admin/account-types'),
                    new AppMenuItem('AccountInfos', 'Pages.PayrollManagement.AccountInfos', 'flaticon-list-3', '/app/admin/account-infos'),
                    new AppMenuItem('PayrollReasonTypes', 'Pages.PayrollManagement.PayrollReasonTypes', 'flaticon-list-3', '/app/admin/payroll-reason-types'),
                    new AppMenuItem('TicketClasses', 'Pages.PayrollManagement.TicketClasses', 'flaticon-list-3', '/app/admin/ticket-classes'),
                    new AppMenuItem('TicketPaxTypes', 'Pages.PayrollManagement.TicketPaxTypes', 'flaticon-list-3', '/app/admin/ticket-pax-types'),
                    new AppMenuItem('TicketRoutes', 'Pages.PayrollManagement.TicketRoutes', 'flaticon-list-3', '/app/admin/ticket-routes'),
                    new AppMenuItem('AbsenceTypes', 'Pages.PayrollManagement.AbsenceTypes', 'flaticon-list-3', '/app/admin/absence-types'),
                    new AppMenuItem('AbsenceTypeExceptions', 'Pages.PayrollManagement.AbsenceTypeExceptions', 'flaticon-list-3', '/app/admin/absence-type-exceptions'),
                    new AppMenuItem('AbsenceDeductionRules', 'Pages.PayrollManagement.AbsenceDeductionRules', 'flaticon-list-3', '/app/admin/absence-deduction-rules'),
                    new AppMenuItem('AbsenceDedRuleDetails', 'Pages.PayrollManagement.AbsenceDedRuleDetails', 'flaticon-list-3', '/app/admin/absence-ded-rule-details'),
                    new AppMenuItem('AbsenceViolationRules', 'Pages.PayrollManagement.AbsenceViolationRules', 'flaticon-list-3', '/app/admin/absence-violation-rules'),
                    new AppMenuItem('AbsenceViolationRuleDetails', 'Pages.PayrollManagement.AbsenceViolationRuleDetails', 'flaticon-list-3', '/app/admin/absence-violation-rule-details'),
                    new AppMenuItem('AbsenceViolationRuleExceptions', 'Pages.PayrollManagement.AbsenceViolationRuleExceptions', 'flaticon-list-3', '/app/admin/absence-violation-rule-exceptions'),
                    new AppMenuItem('AbsenceViolationActionRecords', 'Pages.PayrollManagement.AbsenceViolationActionRecords', 'flaticon-list-3', '/app/admin/absence-violation-action-records'),
                    new AppMenuItem('AbsenceViolationActionLogs', 'Pages.PayrollManagement.AbsenceViolationActionLogs', 'flaticon-list-3', '/app/admin/absence-violation-action-logs'),
                    new AppMenuItem('AbsencePayments', 'Pages.PayrollManagement.AbsencePayments', 'flaticon-list-3', '/app/admin/absence-payments'),
                    new AppMenuItem('WageTypeExceptions', 'Pages.PayrollManagement.WageTypeExceptions', 'flaticon-list-3', '/app/admin/wage-type-exceptions'),
                    new AppMenuItem('EmployeeGroups', 'Pages.PayrollManagement.EmployeeGroups', 'flaticon-list-3', '/app/admin/employee-groups'),
                    new AppMenuItem('EmployeeSubGroups', 'Pages.PayrollManagement.EmployeeSubGroups', 'flaticon-list-3', '/app/admin/employee-sub-groups'),
                    new AppMenuItem('PayrollAreas', 'Pages.PayrollManagement.PayrollAreas', 'flaticon-list-3', '/app/admin/payroll-areas'),
                    new AppMenuItem('CostingFile', 'Pages.PayrollManagement.CostingFile', 'flaticon-line-graph', '/app/admin/costing-file'),
                    new AppMenuItem('PostJv', 'Pages.PayrollManagement.PostJv', 'flaticon-piggy-bank', '/app/admin/post-jv'),
                    // Legacy HCM10.Web/UserControls/ImportExport/ -- the screen is cross-module (it can
                    // import ANY entity), but it is placed here so it sits next to the other operator
                    // "run something" screens rather than needing a new top-level group.
                    new AppMenuItem('BulkImportExport', 'Pages.ImportExport.BulkImportExport', 'flaticon-download', '/app/admin/bulk-import-export'),
                    new AppMenuItem('BankFile', 'Pages.PayrollManagement.BankFiles', 'flaticon-piggy-bank', '/app/admin/bank-file'),
                    new AppMenuItem('PayrollCalendars', 'Pages.PayrollManagement.PayrollCalendars', 'flaticon-list-3', '/app/admin/payroll-calendars'),
                    new AppMenuItem('PayrollPeriods', 'Pages.PayrollManagement.PayrollPeriods', 'flaticon-list-3', '/app/admin/payroll-periods'),
                    new AppMenuItem('PayrollControlRecords', 'Pages.PayrollManagement.PayrollControlRecords', 'flaticon-list-3', '/app/admin/payroll-control-records'),
                    new AppMenuItem('WageTypes', 'Pages.PayrollManagement.WageTypes', 'flaticon-list-3', '/app/admin/wage-types'),
                    new AppMenuItem('WageAccGroups', 'Pages.PayrollManagement.WageAccGroups', 'flaticon-list-3', '/app/admin/wage-acc-groups'),
                    new AppMenuItem('WageAccGroupDetails', 'Pages.PayrollManagement.WageAccGroupDetails', 'flaticon-list-3', '/app/admin/wage-acc-group-details'),
                    new AppMenuItem('WageAccounts', 'Pages.PayrollManagement.WageAccounts', 'flaticon-list-3', '/app/admin/wage-accounts'),
                    new AppMenuItem('WageTypeLeaves', 'Pages.PayrollManagement.WageTypeLeaves', 'flaticon-list-3', '/app/admin/wage-type-leaves'),
                    new AppMenuItem('WageTypeSecurityRoles', 'Pages.PayrollManagement.WageTypeSecurityRoles', 'flaticon-list-3', '/app/admin/wage-type-security-roles'),
                    new AppMenuItem('WageTypeRegisters', 'Pages.PayrollManagement.WageTypeRegisters', 'flaticon-list-3', '/app/admin/wage-type-registers'),
                    new AppMenuItem('IndemnityPayRules', 'Pages.PayrollManagement.IndemnityPayRules', 'flaticon-list-3', '/app/admin/indemnity-pay-rules'),
                    new AppMenuItem('IndemnityPayRuleDetails', 'Pages.PayrollManagement.IndemnityPayRuleDetails', 'flaticon-list-3', '/app/admin/indemnity-pay-rule-details'),
                    new AppMenuItem('OrgUnitWageAccGroups', 'Pages.PayrollManagement.OrgUnitWageAccGroups', 'flaticon-list-3', '/app/admin/org-unit-wage-acc-groups'),
                    new AppMenuItem('SocialSecurities', 'Pages.PayrollManagement.SocialSecurities', 'flaticon-list-3', '/app/admin/social-securities'),
                    new AppMenuItem('SocialSecurityDetails', 'Pages.PayrollManagement.SocialSecurityDetails', 'flaticon-list-3', '/app/admin/social-security-details'),
                    new AppMenuItem('SocialSecurityMaritalAllowances', 'Pages.PayrollManagement.SocialSecurityMaritalAllowances', 'flaticon-list-3', '/app/admin/social-security-marital-allowances'),
                    new AppMenuItem('SocialSecurityEducationAllowances', 'Pages.PayrollManagement.SocialSecurityEducationAllowances', 'flaticon-list-3', '/app/admin/social-security-education-allowances'),
                    new AppMenuItem('SocialSecurityRules', 'Pages.PayrollManagement.SocialSecurityRules', 'flaticon-list-3', '/app/admin/social-security-rules'),
                    new AppMenuItem('TicketTypes', 'Pages.PayrollManagement.TicketTypes', 'flaticon-list-3', '/app/admin/ticket-types'),
                    new AppMenuItem('TicketFares', 'Pages.PayrollManagement.TicketFares', 'flaticon-list-3', '/app/admin/ticket-fares'),
                    new AppMenuItem('TicketAccrualSummaries', 'Pages.PayrollManagement.TicketAccrualSummaries', 'flaticon-list-3', '/app/admin/ticket-accrual-summaries'),
                    new AppMenuItem('TempBenefitTypes', 'Pages.PayrollManagement.TempBenefitTypes', 'flaticon-list-3', '/app/admin/temp-benefit-types'),
                    new AppMenuItem('TempEmployeeBenefits', 'Pages.PayrollManagement.TempEmployeeBenefits', 'flaticon-list-3', '/app/admin/temp-employee-benefits'),
                    new AppMenuItem('PayrollLogs', 'Pages.PayrollManagement.PayrollLogs', 'flaticon-list-3', '/app/admin/payroll-logs'),
                    new AppMenuItem('CostRegisters', 'Pages.PayrollManagement.CostRegisters', 'flaticon-list-3', '/app/admin/cost-registers'),
                    new AppMenuItem('CostRegisterDetails', 'Pages.PayrollManagement.CostRegisterDetails', 'flaticon-list-3', '/app/admin/cost-register-details'),
                    new AppMenuItem('SalaryRegisters', 'Pages.PayrollManagement.SalaryRegisters', 'flaticon-list-3', '/app/admin/salary-registers'),
                    new AppMenuItem('SalaryRegisterDetails', 'Pages.PayrollManagement.SalaryRegisterDetails', 'flaticon-list-3', '/app/admin/salary-register-details'),
                    new AppMenuItem('SalaryRegisterDaysDetails', 'Pages.PayrollManagement.SalaryRegisterDaysDetails', 'flaticon-list-3', '/app/admin/salary-register-days-details'),
                    new AppMenuItem('SimulationSalaryRegisters', 'Pages.PayrollManagement.SimulationSalaryRegisters', 'flaticon-list-3', '/app/admin/simulation-salary-registers'),
                    new AppMenuItem('SimulationSalaryRegisterDetails', 'Pages.PayrollManagement.SimulationSalaryRegisterDetails', 'flaticon-list-3', '/app/admin/simulation-salary-register-details'),
                    new AppMenuItem('SimulationSalaryRegisterDaysDetails', 'Pages.PayrollManagement.SimulationSalaryRegisterDaysDetails', 'flaticon-list-3', '/app/admin/simulation-salary-register-days-details'),
                    new AppMenuItem('GratuityAccrualSummaries', 'Pages.PayrollManagement.GratuityAccrualSummaries', 'flaticon-list-3', '/app/admin/gratuity-accrual-summaries'),
                    new AppMenuItem('LeaveAccrualSummaries', 'Pages.PayrollManagement.LeaveAccrualSummaries', 'flaticon-list-3', '/app/admin/leave-accrual-summaries'),
                ]
            ),
            new AppMenuItem(
                'JobProfileManagement',
                '',
                'flaticon-suitcase',
                '',
                [],
                [
                    new AppMenuItem('JobFamilies', 'Pages.JobProfileManagement.JobFamilies', 'flaticon-list-3', '/app/admin/job-families'),
                    new AppMenuItem('JobFunctions', 'Pages.JobProfileManagement.JobFunctions', 'flaticon-list-3', '/app/admin/job-functions'),
                    new AppMenuItem('JobResponsibilities', 'Pages.JobProfileManagement.JobResponsibilities', 'flaticon-list-3', '/app/admin/job-responsibilities'),
                    new AppMenuItem('EducationCertificates', 'Pages.JobProfileManagement.EducationCertificates', 'flaticon-list-3', '/app/admin/education-certificates'),
                    new AppMenuItem('EducationDegrees', 'Pages.JobProfileManagement.EducationDegrees', 'flaticon-list-3', '/app/admin/education-degrees'),
                    new AppMenuItem('InterviewQuestions', 'Pages.JobProfileManagement.InterviewQuestions', 'flaticon-list-3', '/app/admin/interview-questions'),
                    new AppMenuItem('Skills', 'Pages.JobProfileManagement.Skills', 'flaticon-list-3', '/app/admin/skills'),
                    new AppMenuItem('JobDescriptions', 'Pages.JobProfileManagement.JobDescriptions', 'flaticon-list-3', '/app/admin/job-descriptions'),
                    new AppMenuItem('JobDescriptionAcknowledgements', 'Pages.JobProfileManagement.JobDescriptionAcknowledgements', 'flaticon-list-3', '/app/admin/job-description-acknowledgements'),
                ]
            ),
            new AppMenuItem(
                'GatePassManagement',
                '',
                'flaticon-security',
                '',
                [],
                [
                    new AppMenuItem('GatePassTypes', 'Pages.GatePassManagement.GatePassTypes', 'flaticon-list-3', '/app/admin/gate-pass-types'),
                    new AppMenuItem('GatePassMovements', 'Pages.GatePassManagement.GatePassMovements', 'flaticon-list-3', '/app/admin/gate-pass-movements'),
                    new AppMenuItem('GatePassStatuses', 'Pages.GatePassManagement.GatePassStatuses', 'flaticon-list-3', '/app/admin/gate-pass-statuses'),
                    new AppMenuItem('PersonTypes', 'Pages.GatePassManagement.PersonTypes', 'flaticon-list-3', '/app/admin/person-types'),
                    new AppMenuItem('VehicleTypes', 'Pages.GatePassManagement.VehicleTypes', 'flaticon-list-3', '/app/admin/vehicle-types'),
                    new AppMenuItem('VehicleBrands', 'Pages.GatePassManagement.VehicleBrands', 'flaticon-list-3', '/app/admin/vehicle-brands'),
                    new AppMenuItem('VehicleModels', 'Pages.GatePassManagement.VehicleModels', 'flaticon-list-3', '/app/admin/vehicle-models'),
                    new AppMenuItem('GatePassVendors', 'Pages.GatePassManagement.GatePassVendors', 'flaticon-list-3', '/app/admin/gate-pass-vendors'),
                    new AppMenuItem('GatePasses', 'Pages.GatePassManagement.GatePasses', 'flaticon-list-3', '/app/admin/gate-passes'),
                    new AppMenuItem('PoolCars', 'Pages.GatePassManagement.PoolCars', 'flaticon-list-3', '/app/admin/pool-cars'),
                    new AppMenuItem('EmployeePoolCars', 'Pages.GatePassManagement.EmployeePoolCars', 'flaticon-list-3', '/app/admin/employee-pool-cars'),
                ]
            ),
            new AppMenuItem(
                'SuccessionManagement',
                '',
                'flaticon-network',
                '',
                [],
                [
                    new AppMenuItem('ImpactOfLosses', 'Pages.SuccessionManagement.ImpactOfLosses', 'flaticon-list-3', '/app/admin/impact-of-losses'),
                    new AppMenuItem('NineBoxRatings', 'Pages.SuccessionManagement.NineBoxRatings', 'flaticon-list-3', '/app/admin/nine-box-ratings'),
                    new AppMenuItem('Rankings', 'Pages.SuccessionManagement.Rankings', 'flaticon-list-3', '/app/admin/rankings'),
                    new AppMenuItem('Readinesses', 'Pages.SuccessionManagement.Readinesses', 'flaticon-list-3', '/app/admin/readinesses'),
                    new AppMenuItem('ReasonForLeavings', 'Pages.SuccessionManagement.ReasonForLeavings', 'flaticon-list-3', '/app/admin/reason-for-leavings'),
                    new AppMenuItem('RiskOfLosses', 'Pages.SuccessionManagement.RiskOfLosses', 'flaticon-list-3', '/app/admin/risk-of-losses'),
                    new AppMenuItem('SuccessionRatings', 'Pages.SuccessionManagement.SuccessionRatings', 'flaticon-list-3', '/app/admin/succession-ratings'),
                    new AppMenuItem('TalentPools', 'Pages.SuccessionManagement.TalentPools', 'flaticon-list-3', '/app/admin/talent-pools'),
                    new AppMenuItem('NineBoxMappings', 'Pages.SuccessionManagement.NineBoxMappings', 'flaticon-list-3', '/app/admin/nine-box-mappings'),
                    new AppMenuItem('SuccessorNominees', 'Pages.SuccessionManagement.SuccessorNominees', 'flaticon-list-3', '/app/admin/successor-nominees'),
                ]
            ),
            new AppMenuItem(
                'PersonnelAdministration',
                '',
                'flaticon-user',
                '',
                [],
                [
                    new AppMenuItem('Employees', 'Pages.PersonnelAdministration.Employees', 'flaticon-user', '/app/admin/employees'),
                    new AppMenuItem('EmployeeBasicPayDetails', 'Pages.PersonnelAdministration.EmployeeBasicPayDetails', 'flaticon-list-3', '/app/admin/employee-basic-pay-details'),
                    new AppMenuItem('EmployeeLoanExemptions', 'Pages.PersonnelAdministration.EmployeeLoanExemptions', 'flaticon-list-3', '/app/admin/employee-loan-exemptions'),
                    new AppMenuItem('EmployeeLoanRecoveries', 'Pages.PersonnelAdministration.EmployeeLoanRecoveries', 'flaticon-list-3', '/app/admin/employee-loan-recoveries'),
                    new AppMenuItem('LoanInstallments', 'Pages.PersonnelAdministration.LoanInstallments', 'flaticon-list-3', '/app/admin/loan-installments'),
                    new AppMenuItem('EmployeeFundDetails', 'Pages.PersonnelAdministration.EmployeeFundDetails', 'flaticon-list-3', '/app/admin/employee-fund-details'),
                    new AppMenuItem('EmployeeFundExemptions', 'Pages.PersonnelAdministration.EmployeeFundExemptions', 'flaticon-list-3', '/app/admin/employee-fund-exemptions'),
                    new AppMenuItem('EmployeeDependantExceptions', 'Pages.PersonnelAdministration.EmployeeDependantExceptions', 'flaticon-list-3', '/app/admin/employee-dependant-exceptions'),
                ]
            ),
            new AppMenuItem(
                'GovtRelationsManagement',
                '',
                'flaticon-map',
                '',
                [],
                [
                    new AppMenuItem('BusinessTypes', 'Pages.GovtRelationsManagement.BusinessTypes', 'flaticon-list-3', '/app/admin/business-types'),
                    new AppMenuItem('GovtJobCategories', 'Pages.GovtRelationsManagement.GovtJobCategories', 'flaticon-list-3', '/app/admin/govt-job-categories'),
                    new AppMenuItem('GovtJobTitles', 'Pages.GovtRelationsManagement.GovtJobTitles', 'flaticon-list-3', '/app/admin/govt-job-titles'),
                    new AppMenuItem('Signatories', 'Pages.GovtRelationsManagement.Signatories', 'flaticon-list-3', '/app/admin/signatories'),
                    new AppMenuItem('Sponsors', 'Pages.GovtRelationsManagement.Sponsors', 'flaticon-list-3', '/app/admin/sponsors'),
                    new AppMenuItem('OrganizationDocuments', 'Pages.GovtRelationsManagement.OrganizationDocuments', 'flaticon-list-3', '/app/admin/organization-documents'),
                ]
            ),
            new AppMenuItem(
                'Recruitment',
                '',
                'flaticon-questions-circular-button',
                '',
                [],
                [
                    new AppMenuItem('Applicants', 'Pages.Recruitment.Applicants', 'flaticon-list-3', '/app/admin/applicants'),
                    new AppMenuItem('Vacancies', 'Pages.Recruitment.Vacancies', 'flaticon-list-3', '/app/admin/vacancies'),
                    new AppMenuItem('ApplicationStatuses', 'Pages.Recruitment.ApplicationStatuses', 'flaticon-list-3', '/app/admin/application-statuses'),
                    new AppMenuItem('InterviewDetails', 'Pages.Recruitment.InterviewDetails', 'flaticon-list-3', '/app/admin/interview-details'),
                    new AppMenuItem('OnbGroups', 'Pages.Recruitment.OnbGroups', 'flaticon-list-3', '/app/admin/onb-groups'),
                    new AppMenuItem('OnbGroupDetails', 'Pages.Recruitment.OnbGroupDetails', 'flaticon-list-3', '/app/admin/onb-group-details'),
                    new AppMenuItem('OnbTasks', 'Pages.Recruitment.OnbTasks', 'flaticon-list-3', '/app/admin/onb-tasks'),
                    new AppMenuItem('OnbTemplates', 'Pages.Recruitment.OnbTemplates', 'flaticon-list-3', '/app/admin/onb-templates'),
                    new AppMenuItem('OnbTemplateDetails', 'Pages.Recruitment.OnbTemplateDetails', 'flaticon-list-3', '/app/admin/onb-template-details'),
                    new AppMenuItem('OnbApplicantTasks', 'Pages.Recruitment.OnbApplicantTasks', 'flaticon-list-3', '/app/admin/onb-applicant-tasks'),
                    new AppMenuItem('OnbAppTaskComments', 'Pages.Recruitment.OnbAppTaskComments', 'flaticon-list-3', '/app/admin/onb-app-task-comments'),
                    new AppMenuItem('OnbVideoLinks', 'Pages.Recruitment.OnbVideoLinks', 'flaticon-list-3', '/app/admin/onb-video-links'),
                ]
            ),
            new AppMenuItem(
                'ProjectManagement',
                '',
                'flaticon-folder-1',
                '',
                [],
                [
                    new AppMenuItem('ProjectTypes', 'Pages.ProjectManagement.ProjectTypes', 'flaticon-list-3', '/app/admin/project-types'),
                    new AppMenuItem('Projects', 'Pages.ProjectManagement.Projects', 'flaticon-list-3', '/app/admin/projects'),
                    new AppMenuItem('ProjectWageTypes', 'Pages.ProjectManagement.ProjectWageTypes', 'flaticon-list-3', '/app/admin/project-wage-types'),
                    new AppMenuItem('EmployeeProjects', 'Pages.ProjectManagement.EmployeeProjects', 'flaticon-list-3', '/app/admin/employee-projects'),
                    new AppMenuItem('EmployeeProjectDetails', 'Pages.ProjectManagement.EmployeeProjectDetails', 'flaticon-list-3', '/app/admin/employee-project-details'),
                    new AppMenuItem('ProjectRegisters', 'Pages.ProjectManagement.ProjectRegisters', 'flaticon-list-3', '/app/admin/project-registers'),
                    new AppMenuItem('ProjectRegisterDetails', 'Pages.ProjectManagement.ProjectRegisterDetails', 'flaticon-list-3', '/app/admin/project-register-details'),
                    new AppMenuItem('ProjectLogs', 'Pages.ProjectManagement.ProjectLogs', 'flaticon-list-3', '/app/admin/project-logs'),
                    new AppMenuItem('ProjectCostEngine', 'Pages.ProjectManagement.ProjectCostEngine', 'flaticon-settings', '/app/admin/project-cost-engine'),
                ]
            ),
            new AppMenuItem(
                'Workflow',
                '',
                'flaticon-refresh',
                '',
                [],
                [
                    new AppMenuItem('WorkflowRoles', 'Pages.Workflow.WorkflowRoles', 'flaticon-list-3', '/app/admin/workflow-roles'),
                    new AppMenuItem('WorkflowGroups', 'Pages.Workflow.WorkflowGroups', 'flaticon-list-3', '/app/admin/workflow-groups'),
                    new AppMenuItem('RequestMasters', 'Pages.Workflow.RequestMasters', 'flaticon-list-3', '/app/admin/request-masters'),
                    new AppMenuItem('RequestDetails', 'Pages.Workflow.RequestDetails', 'flaticon-list-3', '/app/admin/request-details'),
                    new AppMenuItem('RequestDetailParallels', 'Pages.Workflow.RequestDetailParallels', 'flaticon-list-3', '/app/admin/request-detail-parallels'),
                    new AppMenuItem('WorkflowRequests', 'Pages.Workflow.WorkflowRequests', 'flaticon-list-3', '/app/admin/workflow-requests'),
                    new AppMenuItem('RequestLogs', 'Pages.Workflow.RequestLogs', 'flaticon-list-3', '/app/admin/request-logs'),
                    new AppMenuItem('WorkFlowApprovers', 'Pages.Workflow.WorkFlowApprovers', 'flaticon-list-3', '/app/admin/work-flow-approvers'),
                    new AppMenuItem('WorkflowRemarks', 'Pages.Workflow.WorkflowRemarks', 'flaticon-list-3', '/app/admin/workflow-remarks'),
                    new AppMenuItem('WFReadLogs', 'Pages.Workflow.WFReadLogs', 'flaticon-list-3', '/app/admin/wf-read-logs'),
                ]
            ),
            new AppMenuItem(
                'AccommodationManagement',
                '',
                'flaticon-home',
                '',
                [],
                [
                    new AppMenuItem('Locations', 'Pages.AccommodationManagement.Locations', 'flaticon-list-3', '/app/admin/locations'),
                    new AppMenuItem('Buildings', 'Pages.AccommodationManagement.Buildings', 'flaticon-list-3', '/app/admin/buildings'),
                    new AppMenuItem('BuildingContracts', 'Pages.AccommodationManagement.BuildingContracts', 'flaticon-list-3', '/app/admin/building-contracts'),
                    new AppMenuItem('Floors', 'Pages.AccommodationManagement.Floors', 'flaticon-list-3', '/app/admin/floors'),
                    new AppMenuItem('Flats', 'Pages.AccommodationManagement.Flats', 'flaticon-list-3', '/app/admin/flats'),
                    new AppMenuItem('Rooms', 'Pages.AccommodationManagement.Rooms', 'flaticon-list-3', '/app/admin/rooms'),
                    new AppMenuItem('Beds', 'Pages.AccommodationManagement.Beds', 'flaticon-list-3', '/app/admin/beds'),
                    new AppMenuItem('AccommodationTypes', 'Pages.AccommodationManagement.AccommodationTypes', 'flaticon-list-3', '/app/admin/accommodation-types'),
                    new AppMenuItem('AccommodationServiceTypes', 'Pages.AccommodationManagement.AccommodationServiceTypes', 'flaticon-list-3', '/app/admin/accommodation-service-types'),
                    new AppMenuItem('EmployeeAccommodations', 'Pages.AccommodationManagement.EmployeeAccommodations', 'flaticon-list-3', '/app/admin/employee-accommodations'),
                    new AppMenuItem('AccommodationServices', 'Pages.AccommodationManagement.AccommodationServices', 'flaticon-list-3', '/app/admin/accommodation-services'),
                ]
            ),
            new AppMenuItem(
                'SurveyManagement',
                '',
                'flaticon-notepad',
                '',
                [],
                [
                    new AppMenuItem('Surveys', 'Pages.SurveyManagement.Surveys', 'flaticon-list-3', '/app/admin/surveys'),
                    new AppMenuItem('SurveyDetails', 'Pages.SurveyManagement.SurveyDetails', 'flaticon-list-3', '/app/admin/survey-details'),
                    new AppMenuItem('EmployeeSurveys', 'Pages.SurveyManagement.EmployeeSurveys', 'flaticon-list-3', '/app/admin/employee-surveys'),
                    new AppMenuItem('EmployeeSurveyDetails', 'Pages.SurveyManagement.EmployeeSurveyDetails', 'flaticon-list-3', '/app/admin/employee-survey-details'),
                    new AppMenuItem('TakeSurvey', 'Pages.SurveyManagement.EmployeeSurveys', 'flaticon-edit', '/app/admin/take-survey'),
                ]
            ),
            new AppMenuItem(
                'BenefitsManagement',
                '',
                'flaticon-medical',
                '',
                [],
                [
                    new AppMenuItem('InsurancePlans', 'Pages.BenefitsManagement.InsurancePlans', 'flaticon-list-3', '/app/admin/insurance-plans'),
                    new AppMenuItem('InsuranceProviders', 'Pages.BenefitsManagement.InsuranceProviders', 'flaticon-list-3', '/app/admin/insurance-providers'),
                    new AppMenuItem('EmployeeInsuranceDetails', 'Pages.BenefitsManagement.EmployeeInsuranceDetails', 'flaticon-list-3', '/app/admin/employee-insurance-details'),
                    new AppMenuItem('CompanyInsuranceDetails', 'Pages.BenefitsManagement.CompanyInsuranceDetails', 'flaticon-list-3', '/app/admin/company-insurance-details'),
                ]
            ),
            new AppMenuItem(
                'TravelManagement',
                '',
                'flaticon-airplane',
                '',
                [],
                [
                    new AppMenuItem('ExpenseTypes', 'Pages.TravelManagement.ExpenseTypes', 'flaticon-list-3', '/app/admin/expense-types'),
                    new AppMenuItem('Hotels', 'Pages.TravelManagement.Hotels', 'flaticon-list-3', '/app/admin/hotels'),
                    new AppMenuItem('TravelAirlines', 'Pages.TravelManagement.TravelAirlines', 'flaticon-list-3', '/app/admin/travel-airlines'),
                    new AppMenuItem('TravelCancelReasons', 'Pages.TravelManagement.TravelCancelReasons', 'flaticon-list-3', '/app/admin/travel-cancel-reasons'),
                    new AppMenuItem('TravelDestinations', 'Pages.TravelManagement.TravelDestinations', 'flaticon-list-3', '/app/admin/travel-destinations'),
                    new AppMenuItem('TravelTypes', 'Pages.TravelManagement.TravelTypes', 'flaticon-list-3', '/app/admin/travel-types'),
                    new AppMenuItem('TravelPerDiems', 'Pages.TravelManagement.TravelPerDiems', 'flaticon-list-3', '/app/admin/travel-per-diems'),
                    new AppMenuItem('Travels', 'Pages.TravelManagement.Travels', 'flaticon-list-3', '/app/admin/travels'),
                    new AppMenuItem('TravelDetails', 'Pages.TravelManagement.TravelDetails', 'flaticon-list-3', '/app/admin/travel-details'),
                    new AppMenuItem('TravelDetailCosts', 'Pages.TravelManagement.TravelDetailCosts', 'flaticon-list-3', '/app/admin/travel-detail-costs'),
                    new AppMenuItem('TravelFeedbacks', 'Pages.TravelManagement.TravelFeedbacks', 'flaticon-list-3', '/app/admin/travel-feedbacks'),
                ]
            ),
            new AppMenuItem(
                'PerformanceManagement',
                '',
                'flaticon-medal',
                '',
                [],
                [
                    new AppMenuItem('FormTypes', 'Pages.PerformanceManagement.FormTypes', 'flaticon-list-3', '/app/admin/form-types'),
                    new AppMenuItem('KpiCategories', 'Pages.PerformanceManagement.KpiCategories', 'flaticon-list-3', '/app/admin/kpi-categories'),
                    new AppMenuItem('KpiInfos', 'Pages.PerformanceManagement.KpiInfos', 'flaticon-list-3', '/app/admin/kpi-infos'),
                    new AppMenuItem('PARatings', 'Pages.PerformanceManagement.PARatings', 'flaticon-list-3', '/app/admin/pa-ratings'),
                    new AppMenuItem('PACycles', 'Pages.PerformanceManagement.PACycles', 'flaticon-list-3', '/app/admin/pa-cycles'),
                    new AppMenuItem('PAPeriods', 'Pages.PerformanceManagement.PAPeriods', 'flaticon-list-3', '/app/admin/pa-periods'),
                    new AppMenuItem('PATimelines', 'Pages.PerformanceManagement.PATimelines', 'flaticon-list-3', '/app/admin/pa-timelines'),
                    new AppMenuItem('PATimelineExceptions', 'Pages.PerformanceManagement.PATimelineExceptions', 'flaticon-list-3', '/app/admin/pa-timeline-exceptions'),
                    new AppMenuItem('PAPeerNominees', 'Pages.PerformanceManagement.PAPeerNominees', 'flaticon-list-3', '/app/admin/pa-peer-nominees'),
                    new AppMenuItem('KpiByJobTitles', 'Pages.PerformanceManagement.KpiByJobTitles', 'flaticon-list-3', '/app/admin/kpi-by-job-titles'),
                    new AppMenuItem('KpiByJobTitleDetails', 'Pages.PerformanceManagement.KpiByJobTitles', 'flaticon-list-3', '/app/admin/kpi-by-job-title-details'),
                    new AppMenuItem('KpiByOrgUnits', 'Pages.PerformanceManagement.KpiByOrgUnits', 'flaticon-list-3', '/app/admin/kpi-by-org-units'),
                    new AppMenuItem('KpiByOrgUnitDetails', 'Pages.PerformanceManagement.KpiByOrgUnits', 'flaticon-list-3', '/app/admin/kpi-by-org-unit-details'),
                    new AppMenuItem('PAForms', 'Pages.PerformanceManagement.PAForms', 'flaticon-list-3', '/app/admin/pa-forms'),
                    new AppMenuItem('PerformanceAppraisalDashboard', 'Pages.PerformanceManagement.PAForms', 'flaticon-analytics', '/app/admin/performance-appraisal-dashboard'),
                    new AppMenuItem('PAEmployees', 'Pages.PerformanceManagement.PAEmployees', 'flaticon-list-3', '/app/admin/pa-employees'),
                ]
            ),
            new AppMenuItem(
                'CompetencyDevelopment',
                '',
                'flaticon-medal',
                '',
                [],
                [
                    new AppMenuItem('CompetencyTypes', 'Pages.CompetencyDevelopment.CompetencyTypes', 'flaticon-list-3', '/app/admin/competency-types'),
                    new AppMenuItem('Competencies', 'Pages.CompetencyDevelopment.Competencies', 'flaticon-list-3', '/app/admin/competencies'),
                    new AppMenuItem('CompetencyByJobTitles', 'Pages.CompetencyDevelopment.CompetencyByJobTitles', 'flaticon-list-3', '/app/admin/competency-by-job-titles'),
                    new AppMenuItem('CompetencyByJobTitleDetails', 'Pages.CompetencyDevelopment.CompetencyByJobTitles', 'flaticon-list-3', '/app/admin/competency-by-job-title-details'),
                    new AppMenuItem('CDPeriods', 'Pages.CompetencyDevelopment.CDPeriods', 'flaticon-list-3', '/app/admin/cd-periods'),
                    new AppMenuItem('CDRatings', 'Pages.CompetencyDevelopment.CDRatings', 'flaticon-list-3', '/app/admin/cd-ratings'),
                    new AppMenuItem('CDForms', 'Pages.CompetencyDevelopment.CDForms', 'flaticon-list-3', '/app/admin/cd-forms'),
                ]
            ),
            new AppMenuItem(
                'ManpowerPlanningManagement',
                '',
                'flaticon-map',
                '',
                [],
                [
                    new AppMenuItem('ManpowerPlanningPeriods', 'Pages.ManpowerPlanningManagement.ManpowerPlanningPeriods', 'flaticon-list-3', '/app/admin/manpower-planning-periods'),
                    new AppMenuItem('Positions', 'Pages.ManpowerPlanningManagement.Positions', 'flaticon-list-3', '/app/admin/positions'),
                    new AppMenuItem('PositionRelationships', 'Pages.ManpowerPlanningManagement.PositionRelationships', 'flaticon-list-3', '/app/admin/position-relationships'),
                    new AppMenuItem('ManpowerPlannings', 'Pages.ManpowerPlanningManagement.ManpowerPlannings', 'flaticon-list-3', '/app/admin/manpower-plannings'),
                ]
            ),
            new AppMenuItem(
                'TrainingManagement',
                '',
                'flaticon-book',
                '',
                [],
                [
                    new AppMenuItem('TrainingCourses', 'Pages.TrainingManagement.TrainingCourses', 'flaticon-list-3', '/app/admin/training-courses'),
                    new AppMenuItem('TrainingSchedules', 'Pages.TrainingManagement.TrainingSchedules', 'flaticon-list-3', '/app/admin/training-schedules'),
                    new AppMenuItem('EmployeeTrainings', 'Pages.TrainingManagement.EmployeeTrainings', 'flaticon-list-3', '/app/admin/employee-trainings'),
                    new AppMenuItem('TrainingAttendances', 'Pages.TrainingManagement.TrainingAttendances', 'flaticon-list-3', '/app/admin/training-attendances'),
                ]
            ),
            new AppMenuItem(
                'SystemSettings',
                '',
                'flaticon-settings',
                '',
                [],
                [
                    new AppMenuItem('AppraisalSettings', 'Pages.SystemSettings.AppraisalSettings', 'flaticon-list-3', '/app/admin/manage-appraisal-settings'),
                    new AppMenuItem('DocumentSettings', 'Pages.SystemSettings.DocumentSettings', 'flaticon-list-3', '/app/admin/manage-document-settings'),
                    new AppMenuItem('FIPostingSettings', 'Pages.SystemSettings.FIPostingSettings', 'flaticon-list-3', '/app/admin/manage-fi-posting-settings'),
                    new AppMenuItem('GeneralSettings', 'Pages.SystemSettings.GeneralSettings', 'flaticon-list-3', '/app/admin/manage-general-settings'),
                    new AppMenuItem('NotificationSettings', 'Pages.SystemSettings.NotificationSettings', 'flaticon-list-3', '/app/admin/manage-notification-settings'),
                    new AppMenuItem('OrgSettings', 'Pages.SystemSettings.OrgSettings', 'flaticon-list-3', '/app/admin/manage-org-settings'),
                    new AppMenuItem('PayrollSettings', 'Pages.SystemSettings.PayrollSettings', 'flaticon-list-3', '/app/admin/manage-payroll-settings'),
                    new AppMenuItem('PersonnelSettings', 'Pages.SystemSettings.PersonnelSettings', 'flaticon-list-3', '/app/admin/manage-personnel-settings'),
                    new AppMenuItem('ProjectCostingSettings', 'Pages.SystemSettings.ProjectCostingSettings', 'flaticon-list-3', '/app/admin/manage-project-costing-settings'),
                    new AppMenuItem('ReportSettings', 'Pages.SystemSettings.ReportSettings', 'flaticon-list-3', '/app/admin/manage-report-settings'),
                    new AppMenuItem('SecuritySettings', 'Pages.SystemSettings.SecuritySettings', 'flaticon-list-3', '/app/admin/manage-security-settings'),
                    new AppMenuItem('SelfServiceSettings', 'Pages.SystemSettings.SelfServiceSettings', 'flaticon-list-3', '/app/admin/manage-self-service-settings'),
                    new AppMenuItem('TimeSettings', 'Pages.SystemSettings.TimeSettings', 'flaticon-list-3', '/app/admin/manage-time-settings'),
                    new AppMenuItem('WorkflowSettings', 'Pages.SystemSettings.WorkflowSettings', 'flaticon-list-3', '/app/admin/manage-workflow-settings'),
                    new AppMenuItem('PayrollDrivers', 'Pages.SystemSettings.PayrollDrivers', 'flaticon-list-3', '/app/admin/payroll-drivers'),
                ]
            ),
            new AppMenuItem(
                'SystemSecurity',
                '',
                'flaticon-lock',
                '',
                [],
                [
                    new AppMenuItem('SysUserOrgUnitDatas', 'Pages.SystemSecurity.SysUserOrgUnitData', 'flaticon-list-3', '/app/admin/sys-user-org-unit-datas'),
                    new AppMenuItem('EmailTemplates', 'Pages.SystemSecurity.EmailTemplates', 'flaticon-list-3', '/app/admin/email-templates'),
                    new AppMenuItem('EmailTokens', 'Pages.SystemSecurity.EmailTokens', 'flaticon-list-3', '/app/admin/email-tokens'),
                    new AppMenuItem('PayslipConfigs', 'Pages.SystemSecurity.PayslipConfigs', 'flaticon-list-3', '/app/admin/payslip-configs'),
                    new AppMenuItem('NotificationMasters', 'Pages.SystemSecurity.NotificationMasters', 'flaticon-list-3', '/app/admin/notification-masters'),
                    new AppMenuItem('SystemNotifications', 'Pages.SystemSecurity.SystemNotifications', 'flaticon-list-3', '/app/admin/system-notifications'),
                    new AppMenuItem('SysObjects', 'Pages.SystemSecurity.SysObjects', 'flaticon-list-3', '/app/admin/sys-objects'),
                    new AppMenuItem('RetroReasonLogs', 'Pages.SystemSecurity.RetroReasonLogs', 'flaticon-list-3', '/app/admin/retro-reason-logs'),
                    new AppMenuItem('PunchNotificationLogs', 'Pages.SystemSecurity.PunchNotificationLogs', 'flaticon-list-3', '/app/admin/punch-notification-logs'),
                    new AppMenuItem('BulkUploadLogs', 'Pages.SystemSecurity.BulkUploadLogs', 'flaticon-list-3', '/app/admin/bulk-upload-logs'),
                    new AppMenuItem('ExecutionLogs', 'Pages.SystemSecurity.ExecutionLogs', 'flaticon-list-3', '/app/admin/execution-logs'),
                    new AppMenuItem('EmailNotificationLogs', 'Pages.SystemSecurity.EmailNotificationLogs', 'flaticon-list-3', '/app/admin/email-notification-logs'),
                    new AppMenuItem('SysTenantConfigs', 'Pages.SystemSecurity.SysTenantConfigs', 'flaticon-list-3', '/app/admin/sys-tenant-configs'),
                ]
            ),
            new AppMenuItem(
                'ReportManagement',
                '',
                'flaticon-list-3',
                '',
                [],
                [
                    new AppMenuItem('DomainTypes', 'Pages.ReportManagement.DomainTypes', 'flaticon-list-3', '/app/admin/domain-types'),
                    new AppMenuItem('ReportTypes', 'Pages.ReportManagement.ReportTypes', 'flaticon-list-3', '/app/admin/report-types'),
                    new AppMenuItem('PrintReports', 'Pages.ReportManagement.PrintReports', 'flaticon-list-3', '/app/admin/print-reports'),
                    new AppMenuItem('PrintReportPermissions', 'Pages.ReportManagement.PrintReportPermissions', 'flaticon-list-3', '/app/admin/print-report-permissions'),
                    new AppMenuItem('PrintReportPermissionDetails', 'Pages.ReportManagement.PrintReportPermissionDetails', 'flaticon-list-3', '/app/admin/print-report-permission-details'),
                    new AppMenuItem('ReportMasters', 'Pages.ReportManagement.ReportMasters', 'flaticon-list-3', '/app/admin/report-masters'),
                    new AppMenuItem('ReportDetails', 'Pages.ReportManagement.ReportDetails', 'flaticon-list-3', '/app/admin/report-details'),
                    new AppMenuItem('ReportSettings', 'Pages.ReportManagement.ReportSettings', 'flaticon-list-3', '/app/admin/report-settings'),
                    new AppMenuItem('ReportSettingDetails', 'Pages.ReportManagement.ReportSettingDetails', 'flaticon-list-3', '/app/admin/report-setting-details'),
                    new AppMenuItem('ReportInfos', 'Pages.ReportManagement.ReportInfos', 'flaticon-list-3', '/app/admin/report-infos'),
                    new AppMenuItem('ReportInfoDetails', 'Pages.ReportManagement.ReportInfoDetails', 'flaticon-list-3', '/app/admin/report-info-details'),
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
                        'CopyWorkflowSetup',
                        'Pages.Administration.Tools.CopyWorkflowSetup',
                        'flaticon-interface-9',
                        '/app/admin/copy-workflow-setup'
                    ),
                    new AppMenuItem(
                        'PayrollAdminTools',
                        'Pages.Administration.Tools',
                        'flaticon-refresh',
                        '/app/admin/payroll-admin-tools'
                    ),
                    new AppMenuItem(
                        'SystemAdminTools',
                        'Pages.Administration.Tools',
                        'flaticon-settings',
                        '/app/admin/system-admin-tools'
                    ),
                    new AppMenuItem(
                        'TransferApprovals',
                        'Pages.Administration.Tools.TransferApprovals',
                        'flaticon-share',
                        '/app/admin/transfer-approvals'
                    ),
                    new AppMenuItem(
                        'BulkPasswordReset',
                        'Pages.Administration.Tools.BulkPasswordReset',
                        'flaticon-lock',
                        '/app/admin/bulk-password-reset'
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
            new AppMenuItem(
                'HealthAndSafetyManagement',
                '',
                'flaticon-safe-shield-protection',
                '',
                [],
                [
                    new AppMenuItem('ActionStatuses', 'Pages.HealthAndSafetyManagement.ActionStatuses', 'flaticon-list-3', '/app/admin/action-statuses'),
                    new AppMenuItem('FirstAidMedicines', 'Pages.HealthAndSafetyManagement.FirstAidMedicines', 'flaticon-list-3', '/app/admin/first-aid-medicines'),
                    new AppMenuItem('IncidentClaimStatuses', 'Pages.HealthAndSafetyManagement.IncidentClaimStatuses', 'flaticon-list-3', '/app/admin/incident-claim-statuses'),
                    new AppMenuItem('IncidentNatures', 'Pages.HealthAndSafetyManagement.IncidentNatures', 'flaticon-list-3', '/app/admin/incident-natures'),
                    new AppMenuItem('IncidentSeverities', 'Pages.HealthAndSafetyManagement.IncidentSeverities', 'flaticon-list-3', '/app/admin/incident-severities'),
                    new AppMenuItem('IncidentStatuses', 'Pages.HealthAndSafetyManagement.IncidentStatuses', 'flaticon-list-3', '/app/admin/incident-statuses'),
                    new AppMenuItem('IncidentTypes', 'Pages.HealthAndSafetyManagement.IncidentTypes', 'flaticon-list-3', '/app/admin/incident-types'),
                    new AppMenuItem('InvestigationRootCauses', 'Pages.HealthAndSafetyManagement.InvestigationRootCauses', 'flaticon-list-3', '/app/admin/investigation-root-causes'),
                    new AppMenuItem('RiskActions', 'Pages.HealthAndSafetyManagement.RiskActions', 'flaticon-list-3', '/app/admin/risk-actions'),
                    new AppMenuItem('RiskImpacts', 'Pages.HealthAndSafetyManagement.RiskImpacts', 'flaticon-list-3', '/app/admin/risk-impacts'),
                    new AppMenuItem('RiskPossibilities', 'Pages.HealthAndSafetyManagement.RiskPossibilities', 'flaticon-list-3', '/app/admin/risk-possibilities'),
                    new AppMenuItem('RiskStatuses', 'Pages.HealthAndSafetyManagement.RiskStatuses', 'flaticon-list-3', '/app/admin/risk-statuses'),
                    new AppMenuItem('RiskTypes', 'Pages.HealthAndSafetyManagement.RiskTypes', 'flaticon-list-3', '/app/admin/risk-types'),
                    new AppMenuItem('SafetyAreaAssets', 'Pages.HealthAndSafetyManagement.SafetyAreaAssets', 'flaticon-list-3', '/app/admin/safety-area-assets'),
                    new AppMenuItem('ActionRegisters', 'Pages.HealthAndSafetyManagement.ActionRegisters', 'flaticon-list-3', '/app/admin/action-registers'),
                    new AppMenuItem('FirstAidBoxes', 'Pages.HealthAndSafetyManagement.FirstAidBoxes', 'flaticon-list-3', '/app/admin/first-aid-boxes'),
                    new AppMenuItem('IncidentClaims', 'Pages.HealthAndSafetyManagement.IncidentClaims', 'flaticon-list-3', '/app/admin/incident-claims'),
                    new AppMenuItem('IncidentInvestigations', 'Pages.HealthAndSafetyManagement.IncidentInvestigations', 'flaticon-list-3', '/app/admin/incident-investigations'),
                    new AppMenuItem('IncidentRegisters', 'Pages.HealthAndSafetyManagement.IncidentRegisters', 'flaticon-list-3', '/app/admin/incident-registers'),
                    new AppMenuItem('RiskRegisters', 'Pages.HealthAndSafetyManagement.RiskRegisters', 'flaticon-list-3', '/app/admin/risk-registers'),
                    new AppMenuItem('SafetyChecklists', 'Pages.HealthAndSafetyManagement.SafetyChecklists', 'flaticon-list-3', '/app/admin/safety-checklists'),
                    new AppMenuItem('SiteAudits', 'Pages.HealthAndSafetyManagement.SiteAudits', 'flaticon-list-3', '/app/admin/site-audits'),
                ]
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

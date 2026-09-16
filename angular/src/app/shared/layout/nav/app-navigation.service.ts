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
            // "Home", not "Dashboard": the label says what somebody goes there for, not what it is built
            // from. The HOST dashboard above keeps its name -- it really is a dashboard, and only a host
            // administrator sees it.
            new AppMenuItem('Home', 'Pages.Tenant.Dashboard', 'flaticon-home', '/app/main/home'),

            // GO-LIVE SETUP. Deliberately near the top: while a tenant is being set up this is
            // the only screen that matters, and burying it under Administration is how onboarding stalls.
            // Once the configuration is locked the permission is revoked and the item disappears on its
            // own -- there is no "hide when finished" flag on AppMenuItem, and revoking the permission is
            // both less code and self-auditing.
            new AppMenuItem(
                'GoLiveSetup',
                'Pages.GoLiveSetup',
                'flaticon-interface-8',
                '/app/admin/go-live-setup'
            ),
            new AppMenuItem(
                'GoLiveSetupV2',
                'Pages.GoLiveSetup',
                'flaticon-interface-8',
                '/app/admin/go-live-setup-v2'
            ),

            // EMPLOYEE SELF SERVICE. Placed first because for most users it is the only section they can
            // see at all: everything below is Pages.* and does not render for someone holding only the
            // Employee role. That permission filtering is what removes the need for a separate ESS
            // application like legacy HCM10.ESS -- though hiding the menu is only cosmetic, and the real
            // guard is [AbpAuthorize(Ess_*)] plus the employee scope filter on every endpoint.
            //
            // Entries are added here only as their screens are built -- a menu item pointing at a route that
            // does not exist is worse than no menu item. The landing page lists the rest from the catalog.
            new AppMenuItem('EmployeeSelfService', 'Ess', 'flaticon-user-ok', '', [], [
                // No EssHome entry: self-service now opens on the main dashboard. A menu item pointing at
                // a route that only redirects would look like a second landing page and land people back
                // where they started, which reads as a broken link rather than a deliberate move.
                new AppMenuItem('EssInbox', 'Ess.Inbox', 'flaticon-multimedia', '/app/main/ess/inbox'),
                new AppMenuItem('EssMyLeave', 'Ess.MyLeave', 'flaticon-calendar', '/app/main/ess/leave'),
                new AppMenuItem('EssMyAssets', 'Ess.MyAssets', 'flaticon-laptop', '/app/main/ess/asset'),
                new AppMenuItem('EssMyDocuments', 'Ess.MyDocuments', 'flaticon-file-2', '/app/main/ess/document'),
                new AppMenuItem(
                    'EssMyPassportWithdrawals',
                    'Ess.MyPassportWithdrawals',
                    'flaticon-suitcase',
                    '/app/main/ess/passport-withdrawal'
                ),
                new AppMenuItem('EssMyExitPermits', 'Ess.MyExitPermits', 'flaticon-paper-plane', '/app/main/ess/exit-permit'),
                new AppMenuItem('EssMyHRServices', 'Ess.MyHRServices', 'flaticon-notepad', '/app/main/ess/hr-service'),
                new AppMenuItem('EssMyLetterRequests', 'Ess.MyLetterRequests', 'flaticon-mail-1', '/app/main/ess/letter'),
                new AppMenuItem('EssMyLoans', 'Ess.MyLoans', 'flaticon-piggy-bank', '/app/main/ess/loan'),
                new AppMenuItem('EssMyOvertime', 'Ess.MyOvertime', 'flaticon-time-3', '/app/main/ess/overtime'),
                new AppMenuItem('EssMyTickets', 'Ess.MyTickets', 'flaticon-price-tag', '/app/main/ess/ticket'),
                new AppMenuItem('EssMyAttendance', 'Ess.MyAttendance', 'flaticon-time-3', '/app/main/ess/attendance'),
                new AppMenuItem('EssMyBank', 'Ess.MyBank', 'flaticon-coins', '/app/main/ess/bank'),
                new AppMenuItem('EssMyTravelClaims', 'Ess.MyTravelClaims', 'flaticon-map', '/app/main/ess/travel-claim'),
                new AppMenuItem('EssMyTravel', 'Ess.MyTravel', 'flaticon-airplane', '/app/main/ess/travel'),
                new AppMenuItem('EssMyDelegations', 'Ess.MyDelegations', 'flaticon-user-ok', '/app/main/ess/delegation'),
                new AppMenuItem('EssMyLoanExemptions', 'Ess.MyLoanExemptions', 'flaticon-coins', '/app/main/ess/loan-exemption'),
                new AppMenuItem('EssMyPunches', 'Ess.MyPunches', 'flaticon-alarm', '/app/main/ess/punch'),
                new AppMenuItem('EssMyClearances', 'Ess.MyClearances', 'flaticon-list-3', '/app/main/ess/clearance'),
                new AppMenuItem(
                    'EssMyHealthReimburses',
                    'Ess.MyHealthReimburses',
                    'flaticon-medical',
                    '/app/main/ess/health-reimburse'
                ),
                new AppMenuItem('EssMyProfile', 'Ess.MyProfile', 'flaticon-user', '/app/main/ess/my-details'),
            ]),
            // PRINT REPORTS LAUNCHER (legacy HRMS "Print Reports"). Deliberately a top-level item, not a
            // child of AnalysisReports - that tree is for the ported grid reports.
            new AppMenuItem(
                'PrintReports',
                'Pages.Reports.PrintReports',
                'flaticon-interface-9',
                '/app/admin/print-reports-launcher',
                ['/app/admin/print-reports-launcher/view/{reportName}']
            ),
            // REPORT DISTRIBUTION (report scheduling + bursting). Sits next to the launcher because it
            // schedules the same reports the launcher runs by hand.
            new AppMenuItem(
                'ReportDistribution',
                'Pages.Reports.Distribution',
                'flaticon-paper-plane',
                '/app/admin/report-distribution',
                [
                    '/app/admin/report-distribution/wizard',
                    '/app/admin/report-distribution/wizard/{id}',
                ]
            ),
            // ANALYSIS GRID REPORTS (legacy HCM10.Web/UserControls/Reports/Analysis). One child entry per
            // ported report; the wave adds ~139 more siblings under this same parent.
            new AppMenuItem(
                'AnalysisReports',
                '',
                'flaticon-analytics',
                '',
                [],
                [
                    new AppMenuItem(
                        'EmployeeReports',
                        '',
                        'flaticon-user',
                        '',
                        [],
                        [
                            new AppMenuItem(
                                'EmployeeActionReport',
                                'Pages.Reports.Analysis.EmployeeAction',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-action'
                            ),
                            new AppMenuItem(
                                'EmployeeActionChangeReport',
                                'Pages.Reports.Analysis.EmployeeActionChange',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-action-change'
                            ),
                            new AppMenuItem(
                                'EmployeeAssetReport',
                                'Pages.Reports.Analysis.EmployeeAsset',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-asset'
                            ),
                            new AppMenuItem(
                                'EmployeeAttendanceReport',
                                'Pages.Reports.Analysis.EmployeeAttendance',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-attendance'
                            ),
                            new AppMenuItem(
                                'EmployeeBankReport',
                                'Pages.Reports.Analysis.EmployeeBank',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-bank'
                            ),
                            new AppMenuItem(
                                'EmployeeBasicPayReport',
                                'Pages.Reports.Analysis.EmployeeBasicPay',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-basic-pay'
                            ),
                            new AppMenuItem(
                                'EmployeeBonusReport',
                                'Pages.Reports.Analysis.EmployeeBonus',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-bonus'
                            ),
                            new AppMenuItem(
                                'EmployeeContractReport',
                                'Pages.Reports.Analysis.EmployeeContract',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-contract'
                            ),
                            new AppMenuItem(
                                'EmployeeCTCReport',
                                'Pages.Reports.Analysis.EmployeeCTC',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-ctc'
                            ),
                            new AppMenuItem(
                                'EmployeeDeductionReport',
                                'Pages.Reports.Analysis.EmployeeDeduction',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-deduction'
                            ),
                            new AppMenuItem(
                                'EmployeeHistoryReport',
                                'Pages.Reports.Analysis.EmployeeHistory',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-history'
                            ),
                            new AppMenuItem(
                                'EmployeeIncentiveReport',
                                'Pages.Reports.Analysis.EmployeeIncentive',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-incentive'
                            ),
                            new AppMenuItem(
                                'EmployeeInfoReport',
                                'Pages.Reports.Analysis.EmployeeInfo',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-info'
                            ),
                            new AppMenuItem(
                                'EmployeeManagerReport',
                                'Pages.Reports.Analysis.EmployeeManager',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-manager'
                            ),
                            new AppMenuItem(
                                'EmployeePostWiseReport',
                                'Pages.Reports.Analysis.EmployeePostWise',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-post-wise'
                            ),
                            new AppMenuItem(
                                'EmployeeRejoinReport',
                                'Pages.Reports.Analysis.EmployeeRejoin',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-rejoin'
                            ),
                            new AppMenuItem(
                                'EmployeeSalaryChangeReport',
                                'Pages.Reports.Analysis.EmployeeSalaryChange',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-salary-change'
                            ),
                            new AppMenuItem(
                                'EmployeeSponsorReport',
                                'Pages.Reports.Analysis.EmployeeSponsor',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-sponsor'
                            ),
                            new AppMenuItem(
                                'EmployeeStatusReport',
                                'Pages.Reports.Analysis.EmployeeStatus',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-status'
                            ),
                            new AppMenuItem(
                                'EmployeeTransferReport',
                                'Pages.Reports.Analysis.EmployeeTransfer',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-transfer'
                            ),
                            new AppMenuItem(
                                'EmployeeWorkflowRoleReport',
                                'Pages.Reports.Analysis.EmployeeWorkflowRole',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-workflow-role'
                            ),
                            new AppMenuItem(
                                'EmployeeWorkLocationReport',
                                'Pages.Reports.Analysis.EmployeeWorkLocation',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-work-location'
                            ),
                            new AppMenuItem(
                                'SalaryBenefitsReport',
                                'Pages.Reports.Analysis.SalaryBenefits',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/salary-benefits'
                            ),
                            new AppMenuItem(
                                'SalaryByNationalityReport',
                                'Pages.Reports.Analysis.SalaryByNationality',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/salary-by-nationality'
                            ),
                            new AppMenuItem(
                                'StaffHeadcountReport',
                                'Pages.Reports.Analysis.StaffHeadcount',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/staff-headcount'
                            ),
                        ]
                    ),
                    new AppMenuItem(
                        'PayrollReports',
                        '',
                        'flaticon-coins',
                        '',
                        [],
                        [
                            new AppMenuItem(
                                'BasicSalaryRegisterReport',
                                'Pages.Reports.Analysis.BasicSalaryRegister',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/basic-salary-register'
                            ),
                            new AppMenuItem(
                                'PayrollAnalysisByDeptReport',
                                'Pages.Reports.Analysis.PayrollAnalysisByDept',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/payroll-analysis-by-dept'
                            ),
                            new AppMenuItem(
                                'PayrollAnalysisReport',
                                'Pages.Reports.Analysis.PayrollAnalysis',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/payroll-analysis'
                            ),
                            new AppMenuItem(
                                'PayrollCTCReport',
                                'Pages.Reports.Analysis.PayrollCTC',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/payroll-ctc'
                            ),
                            new AppMenuItem(
                                'PayrollJVCustom016LoanFormatBmcReport',
                                'Pages.Reports.Analysis.PayrollJVCustom016LoanFormatBmc',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/payroll-jv-custom016-loan-format-bmc'
                            ),
                            new AppMenuItem(
                                'PayrollJVCustom016LoanFormatMybReport',
                                'Pages.Reports.Analysis.PayrollJVCustom016LoanFormatMyb',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/payroll-jv-custom016-loan-format-myb'
                            ),
                            new AppMenuItem(
                                'PayrollJVCustom016MainFormatBmcReport',
                                'Pages.Reports.Analysis.PayrollJVCustom016MainFormatBmc',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/payroll-jv-custom016-main-format-bmc'
                            ),
                            new AppMenuItem(
                                'PayrollJVCustom016MainFormatMybReport',
                                'Pages.Reports.Analysis.PayrollJVCustom016MainFormatMyb',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/payroll-jv-custom016-main-format-myb'
                            ),
                            new AppMenuItem(
                                'PayrollJVCustom016DetailReport',
                                'Pages.Reports.Analysis.PayrollJVCustom016Detail',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/payroll-jv-custom016-detail'
                            ),
                            new AppMenuItem(
                                'PayrollJVCustom016LeaveProvisionReport',
                                'Pages.Reports.Analysis.PayrollJVCustom016LeaveProvision',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/payroll-jv-custom016-leave-provision'
                            ),
                            new AppMenuItem(
                                'PayrollJVCustom016IndemnityProvisionReport',
                                'Pages.Reports.Analysis.PayrollJVCustom016IndemnityProvision',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/payroll-jv-custom016-indemnity-provision'
                            ),
                            new AppMenuItem(
                                'PayrollJVCustom017Report',
                                'Pages.Reports.Analysis.PayrollJVCustom017',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/payroll-jv-custom017'
                            ),
                            new AppMenuItem(
                                'PayrollJVCustom018Report',
                                'Pages.Reports.Analysis.PayrollJVCustom018',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/payroll-jv-custom018'
                            ),
                            new AppMenuItem(
                                'PayrollJVCustom019Report',
                                'Pages.Reports.Analysis.PayrollJVCustom019',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/payroll-jv-custom019'
                            ),
                            new AppMenuItem(
                                'PayrollJVCustom023Report',
                                'Pages.Reports.Analysis.PayrollJVCustom023',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/payroll-jv-custom023'
                            ),
                            new AppMenuItem(
                                'PayrollJVCustom030Report',
                                'Pages.Reports.Analysis.PayrollJVCustom030',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/payroll-jv-custom030'
                            ),
                            new AppMenuItem(
                                'PayrollJVCustom035Report',
                                'Pages.Reports.Analysis.PayrollJVCustom035',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/payroll-jv-custom035'
                            ),
                            new AppMenuItem(
                                'PayrollJVCustom042AdvanceSalaryReport',
                                'Pages.Reports.Analysis.PayrollJVCustom042AdvanceSalary',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/payroll-jv-custom042-advance-salary'
                            ),
                            new AppMenuItem(
                                'PayrollJVCustom042AnnualLeaveProvisionReport',
                                'Pages.Reports.Analysis.PayrollJVCustom042AnnualLeaveProvision',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/payroll-jv-custom042-annual-leave-provision'
                            ),
                            new AppMenuItem(
                                'PayrollJVCustom042EmployeeWiseBreakDownReport',
                                'Pages.Reports.Analysis.PayrollJVCustom042EmployeeWiseBreakDown',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/payroll-jv-custom042-employee-wise-break-down'
                            ),
                            new AppMenuItem(
                                'PayrollJVCustom042IndemnityProvisionReport',
                                'Pages.Reports.Analysis.PayrollJVCustom042IndemnityProvision',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/payroll-jv-custom042-indemnity-provision'
                            ),
                            new AppMenuItem(
                                'PayrollJVCustom042LieuDayLeaveProvisionReport',
                                'Pages.Reports.Analysis.PayrollJVCustom042LieuDayLeaveProvision',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/payroll-jv-custom042-lieu-day-leave-provision'
                            ),
                            new AppMenuItem(
                                'PayrollJVCustom042MissingAccountsReport',
                                'Pages.Reports.Analysis.PayrollJVCustom042MissingAccounts',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/payroll-jv-custom042-missing-accounts'
                            ),
                            new AppMenuItem(
                                'PayrollJVCustom042OvertimeReport',
                                'Pages.Reports.Analysis.PayrollJVCustom042Overtime',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/payroll-jv-custom042-overtime'
                            ),
                            new AppMenuItem(
                                'PayrollJVCustom042PublicHolidayLeaveProvisionReport',
                                'Pages.Reports.Analysis.PayrollJVCustom042PublicHolidayLeaveProvision',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/payroll-jv-custom042-public-holiday-leave-provision'
                            ),
                            new AppMenuItem(
                                'PayrollJVCustom042RamadanTimeBackLeaveProvisionReport',
                                'Pages.Reports.Analysis.PayrollJVCustom042RamadanTimeBackLeaveProvision',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/payroll-jv-custom042-ramadan-time-back-leave-provision'
                            ),
                            new AppMenuItem(
                                'PayrollJVCustom042SalaryReport',
                                'Pages.Reports.Analysis.PayrollJVCustom042Salary',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/payroll-jv-custom042-salary'
                            ),
                            new AppMenuItem(
                                'PayrollJVCustom045Report',
                                'Pages.Reports.Analysis.PayrollJVCustom045',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/payroll-jv-custom045'
                            ),
                            new AppMenuItem(
                                'PayrollJVCustom051Report',
                                'Pages.Reports.Analysis.PayrollJVCustom051',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/payroll-jv-custom051'
                            ),
                            new AppMenuItem(
                                'PayrollNetPayContractDiffReport',
                                'Pages.Reports.Analysis.PayrollNetPayContractDiff',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/payroll-net-pay-contract-diff'
                            ),
                            new AppMenuItem(
                                'PayrollSheetCustom023Report',
                                'Pages.Reports.Analysis.PayrollSheetCustom023',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/payroll-sheet-custom023'
                            ),
                            new AppMenuItem(
                                'PayrollSheetReport',
                                'Pages.Reports.Analysis.PayrollSheet',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/payroll-sheet'
                            ),
                            new AppMenuItem(
                                'PrePayrollReportReport',
                                'Pages.Reports.Analysis.PrePayrollReport',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/pre-payroll-report'
                            ),
                            new AppMenuItem(
                                'SalaryDeductionReport',
                                'Pages.Reports.Analysis.SalaryDeduction',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/salary-deduction'
                            ),
                            new AppMenuItem(
                                'SalaryForecastReport',
                                'Pages.Reports.Analysis.SalaryForecast',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/salary-forecast'
                            ),
                            new AppMenuItem(
                                'SalaryRegisterReport',
                                'Pages.Reports.Analysis.SalaryRegister',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/salary-register'
                            ),
                            new AppMenuItem(
                                'SalaryRetroResultReport',
                                'Pages.Reports.Analysis.SalaryRetroResult',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/salary-retro-result'
                            ),
                            new AppMenuItem(
                                'ServiceBonusReport',
                                'Pages.Reports.Analysis.ServiceBonus',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/service-bonus'
                            ),
                            new AppMenuItem(
                                'StaffNotinPayrollReport',
                                'Pages.Reports.Analysis.StaffNotinPayroll',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/staff-notin-payroll'
                            ),
                            new AppMenuItem(
                                'StaffOvertimeReport',
                                'Pages.Reports.Analysis.StaffOvertime',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/staff-overtime'
                            ),
                            new AppMenuItem(
                                'StaffTurnoverReport',
                                'Pages.Reports.Analysis.StaffTurnover',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/staff-turnover'
                            ),
                        ]
                    ),
                    new AppMenuItem(
                        'TimeAttendanceReports',
                        '',
                        'flaticon-time',
                        '',
                        [],
                        [
                            new AppMenuItem(
                                'AttendanceLogReport',
                                'Pages.Reports.Analysis.AttendanceLog',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/attendance-log'
                            ),
                            new AppMenuItem(
                                'EmpAttendanceLogReport',
                                'Pages.Reports.Analysis.EmpAttendanceLog',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/emp-attendance-log'
                            ),
                            new AppMenuItem(
                                'DailyAttendanceSummaryReport',
                                'Pages.Reports.Analysis.DailyAttendanceSummary',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/daily-attendance-summary'
                            ),
                            new AppMenuItem(
                                'DailyManHoursSummaryReport',
                                'Pages.Reports.Analysis.DailyManHoursSummary',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/daily-man-hours-summary'
                            ),
                            new AppMenuItem(
                                'EmployeeDutyPeriodReport',
                                'Pages.Reports.Analysis.EmployeeDutyPeriod',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-duty-period'
                            ),
                            new AppMenuItem(
                                'LateEarlyLogReport',
                                'Pages.Reports.Analysis.LateEarlyLog',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/late-early-log'
                            ),
                            new AppMenuItem(
                                'LateEarlyLogWithPhotoReport',
                                'Pages.Reports.Analysis.LateEarlyLogWithPhoto',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/late-early-log-with-photo'
                            ),
                            new AppMenuItem(
                                'LateEarlyLogCustom019Report',
                                'Pages.Reports.Analysis.LateEarlyLogCustom019',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/late-early-log-custom019'
                            ),
                            new AppMenuItem(
                                'LateEarlyLogCustom023Report',
                                'Pages.Reports.Analysis.LateEarlyLogCustom023',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/late-early-log-custom023'
                            ),
                            new AppMenuItem(
                                'LateEarlyLogCustom033Report',
                                'Pages.Reports.Analysis.LateEarlyLogCustom033',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/late-early-log-custom033'
                            ),
                            new AppMenuItem(
                                'LateEarlyLogCustom036Report',
                                'Pages.Reports.Analysis.LateEarlyLogCustom036',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/late-early-log-custom036'
                            ),
                            new AppMenuItem(
                                'MonthlyAttWithToleranceReport',
                                'Pages.Reports.Analysis.MonthlyAttWithTolerance',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/monthly-att-with-tolerance'
                            ),
                            new AppMenuItem(
                                'MonthlyTimeAttendanceReport',
                                'Pages.Reports.Analysis.MonthlyTimeAttendance',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/monthly-time-attendance'
                            ),
                            new AppMenuItem(
                                'TimeAnalysisByDeptReport',
                                'Pages.Reports.Analysis.TimeAnalysisByDept',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/time-analysis-by-dept'
                            ),
                            new AppMenuItem(
                                'TimeRegisterReport',
                                'Pages.Reports.Analysis.TimeRegister',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/time-register'
                            ),
                            new AppMenuItem(
                                'TimeRegisterApprovalReport',
                                'Pages.Reports.Analysis.TimeRegisterApproval',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/time-register-approval'
                            ),
                            new AppMenuItem(
                                'TimeScheduleReport',
                                'Pages.Reports.Analysis.TimeSchedule',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/time-schedule'
                            ),
                        ]
                    ),
                    new AppMenuItem(
                        'TransactionReports',
                        '',
                        'flaticon-shapes',
                        '',
                        [],
                        [
                            new AppMenuItem(
                                'EmployeeAssetInvoiceReport',
                                'Pages.Reports.Analysis.EmployeeAssetInvoice',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-asset-invoice'
                            ),
                            new AppMenuItem(
                                'EmployeeDisciplinaryReport',
                                'Pages.Reports.Analysis.EmployeeDisciplinary',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-disciplinary'
                            ),
                            new AppMenuItem(
                                'EmployeeDocumentReport',
                                'Pages.Reports.Analysis.EmployeeDocument',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-document'
                            ),
                            new AppMenuItem(
                                'EmployeeDocumentCustom033Report',
                                'Pages.Reports.Analysis.EmployeeDocumentCustom033',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-document-custom033'
                            ),
                            new AppMenuItem(
                                'EmployeeHealthReimburseReport',
                                'Pages.Reports.Analysis.EmployeeHealthReimburse',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-health-reimburse'
                            ),
                            new AppMenuItem(
                                'EmployeeLeaveSalaryReport',
                                'Pages.Reports.Analysis.EmployeeLeaveSalary',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-leave-salary'
                            ),
                            new AppMenuItem(
                                'EmployeeLoanReport',
                                'Pages.Reports.Analysis.EmployeeLoan',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-loan'
                            ),
                            new AppMenuItem(
                                'EmployeeNonPayrollReport',
                                'Pages.Reports.Analysis.EmployeeNonPayroll',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-non-payroll'
                            ),
                            new AppMenuItem(
                                'EmployeePaymentReport',
                                'Pages.Reports.Analysis.EmployeePayment',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-payment'
                            ),
                            new AppMenuItem(
                                'EmployeeSocialSecurityReport',
                                'Pages.Reports.Analysis.EmployeeSocialSecurity',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-social-security'
                            ),
                            new AppMenuItem(
                                'EmpOvertimeReport',
                                'Pages.Reports.Analysis.EmpOvertime',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/emp-overtime'
                            ),
                            new AppMenuItem(
                                'EmpOvertimeByDayReport',
                                'Pages.Reports.Analysis.EmpOvertimeByDay',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/emp-overtime-by-day'
                            ),
                            new AppMenuItem(
                                'MonthlyPayrollTransactionReport',
                                'Pages.Reports.Analysis.MonthlyPayrollTransaction',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/monthly-payroll-transaction'
                            ),
                            new AppMenuItem(
                                'NewsReport',
                                'Pages.Reports.Analysis.News',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/news'
                            ),
                        ]
                    ),
                    new AppMenuItem(
                        'QuotaProvisionReports',
                        '',
                        'flaticon-safe-shield-protection',
                        '',
                        [],
                        [
                            new AppMenuItem(
                                'AirTicketQuotaBalanceReport',
                                'Pages.Reports.Analysis.AirTicketQuotaBalance',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/air-ticket-quota-balance'
                            ),
                            new AppMenuItem(
                                'AnnualLeaveSheetReport',
                                'Pages.Reports.Analysis.AnnualLeaveSheet',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/annual-leave-sheet'
                            ),
                            new AppMenuItem(
                                'EndOfServiceReport',
                                'Pages.Reports.Analysis.EndOfService',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/end-of-service'
                            ),
                            new AppMenuItem(
                                'IndemnityProvisionReport',
                                'Pages.Reports.Analysis.IndemnityProvision',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/indemnity-provision'
                            ),
                            new AppMenuItem(
                                'IndemnityProvisionAsOnDateReport',
                                'Pages.Reports.Analysis.IndemnityProvisionAsOnDate',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/indemnity-provision-as-on-date'
                            ),
                            new AppMenuItem(
                                'LeaveProvisionReport',
                                'Pages.Reports.Analysis.LeaveProvision',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/leave-provision'
                            ),
                            new AppMenuItem(
                                'LeaveQuotaBalanceReport',
                                'Pages.Reports.Analysis.LeaveQuotaBalance',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/leave-quota-balance'
                            ),
                            new AppMenuItem(
                                'ProvisionChangeReport',
                                'Pages.Reports.Analysis.ProvisionChange',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/provision-change'
                            ),
                            new AppMenuItem(
                                'QuotaProvisionAsOnDateReport',
                                'Pages.Reports.Analysis.QuotaProvisionAsOnDate',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/quota-provision-as-on-date'
                            ),
                            new AppMenuItem(
                                'YearlyLeaveIndemnityDetailCustom016Report',
                                'Pages.Reports.Analysis.YearlyLeaveIndemnityDetailCustom016',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/yearly-leave-indemnity-detail-custom016'
                            ),
                            new AppMenuItem(
                                'YearlyLeaveIndemnityDetailCustom023Report',
                                'Pages.Reports.Analysis.YearlyLeaveIndemnityDetailCustom023',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/yearly-leave-indemnity-detail-custom023'
                            ),
                        ]
                    ),
                    new AppMenuItem(
                        'StatusReports',
                        '',
                        'flaticon-interface-8',
                        '',
                        [],
                        [
                            new AppMenuItem(
                                'EmployeeDocMovementReport',
                                'Pages.Reports.Analysis.EmployeeDocMovement',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-doc-movement'
                            ),
                            new AppMenuItem(
                                'EmployeeFnFPaymentReport',
                                'Pages.Reports.Analysis.EmployeeFnFPayment',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-fnf-payment'
                            ),
                            new AppMenuItem(
                                'EmployeeNewJoinerReport',
                                'Pages.Reports.Analysis.EmployeeNewJoiner',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-new-joiner'
                            ),
                            new AppMenuItem(
                                'EmployeeOnHoldReport',
                                'Pages.Reports.Analysis.EmployeeOnHold',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-on-hold'
                            ),
                            new AppMenuItem(
                                'EmployeePassportReport',
                                'Pages.Reports.Analysis.EmployeePassport',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-passport'
                            ),
                            new AppMenuItem(
                                'EmployeeProbationReport',
                                'Pages.Reports.Analysis.EmployeeProbation',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-probation'
                            ),
                            new AppMenuItem(
                                'EmployeeResidencyReport',
                                'Pages.Reports.Analysis.EmployeeResidency',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-residency'
                            ),
                            new AppMenuItem(
                                'EmployeeSeparationReport',
                                'Pages.Reports.Analysis.EmployeeSeparation',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-separation'
                            ),
                        ]
                    ),
                    new AppMenuItem(
                        'PerformanceReports',
                        '',
                        'flaticon-medal',
                        '',
                        [],
                        [
                            new AppMenuItem(
                                'PAEmployeeReport',
                                'Pages.Reports.Analysis.PAEmployee',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/pa-employee'
                            ),
                            new AppMenuItem(
                                'PAEmployeeScorecardReport',
                                'Pages.Reports.Analysis.PAEmployeeScorecard',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/pa-employee-scorecard'
                            ),
                            new AppMenuItem(
                                'PASummaryReport',
                                'Pages.Reports.Analysis.PASummary',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/pa-summary'
                            ),
                            new AppMenuItem(
                                'PerformanceAppraisalReport',
                                'Pages.Reports.Analysis.PerformanceAppraisal',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/performance-appraisal'
                            ),
                            new AppMenuItem(
                                'PerformanceAppraisalReviewerSummaryReport',
                                'Pages.Reports.Analysis.PerformanceAppraisalReviewerSummary',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/performance-appraisal-reviewer-summary'
                            ),
                            new AppMenuItem(
                                'PerformanceAppraisalManagerEvaluationReport',
                                'Pages.Reports.Analysis.PerformanceAppraisalManagerEvaluation',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/performance-appraisal-manager-evaluation'
                            ),
                        ]
                    ),
                    new AppMenuItem(
                        'ProjectReports',
                        '',
                        'flaticon-suitcase',
                        '',
                        [],
                        [
                            new AppMenuItem(
                                'ProjectCostingReport',
                                'Pages.Reports.Analysis.ProjectCosting',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/project-costing'
                            ),
                            new AppMenuItem(
                                'ProjectCostingCustom052Report',
                                'Pages.Reports.Analysis.ProjectCostingCustom052',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/project-costing-custom052'
                            ),
                            new AppMenuItem(
                                'ProjectMonthwiseSummaryReport',
                                'Pages.Reports.Analysis.ProjectMonthwiseSummary',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/project-monthwise-summary'
                            ),
                            new AppMenuItem(
                                'ProjectWiseAccrualReport',
                                'Pages.Reports.Analysis.ProjectWiseAccrual',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/project-wise-accrual'
                            ),
                        ]
                    ),
                    new AppMenuItem(
                        'RecruitmentTrainingReports',
                        '',
                        'flaticon-book',
                        '',
                        [],
                        [
                            new AppMenuItem(
                                'ApplicantSearchReport',
                                'Pages.Reports.Analysis.ApplicantSearch',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/applicant-search'
                            ),
                            new AppMenuItem(
                                'CandidateOnbInitReport',
                                'Pages.Reports.Analysis.CandidateOnbInit',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/candidate-onb-init'
                            ),
                            new AppMenuItem(
                                'VacancyStatusReport',
                                'Pages.Reports.Analysis.VacancyStatus',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/vacancy-status'
                            ),
                            new AppMenuItem(
                                'EmployeeTrainingReport',
                                'Pages.Reports.Analysis.EmployeeTraining',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-training'
                            ),
                            new AppMenuItem(
                                'TrainingCourseReport',
                                'Pages.Reports.Analysis.TrainingCourse',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/training-course'
                            ),
                            new AppMenuItem(
                                'TrainingScheduleReport',
                                'Pages.Reports.Analysis.TrainingSchedule',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/training-schedule'
                            ),
                            new AppMenuItem(
                                'EmployeeSurveyReport',
                                'Pages.Reports.Analysis.EmployeeSurvey',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-survey'
                            ),
                            new AppMenuItem(
                                'SurveyStatusReport',
                                'Pages.Reports.Analysis.SurveyStatus',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/survey-status'
                            ),
                        ]
                    ),
                    new AppMenuItem(
                        'AdministrationReports',
                        '',
                        'flaticon-settings',
                        '',
                        [],
                        [
                            new AppMenuItem(
                                'AuditReport',
                                'Pages.Reports.Analysis.AuditReport',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/audit-report'
                            ),
                            new AppMenuItem(
                                'ExecutionLogReport',
                                'Pages.Reports.Analysis.ExecutionLogReport',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/execution-log-report'
                            ),
                            new AppMenuItem(
                                'SponsorReport',
                                'Pages.Reports.Analysis.Sponsor',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/sponsor'
                            ),
                            new AppMenuItem(
                                'SponsorJobTitleQuotaReport',
                                'Pages.Reports.Analysis.SponsorJobTitleQuota',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/sponsor-job-title-quota'
                            ),
                            new AppMenuItem(
                                'OrgUnitTreeReport',
                                'Pages.Reports.Analysis.OrgUnitTree',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/org-unit-tree'
                            ),
                            new AppMenuItem(
                                'OrganizationDocumentReport',
                                'Pages.Reports.Analysis.OrganizationDocument',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/organization-document'
                            ),
                            new AppMenuItem(
                                'EmployeeRequestsReport',
                                'Pages.Reports.Analysis.EmployeeRequests',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-requests'
                            ),
                            new AppMenuItem(
                                'LeaveRequestAnalysisReport',
                                'Pages.Reports.Analysis.LeaveRequestAnalysis',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/leave-request-analysis'
                            ),
                        ]
                    ),
                    new AppMenuItem(
                        'OtherReports',
                        '',
                        'flaticon-folder-1',
                        '',
                        [],
                        [
                            new AppMenuItem(
                                'EmployeeAccommodationReport',
                                'Pages.Reports.Analysis.EmployeeAccommodation',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-accommodation'
                            ),
                            new AppMenuItem(
                                'AssetReport',
                                'Pages.Reports.Analysis.Asset',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/asset'
                            ),
                            new AppMenuItem(
                                'EmployeeTravelReport',
                                'Pages.Reports.Analysis.EmployeeTravel',
                                'flaticon-list-3',
                                '/app/admin/reports/analysis/employee-travel'
                            ),
                        ]
                    ),
                ]
            ),
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
                    new AppMenuItem(
                        'AssetTypes',
                        'Pages.GeneralMasterInfo.AssetTypes',
                        'flaticon-list-3',
                        '/app/admin/asset-types'
                    ),
                    new AppMenuItem('Cities', 'Pages.GeneralMasterInfo.Cities', 'flaticon-list-3', '/app/admin/cities'),
                    new AppMenuItem(
                        'Institutions',
                        'Pages.GeneralMasterInfo.Institutions',
                        'flaticon-list-3',
                        '/app/admin/institutions'
                    ),
                    new AppMenuItem(
                        'Nationalities',
                        'Pages.GeneralMasterInfo.Nationalities',
                        'flaticon-list-3',
                        '/app/admin/nationalities'
                    ),
                    new AppMenuItem(
                        'DependantTypes',
                        'Pages.GeneralMasterInfo.DependantTypes',
                        'flaticon-list-3',
                        '/app/admin/dependant-types'
                    ),
                    new AppMenuItem(
                        'Currencies',
                        'Pages.GeneralMasterInfo.Currencies',
                        'flaticon-list-3',
                        '/app/admin/currencies'
                    ),
                    new AppMenuItem(
                        'SeparationReasons',
                        'Pages.GeneralMasterInfo.SeparationReasons',
                        'flaticon-list-3',
                        '/app/admin/separation-reasons'
                    ),
                    new AppMenuItem(
                        'DisciplinaryDeductionTypes',
                        'Pages.GeneralMasterInfo.DisciplinaryDeductionTypes',
                        'flaticon-list-3',
                        '/app/admin/disciplinary-deduction-types'
                    ),
                    new AppMenuItem(
                        'PersonalActions',
                        'Pages.GeneralMasterInfo.PersonalActions',
                        'flaticon-list-3',
                        '/app/admin/personal-actions'
                    ),
                    new AppMenuItem(
                        'PersonalActionReasons',
                        'Pages.GeneralMasterInfo.PersonalActionReasons',
                        'flaticon-list-3',
                        '/app/admin/personal-action-reasons'
                    ),
                    new AppMenuItem(
                        'Countries',
                        'Pages.GeneralMasterInfo.Countries',
                        'flaticon-map-location',
                        '/app/admin/countries'
                    ),
                    new AppMenuItem(
                        'BankFiles',
                        'Pages.GeneralMasterInfo.BankFiles',
                        'flaticon-list-3',
                        '/app/admin/bank-files'
                    ),
                    new AppMenuItem(
                        'BenefitTypes',
                        'Pages.GeneralMasterInfo.BenefitTypes',
                        'flaticon-list-3',
                        '/app/admin/benefit-types'
                    ),
                    new AppMenuItem(
                        'ContractTypes',
                        'Pages.GeneralMasterInfo.ContractTypes',
                        'flaticon-list-3',
                        '/app/admin/contract-types'
                    ),
                    new AppMenuItem(
                        'DateTypes',
                        'Pages.GeneralMasterInfo.DateTypes',
                        'flaticon-list-3',
                        '/app/admin/date-types'
                    ),
                    new AppMenuItem(
                        'DocumentPlaces',
                        'Pages.GeneralMasterInfo.DocumentPlaces',
                        'flaticon-list-3',
                        '/app/admin/document-places'
                    ),
                    new AppMenuItem(
                        'Educations',
                        'Pages.GeneralMasterInfo.Educations',
                        'flaticon-list-3',
                        '/app/admin/educations'
                    ),
                    new AppMenuItem(
                        'EducationBranches',
                        'Pages.GeneralMasterInfo.EducationBranches',
                        'flaticon-list-3',
                        '/app/admin/education-branches'
                    ),
                    new AppMenuItem(
                        'Governorates',
                        'Pages.GeneralMasterInfo.Governorates',
                        'flaticon-list-3',
                        '/app/admin/governorates'
                    ),
                    new AppMenuItem(
                        'GrievanceTypes',
                        'Pages.GeneralMasterInfo.GrievanceTypes',
                        'flaticon-list-3',
                        '/app/admin/grievance-types'
                    ),
                    new AppMenuItem(
                        'HRServiceTypes',
                        'Pages.GeneralMasterInfo.HRServiceTypes',
                        'flaticon-list-3',
                        '/app/admin/hr-service-types'
                    ),
                    new AppMenuItem(
                        'ITServiceTypes',
                        'Pages.GeneralMasterInfo.ITServiceTypes',
                        'flaticon-list-3',
                        '/app/admin/it-service-types'
                    ),
                    new AppMenuItem(
                        'LanguageInfos',
                        'Pages.GeneralMasterInfo.LanguageInfos',
                        'flaticon-list-3',
                        '/app/admin/language-infos'
                    ),
                    new AppMenuItem(
                        'MaritalStatuses',
                        'Pages.GeneralMasterInfo.MaritalStatuses',
                        'flaticon-list-3',
                        '/app/admin/marital-statuses'
                    ),
                    new AppMenuItem(
                        'NationalityGroups',
                        'Pages.GeneralMasterInfo.NationalityGroups',
                        'flaticon-list-3',
                        '/app/admin/nationality-groups'
                    ),
                    new AppMenuItem(
                        'NewsTypes',
                        'Pages.GeneralMasterInfo.NewsTypes',
                        'flaticon-list-3',
                        '/app/admin/news-types'
                    ),
                    new AppMenuItem(
                        'PassportTypes',
                        'Pages.GeneralMasterInfo.PassportTypes',
                        'flaticon-list-3',
                        '/app/admin/passport-types'
                    ),
                    new AppMenuItem(
                        'PostingGroups',
                        'Pages.GeneralMasterInfo.PostingGroups',
                        'flaticon-list-3',
                        '/app/admin/posting-groups'
                    ),
                    new AppMenuItem(
                        'Qualifications',
                        'Pages.GeneralMasterInfo.Qualifications',
                        'flaticon-list-3',
                        '/app/admin/qualifications'
                    ),
                    new AppMenuItem(
                        'RecruitmentTypes',
                        'Pages.GeneralMasterInfo.RecruitmentTypes',
                        'flaticon-list-3',
                        '/app/admin/recruitment-types'
                    ),
                    new AppMenuItem(
                        'RelationshipTypes',
                        'Pages.GeneralMasterInfo.RelationshipTypes',
                        'flaticon-list-3',
                        '/app/admin/relationship-types'
                    ),
                    new AppMenuItem(
                        'Religions',
                        'Pages.GeneralMasterInfo.Religions',
                        'flaticon-list-3',
                        '/app/admin/religions'
                    ),
                    new AppMenuItem('States', 'Pages.GeneralMasterInfo.States', 'flaticon-list-3', '/app/admin/states'),
                    new AppMenuItem(
                        'TaskTypes',
                        'Pages.GeneralMasterInfo.TaskTypes',
                        'flaticon-list-3',
                        '/app/admin/task-types'
                    ),
                    new AppMenuItem('Titles', 'Pages.GeneralMasterInfo.Titles', 'flaticon-list-3', '/app/admin/titles'),
                    new AppMenuItem(
                        'TrainingTypes',
                        'Pages.GeneralMasterInfo.TrainingTypes',
                        'flaticon-list-3',
                        '/app/admin/training-types'
                    ),
                    new AppMenuItem(
                        'Universities',
                        'Pages.GeneralMasterInfo.Universities',
                        'flaticon-list-3',
                        '/app/admin/universities'
                    ),
                    new AppMenuItem(
                        'VisaTypes',
                        'Pages.GeneralMasterInfo.VisaTypes',
                        'flaticon-list-3',
                        '/app/admin/visa-types'
                    ),
                    new AppMenuItem(
                        'WorkVisaTypes',
                        'Pages.GeneralMasterInfo.WorkVisaTypes',
                        'flaticon-list-3',
                        '/app/admin/work-visa-types'
                    ),
                    new AppMenuItem(
                        'BudgetingGroups',
                        'Pages.GeneralMasterInfo.BudgetingGroups',
                        'flaticon-list-3',
                        '/app/admin/budgeting-groups'
                    ),
                    new AppMenuItem(
                        'ClearanceRequestTypes',
                        'Pages.GeneralMasterInfo.ClearanceRequestTypes',
                        'flaticon-list-3',
                        '/app/admin/clearance-request-types'
                    ),
                    new AppMenuItem('Banks', 'Pages.GeneralMasterInfo.Banks', 'flaticon-list-3', '/app/admin/banks'),
                    new AppMenuItem(
                        'PayeeBankDetails',
                        'Pages.GeneralMasterInfo.PayeeBankDetails',
                        'flaticon-list-3',
                        '/app/admin/payee-bank-details'
                    ),
                    new AppMenuItem(
                        'BudgetingGroupRateDetails',
                        'Pages.GeneralMasterInfo.BudgetingGroupRateDetails',
                        'flaticon-list-3',
                        '/app/admin/budgeting-group-rate-details'
                    ),
                    new AppMenuItem(
                        'ClearanceRequestFields',
                        'Pages.GeneralMasterInfo.ClearanceRequestFields',
                        'flaticon-list-3',
                        '/app/admin/clearance-request-fields'
                    ),
                    new AppMenuItem(
                        'ClearanceRequestFieldDetails',
                        'Pages.GeneralMasterInfo.ClearanceRequestFieldDetails',
                        'flaticon-list-3',
                        '/app/admin/clearance-request-field-details'
                    ),
                    new AppMenuItem(
                        'DisciplinaryTypes',
                        'Pages.GeneralMasterInfo.DisciplinaryTypes',
                        'flaticon-list-3',
                        '/app/admin/disciplinary-types'
                    ),
                    new AppMenuItem(
                        'DisciplinaryTypeDetails',
                        'Pages.GeneralMasterInfo.DisciplinaryTypeDetails',
                        'flaticon-list-3',
                        '/app/admin/disciplinary-type-details'
                    ),
                    new AppMenuItem(
                        'NotificationConfigs',
                        'Pages.GeneralMasterInfo.NotificationConfigs',
                        'flaticon-list-3',
                        '/app/admin/notification-configs'
                    ),
                    new AppMenuItem(
                        'NotificationGroups',
                        'Pages.GeneralMasterInfo.NotificationGroups',
                        'flaticon-list-3',
                        '/app/admin/notification-groups'
                    ),
                    new AppMenuItem(
                        'DocumentTypes',
                        'Pages.GeneralMasterInfo.DocumentTypes',
                        'flaticon-list-3',
                        '/app/admin/document-types'
                    ),
                    new AppMenuItem(
                        'DocumentPrintForms',
                        'Pages.GeneralMasterInfo.DocumentPrintForms',
                        'flaticon-list-3',
                        '/app/admin/document-print-forms'
                    ),
                    new AppMenuItem(
                        'LoanTypes',
                        'Pages.GeneralMasterInfo.LoanTypes',
                        'flaticon-list-3',
                        '/app/admin/loan-types'
                    ),
                    new AppMenuItem(
                        'LoanTypeDetails',
                        'Pages.GeneralMasterInfo.LoanTypeDetails',
                        'flaticon-list-3',
                        '/app/admin/loan-type-details'
                    ),
                    new AppMenuItem(
                        'OvertimeTypes',
                        'Pages.GeneralMasterInfo.OvertimeTypes',
                        'flaticon-list-3',
                        '/app/admin/overtime-types'
                    ),
                    new AppMenuItem(
                        'OvertimeTypeDetails',
                        'Pages.GeneralMasterInfo.OvertimeTypeDetails',
                        'flaticon-list-3',
                        '/app/admin/overtime-type-details'
                    ),
                    new AppMenuItem('Assets', 'Pages.GeneralMasterInfo.Assets', 'flaticon-list-3', '/app/admin/assets'),
                    new AppMenuItem(
                        'NumberSeries',
                        'Pages.GeneralMasterInfo.NumberSeries',
                        'flaticon-list-3',
                        '/app/admin/number-series'
                    ),
                    new AppMenuItem(
                        'CurrencyConvertors',
                        'Pages.GeneralMasterInfo.CurrencyConvertors',
                        'flaticon-list-3',
                        '/app/admin/currency-convertors'
                    ),
                ]
            ),
            new AppMenuItem(
                'OrganizationManagement',
                '',
                'flaticon-list-3',
                '',
                [],
                [
                    new AppMenuItem(
                        'CostCenterGroups',
                        'Pages.OrganizationManagement.CostCenterGroups',
                        'flaticon-list-3',
                        '/app/admin/cost-center-groups'
                    ),
                    new AppMenuItem(
                        'CostElementTypes',
                        'Pages.OrganizationManagement.CostElementTypes',
                        'flaticon-list-3',
                        '/app/admin/cost-element-types'
                    ),
                    new AppMenuItem(
                        'JobTitles',
                        'Pages.OrganizationManagement.JobTitles',
                        'flaticon-list-3',
                        '/app/admin/job-titles'
                    ),
                    new AppMenuItem(
                        'CostCenters',
                        'Pages.OrganizationManagement.CostCenters',
                        'flaticon-list-3',
                        '/app/admin/cost-centers'
                    ),
                    new AppMenuItem(
                        'CostElements',
                        'Pages.OrganizationManagement.CostElements',
                        'flaticon-list-3',
                        '/app/admin/cost-elements'
                    ),
                    new AppMenuItem(
                        'OrganizationUnitTypes',
                        'Pages.OrganizationManagement.OrganizationUnitTypes',
                        'flaticon-list-3',
                        '/app/admin/organization-unit-types'
                    ),
                    new AppMenuItem(
                        'WorkLocations',
                        'Pages.OrganizationManagement.WorkLocations',
                        'flaticon-list-3',
                        '/app/admin/work-locations'
                    ),
                    new AppMenuItem(
                        'OrganizationBankInfo',
                        'Pages.OrganizationManagement.OrganizationBankInfos',
                        'flaticon-list-3',
                        '/app/admin/organization-bank-infos'
                    ),
                    new AppMenuItem(
                        'OrgUnits',
                        'Pages.OrganizationManagement.OrgUnits',
                        'flaticon-list-3',
                        '/app/admin/org-units'
                    ),
                    new AppMenuItem(
                        'OrganizationChart',
                        'Pages.OrganizationManagement.CompanyStructureChart',
                        'flaticon-network',
                        '/app/admin/org-charts/organization'
                    ),
                ]
            ),
            new AppMenuItem(
                'TimeManagement',
                '',
                'flaticon-time',
                '',
                [],
                [
                    new AppMenuItem(
                        'TimeTypes',
                        'Pages.TimeManagement.TimeTypes',
                        'flaticon-list-3',
                        '/app/admin/time-types'
                    ),
                    new AppMenuItem(
                        'TransTypes',
                        'Pages.TimeManagement.TransTypes',
                        'flaticon-list-3',
                        '/app/admin/trans-types'
                    ),
                    new AppMenuItem(
                        'TimeTransferTypes',
                        'Pages.TimeManagement.TimeTransferTypes',
                        'flaticon-list-3',
                        '/app/admin/time-transfer-types'
                    ),
                    new AppMenuItem(
                        'BreakShifts',
                        'Pages.TimeManagement.BreakShifts',
                        'flaticon-list-3',
                        '/app/admin/break-shifts'
                    ),
                    new AppMenuItem('Duties', 'Pages.TimeManagement.Duties', 'flaticon-list-3', '/app/admin/duties'),
                    new AppMenuItem(
                        'DailyDuties',
                        'Pages.TimeManagement.DailyDuties',
                        'flaticon-list-3',
                        '/app/admin/daily-duties'
                    ),
                    new AppMenuItem(
                        'HolidayCalendars',
                        'Pages.TimeManagement.HolidayCalendars',
                        'flaticon-list-3',
                        '/app/admin/holiday-calendars'
                    ),
                    new AppMenuItem(
                        'Holidays',
                        'Pages.TimeManagement.Holidays',
                        'flaticon-list-3',
                        '/app/admin/holidays'
                    ),
                    new AppMenuItem(
                        'HolidayPeriods',
                        'Pages.TimeManagement.HolidayPeriods',
                        'flaticon-list-3',
                        '/app/admin/holiday-periods'
                    ),
                    new AppMenuItem(
                        'TimePolicies',
                        'Pages.TimeManagement.TimePolicies',
                        'flaticon-list-3',
                        '/app/admin/time-policies'
                    ),
                    new AppMenuItem(
                        'DutyPeriods',
                        'Pages.TimeManagement.DutyPeriods',
                        'flaticon-list-3',
                        '/app/admin/duty-periods'
                    ),
                    new AppMenuItem(
                        'DutyPeriodDetails',
                        'Pages.TimeManagement.DutyPeriodDetails',
                        'flaticon-list-3',
                        '/app/admin/duty-period-details'
                    ),
                    new AppMenuItem(
                        'RosterDutyPeriods',
                        'Pages.TimeManagement.RosterDutyPeriods',
                        'flaticon-list-3',
                        '/app/admin/roster-duty-periods'
                    ),
                    new AppMenuItem(
                        'RosterDutyPeriodDetails',
                        'Pages.TimeManagement.RosterDutyPeriodDetails',
                        'flaticon-list-3',
                        '/app/admin/roster-duty-period-details'
                    ),
                    new AppMenuItem(
                        'RosterDutySchedules',
                        'Pages.TimeManagement.RosterDutySchedules',
                        'flaticon-list-3',
                        '/app/admin/roster-duty-schedules'
                    ),
                    new AppMenuItem(
                        'RosterGroups',
                        'Pages.TimeManagement.RosterGroups',
                        'flaticon-list-3',
                        '/app/admin/roster-groups'
                    ),
                    new AppMenuItem(
                        'RosterGroupDetails',
                        'Pages.TimeManagement.RosterGroupDetails',
                        'flaticon-list-3',
                        '/app/admin/roster-group-details'
                    ),
                    new AppMenuItem(
                        'RosterGroupSchedules',
                        'Pages.TimeManagement.RosterGroupSchedules',
                        'flaticon-list-3',
                        '/app/admin/roster-group-schedules'
                    ),
                    new AppMenuItem(
                        'RosterDailyDuties',
                        'Pages.TimeManagement.RosterDailyDuties',
                        'flaticon-list-3',
                        '/app/admin/roster-daily-duties'
                    ),
                    new AppMenuItem(
                        'RosterScheduleViewer',
                        'Pages.TimeManagement.RosterScheduleViewer',
                        'flaticon-calendar-3',
                        '/app/admin/roster-schedule-viewer'
                    ),
                    new AppMenuItem(
                        'WorkLocationDutyPeriods',
                        'Pages.TimeManagement.WorkLocationDutyPeriods',
                        'flaticon-list-3',
                        '/app/admin/work-location-duty-periods'
                    ),
                    new AppMenuItem(
                        'TimeEvents',
                        'Pages.TimeManagement.TimeEvents',
                        'flaticon-list-3',
                        '/app/admin/time-events'
                    ),
                    new AppMenuItem(
                        'TimeAttAPIs',
                        'Pages.TimeManagement.TimeAttAPIs',
                        'flaticon-list-3',
                        '/app/admin/time-att-apis'
                    ),
                    new AppMenuItem(
                        'TimeLogs',
                        'Pages.TimeManagement.TimeLogs',
                        'flaticon-list-3',
                        '/app/admin/time-logs'
                    ),
                    new AppMenuItem(
                        'TimeRegisters',
                        'Pages.TimeManagement.TimeRegisters',
                        'flaticon-list-3',
                        '/app/admin/time-registers'
                    ),
                    new AppMenuItem(
                        'MonthlyTimeRegisters',
                        'Pages.TimeManagement.MonthlyTimeRegisters',
                        'flaticon-list-3',
                        '/app/admin/monthly-time-registers'
                    ),
                    new AppMenuItem(
                        'MonthlyTimeRegisterDetails',
                        'Pages.TimeManagement.MonthlyTimeRegisterDetails',
                        'flaticon-list-3',
                        '/app/admin/monthly-time-register-details'
                    ),
                    new AppMenuItem(
                        'YearlyTimeRegisters',
                        'Pages.TimeManagement.YearlyTimeRegisters',
                        'flaticon-list-3',
                        '/app/admin/yearly-time-registers'
                    ),
                    new AppMenuItem(
                        'YearlyTimeRegisterDetails',
                        'Pages.TimeManagement.YearlyTimeRegisterDetails',
                        'flaticon-list-3',
                        '/app/admin/yearly-time-register-details'
                    ),
                    new AppMenuItem(
                        'SimulationTimeRegisters',
                        'Pages.TimeManagement.SimulationTimeRegisters',
                        'flaticon-list-3',
                        '/app/admin/simulation-time-registers'
                    ),
                    new AppMenuItem(
                        'SimulationTimeRegisterDetails',
                        'Pages.TimeManagement.SimulationTimeRegisterDetails',
                        'flaticon-list-3',
                        '/app/admin/simulation-time-register-details'
                    ),
                    new AppMenuItem(
                        'TimeTransfers',
                        'Pages.TimeManagement.TimeTransfers',
                        'flaticon-list-3',
                        '/app/admin/time-transfers'
                    ),
                    new AppMenuItem(
                        'TimeEngine',
                        'Pages.TimeManagement.TimeEngine',
                        'flaticon-settings',
                        '/app/admin/time-engine'
                    ),
                ]
            ),
            new AppMenuItem(
                'PayrollManagement',
                '',
                'flaticon-coins',
                '',
                [],
                [
                    new AppMenuItem(
                        'PayrollEngine',
                        'Pages.PayrollManagement.PayrollEngine',
                        'flaticon-settings',
                        '/app/admin/payroll-engine'
                    ),
                    new AppMenuItem(
                        'AttendanceTypes',
                        'Pages.PayrollManagement.AttendanceTypes',
                        'flaticon-list-3',
                        '/app/admin/attendance-types'
                    ),
                    new AppMenuItem(
                        'AccountTypes',
                        'Pages.PayrollManagement.AccountTypes',
                        'flaticon-list-3',
                        '/app/admin/account-types'
                    ),
                    new AppMenuItem(
                        'AccountInfos',
                        'Pages.PayrollManagement.AccountInfos',
                        'flaticon-list-3',
                        '/app/admin/account-infos'
                    ),
                    new AppMenuItem(
                        'PayrollReasonTypes',
                        'Pages.PayrollManagement.PayrollReasonTypes',
                        'flaticon-list-3',
                        '/app/admin/payroll-reason-types'
                    ),
                    new AppMenuItem(
                        'TicketClasses',
                        'Pages.PayrollManagement.TicketClasses',
                        'flaticon-list-3',
                        '/app/admin/ticket-classes'
                    ),
                    new AppMenuItem(
                        'TicketPaxTypes',
                        'Pages.PayrollManagement.TicketPaxTypes',
                        'flaticon-list-3',
                        '/app/admin/ticket-pax-types'
                    ),
                    new AppMenuItem(
                        'TicketRoutes',
                        'Pages.PayrollManagement.TicketRoutes',
                        'flaticon-list-3',
                        '/app/admin/ticket-routes'
                    ),
                    new AppMenuItem(
                        'AbsenceTypes',
                        'Pages.PayrollManagement.AbsenceTypes',
                        'flaticon-list-3',
                        '/app/admin/absence-types'
                    ),
                    new AppMenuItem(
                        'AbsenceDeductionRules',
                        'Pages.PayrollManagement.AbsenceDeductionRules',
                        'flaticon-list-3',
                        '/app/admin/absence-deduction-rules'
                    ),
                    new AppMenuItem(
                        'AbsenceViolationRules',
                        'Pages.PayrollManagement.AbsenceViolationRules',
                        'flaticon-list-3',
                        '/app/admin/absence-violation-rules'
                    ),
                    new AppMenuItem(
                        'AbsenceViolationRuleDetails',
                        'Pages.PayrollManagement.AbsenceViolationRuleDetails',
                        'flaticon-list-3',
                        '/app/admin/absence-violation-rule-details'
                    ),
                    new AppMenuItem(
                        'AbsenceViolationRuleExceptions',
                        'Pages.PayrollManagement.AbsenceViolationRuleExceptions',
                        'flaticon-list-3',
                        '/app/admin/absence-violation-rule-exceptions'
                    ),
                    new AppMenuItem(
                        'AbsenceViolationActionRecords',
                        'Pages.PayrollManagement.AbsenceViolationActionRecords',
                        'flaticon-list-3',
                        '/app/admin/absence-violation-action-records'
                    ),
                    new AppMenuItem(
                        'AbsenceViolationActionLogs',
                        'Pages.PayrollManagement.AbsenceViolationActionLogs',
                        'flaticon-list-3',
                        '/app/admin/absence-violation-action-logs'
                    ),
                    new AppMenuItem(
                        'EmployeeGroups',
                        'Pages.PayrollManagement.EmployeeGroups',
                        'flaticon-list-3',
                        '/app/admin/employee-groups'
                    ),
                    new AppMenuItem(
                        'EmployeeSubGroups',
                        'Pages.PayrollManagement.EmployeeSubGroups',
                        'flaticon-list-3',
                        '/app/admin/employee-sub-groups'
                    ),
                    new AppMenuItem(
                        'PayrollAreas',
                        'Pages.PayrollManagement.PayrollAreas',
                        'flaticon-list-3',
                        '/app/admin/payroll-areas'
                    ),
                    new AppMenuItem(
                        'CostingFile',
                        'Pages.PayrollManagement.CostingFile',
                        'flaticon-line-graph',
                        '/app/admin/costing-file'
                    ),
                    new AppMenuItem(
                        'PostJv',
                        'Pages.PayrollManagement.PostJv',
                        'flaticon-piggy-bank',
                        '/app/admin/post-jv'
                    ),
                    // Legacy HCM10.Web/UserControls/ImportExport/ -- the screen is cross-module (it can
                    // import ANY entity), but it is placed here so it sits next to the other operator
                    // "run something" screens rather than needing a new top-level group.
                    new AppMenuItem(
                        'BulkImportExport',
                        'Pages.ImportExport.BulkImportExport',
                        'flaticon-download',
                        '/app/admin/bulk-import-export'
                    ),
                    new AppMenuItem(
                        'BankFile',
                        'Pages.PayrollManagement.BankFiles',
                        'flaticon-piggy-bank',
                        '/app/admin/bank-file'
                    ),
                    new AppMenuItem(
                        'PayrollCalendars',
                        'Pages.PayrollManagement.PayrollCalendars',
                        'flaticon-list-3',
                        '/app/admin/payroll-calendars'
                    ),
                    new AppMenuItem(
                        'PayrollPeriods',
                        'Pages.PayrollManagement.PayrollPeriods',
                        'flaticon-list-3',
                        '/app/admin/payroll-periods'
                    ),
                    new AppMenuItem(
                        'PayrollControlRecords',
                        'Pages.PayrollManagement.PayrollControlRecords',
                        'flaticon-list-3',
                        '/app/admin/payroll-control-records'
                    ),
                    new AppMenuItem(
                        'WageTypes',
                        'Pages.PayrollManagement.WageTypes',
                        'flaticon-list-3',
                        '/app/admin/wage-types'
                    ),
                    new AppMenuItem(
                        'WageAccGroups',
                        'Pages.PayrollManagement.WageAccGroups',
                        'flaticon-list-3',
                        '/app/admin/wage-acc-groups'
                    ),
                    new AppMenuItem(
                        'WageAccGroupDetails',
                        'Pages.PayrollManagement.WageAccGroupDetails',
                        'flaticon-list-3',
                        '/app/admin/wage-acc-group-details'
                    ),
                    new AppMenuItem(
                        'WageAccounts',
                        'Pages.PayrollManagement.WageAccounts',
                        'flaticon-list-3',
                        '/app/admin/wage-accounts'
                    ),
                    new AppMenuItem(
                        'WageTypeRegisters',
                        'Pages.PayrollManagement.WageTypeRegisters',
                        'flaticon-list-3',
                        '/app/admin/wage-type-registers'
                    ),
                    new AppMenuItem(
                        'IndemnityPayRules',
                        'Pages.PayrollManagement.IndemnityPayRules',
                        'flaticon-list-3',
                        '/app/admin/indemnity-pay-rules'
                    ),
                    new AppMenuItem(
                        'IndemnityPayRuleDetails',
                        'Pages.PayrollManagement.IndemnityPayRuleDetails',
                        'flaticon-list-3',
                        '/app/admin/indemnity-pay-rule-details'
                    ),
                    new AppMenuItem(
                        'OrgUnitWageAccGroups',
                        'Pages.PayrollManagement.OrgUnitWageAccGroups',
                        'flaticon-list-3',
                        '/app/admin/org-unit-wage-acc-groups'
                    ),
                    new AppMenuItem(
                        'SocialSecurities',
                        'Pages.PayrollManagement.SocialSecurities',
                        'flaticon-list-3',
                        '/app/admin/social-securities'
                    ),
                    new AppMenuItem(
                        'SocialSecurityDetails',
                        'Pages.PayrollManagement.SocialSecurityDetails',
                        'flaticon-list-3',
                        '/app/admin/social-security-details'
                    ),
                    new AppMenuItem(
                        'SocialSecurityMaritalAllowances',
                        'Pages.PayrollManagement.SocialSecurityMaritalAllowances',
                        'flaticon-list-3',
                        '/app/admin/social-security-marital-allowances'
                    ),
                    new AppMenuItem(
                        'SocialSecurityEducationAllowances',
                        'Pages.PayrollManagement.SocialSecurityEducationAllowances',
                        'flaticon-list-3',
                        '/app/admin/social-security-education-allowances'
                    ),
                    new AppMenuItem(
                        'SocialSecurityRules',
                        'Pages.PayrollManagement.SocialSecurityRules',
                        'flaticon-list-3',
                        '/app/admin/social-security-rules'
                    ),
                    new AppMenuItem(
                        'TicketTypes',
                        'Pages.PayrollManagement.TicketTypes',
                        'flaticon-list-3',
                        '/app/admin/ticket-types'
                    ),
                    new AppMenuItem(
                        'TicketFares',
                        'Pages.PayrollManagement.TicketFares',
                        'flaticon-list-3',
                        '/app/admin/ticket-fares'
                    ),
                    new AppMenuItem(
                        'TicketAccrualSummaries',
                        'Pages.PayrollManagement.TicketAccrualSummaries',
                        'flaticon-list-3',
                        '/app/admin/ticket-accrual-summaries'
                    ),
                    new AppMenuItem(
                        'TempBenefitTypes',
                        'Pages.PayrollManagement.TempBenefitTypes',
                        'flaticon-list-3',
                        '/app/admin/temp-benefit-types'
                    ),
                    new AppMenuItem(
                        'TempEmployeeBenefits',
                        'Pages.PayrollManagement.TempEmployeeBenefits',
                        'flaticon-list-3',
                        '/app/admin/temp-employee-benefits'
                    ),
                    new AppMenuItem(
                        'PayrollLogs',
                        'Pages.PayrollManagement.PayrollLogs',
                        'flaticon-list-3',
                        '/app/admin/payroll-logs'
                    ),
                    new AppMenuItem(
                        'CostRegisters',
                        'Pages.PayrollManagement.CostRegisters',
                        'flaticon-list-3',
                        '/app/admin/cost-registers'
                    ),
                    new AppMenuItem(
                        'CostRegisterDetails',
                        'Pages.PayrollManagement.CostRegisterDetails',
                        'flaticon-list-3',
                        '/app/admin/cost-register-details'
                    ),
                    new AppMenuItem(
                        'SalaryRegisters',
                        'Pages.PayrollManagement.SalaryRegisters',
                        'flaticon-list-3',
                        '/app/admin/salary-registers'
                    ),
                    new AppMenuItem(
                        'SalaryRegisterDetails',
                        'Pages.PayrollManagement.SalaryRegisterDetails',
                        'flaticon-list-3',
                        '/app/admin/salary-register-details'
                    ),
                    new AppMenuItem(
                        'SalaryRegisterDaysDetails',
                        'Pages.PayrollManagement.SalaryRegisterDaysDetails',
                        'flaticon-list-3',
                        '/app/admin/salary-register-days-details'
                    ),
                    new AppMenuItem(
                        'SimulationSalaryRegisters',
                        'Pages.PayrollManagement.SimulationSalaryRegisters',
                        'flaticon-list-3',
                        '/app/admin/simulation-salary-registers'
                    ),
                    new AppMenuItem(
                        'SimulationSalaryRegisterDetails',
                        'Pages.PayrollManagement.SimulationSalaryRegisterDetails',
                        'flaticon-list-3',
                        '/app/admin/simulation-salary-register-details'
                    ),
                    new AppMenuItem(
                        'SimulationSalaryRegisterDaysDetails',
                        'Pages.PayrollManagement.SimulationSalaryRegisterDaysDetails',
                        'flaticon-list-3',
                        '/app/admin/simulation-salary-register-days-details'
                    ),
                    new AppMenuItem(
                        'GratuityAccrualSummaries',
                        'Pages.PayrollManagement.GratuityAccrualSummaries',
                        'flaticon-list-3',
                        '/app/admin/gratuity-accrual-summaries'
                    ),
                    new AppMenuItem(
                        'LeaveAccrualSummaries',
                        'Pages.PayrollManagement.LeaveAccrualSummaries',
                        'flaticon-list-3',
                        '/app/admin/leave-accrual-summaries'
                    ),
                ]
            ),
            new AppMenuItem(
                'JobProfileManagement',
                '',
                'flaticon-suitcase',
                '',
                [],
                [
                    new AppMenuItem(
                        'JobFamilies',
                        'Pages.JobProfileManagement.JobFamilies',
                        'flaticon-list-3',
                        '/app/admin/job-families'
                    ),
                    new AppMenuItem(
                        'JobFunctions',
                        'Pages.JobProfileManagement.JobFunctions',
                        'flaticon-list-3',
                        '/app/admin/job-functions'
                    ),
                    new AppMenuItem(
                        'JobResponsibilities',
                        'Pages.JobProfileManagement.JobResponsibilities',
                        'flaticon-list-3',
                        '/app/admin/job-responsibilities'
                    ),
                    new AppMenuItem(
                        'EducationCertificates',
                        'Pages.JobProfileManagement.EducationCertificates',
                        'flaticon-list-3',
                        '/app/admin/education-certificates'
                    ),
                    new AppMenuItem(
                        'EducationDegrees',
                        'Pages.JobProfileManagement.EducationDegrees',
                        'flaticon-list-3',
                        '/app/admin/education-degrees'
                    ),
                    new AppMenuItem(
                        'InterviewQuestions',
                        'Pages.JobProfileManagement.InterviewQuestions',
                        'flaticon-list-3',
                        '/app/admin/interview-questions'
                    ),
                    new AppMenuItem(
                        'Skills',
                        'Pages.JobProfileManagement.Skills',
                        'flaticon-list-3',
                        '/app/admin/skills'
                    ),
                    new AppMenuItem(
                        'JobDescriptions',
                        'Pages.JobProfileManagement.JobDescriptions',
                        'flaticon-list-3',
                        '/app/admin/job-descriptions'
                    ),
                    new AppMenuItem(
                        'JobDescriptionAcknowledgements',
                        'Pages.JobProfileManagement.JobDescriptionAcknowledgements',
                        'flaticon-list-3',
                        '/app/admin/job-description-acknowledgements'
                    ),
                ]
            ),
            new AppMenuItem(
                'GatePassManagement',
                '',
                'flaticon-security',
                '',
                [],
                [
                    new AppMenuItem(
                        'GatePassTypes',
                        'Pages.GatePassManagement.GatePassTypes',
                        'flaticon-list-3',
                        '/app/admin/gate-pass-types'
                    ),
                    new AppMenuItem(
                        'GatePassMovements',
                        'Pages.GatePassManagement.GatePassMovements',
                        'flaticon-list-3',
                        '/app/admin/gate-pass-movements'
                    ),
                    new AppMenuItem(
                        'GatePassStatuses',
                        'Pages.GatePassManagement.GatePassStatuses',
                        'flaticon-list-3',
                        '/app/admin/gate-pass-statuses'
                    ),
                    new AppMenuItem(
                        'PersonTypes',
                        'Pages.GatePassManagement.PersonTypes',
                        'flaticon-list-3',
                        '/app/admin/person-types'
                    ),
                    new AppMenuItem(
                        'VehicleTypes',
                        'Pages.GatePassManagement.VehicleTypes',
                        'flaticon-list-3',
                        '/app/admin/vehicle-types'
                    ),
                    new AppMenuItem(
                        'VehicleBrands',
                        'Pages.GatePassManagement.VehicleBrands',
                        'flaticon-list-3',
                        '/app/admin/vehicle-brands'
                    ),
                    new AppMenuItem(
                        'VehicleModels',
                        'Pages.GatePassManagement.VehicleModels',
                        'flaticon-list-3',
                        '/app/admin/vehicle-models'
                    ),
                    new AppMenuItem(
                        'GatePassVendors',
                        'Pages.GatePassManagement.GatePassVendors',
                        'flaticon-list-3',
                        '/app/admin/gate-pass-vendors'
                    ),
                    new AppMenuItem(
                        'GatePasses',
                        'Pages.GatePassManagement.GatePasses',
                        'flaticon-list-3',
                        '/app/admin/gate-passes'
                    ),
                    new AppMenuItem(
                        'PoolCars',
                        'Pages.GatePassManagement.PoolCars',
                        'flaticon-list-3',
                        '/app/admin/pool-cars'
                    ),
                    new AppMenuItem(
                        'EmployeePoolCars',
                        'Pages.GatePassManagement.EmployeePoolCars',
                        'flaticon-list-3',
                        '/app/admin/employee-pool-cars'
                    ),
                ]
            ),
            new AppMenuItem(
                'SuccessionManagement',
                '',
                'flaticon-network',
                '',
                [],
                [
                    new AppMenuItem(
                        'ImpactOfLosses',
                        'Pages.SuccessionManagement.ImpactOfLosses',
                        'flaticon-list-3',
                        '/app/admin/impact-of-losses'
                    ),
                    new AppMenuItem(
                        'NineBoxRatings',
                        'Pages.SuccessionManagement.NineBoxRatings',
                        'flaticon-list-3',
                        '/app/admin/nine-box-ratings'
                    ),
                    new AppMenuItem(
                        'Rankings',
                        'Pages.SuccessionManagement.Rankings',
                        'flaticon-list-3',
                        '/app/admin/rankings'
                    ),
                    new AppMenuItem(
                        'Readinesses',
                        'Pages.SuccessionManagement.Readinesses',
                        'flaticon-list-3',
                        '/app/admin/readinesses'
                    ),
                    new AppMenuItem(
                        'ReasonForLeavings',
                        'Pages.SuccessionManagement.ReasonForLeavings',
                        'flaticon-list-3',
                        '/app/admin/reason-for-leavings'
                    ),
                    new AppMenuItem(
                        'RiskOfLosses',
                        'Pages.SuccessionManagement.RiskOfLosses',
                        'flaticon-list-3',
                        '/app/admin/risk-of-losses'
                    ),
                    new AppMenuItem(
                        'SuccessionRatings',
                        'Pages.SuccessionManagement.SuccessionRatings',
                        'flaticon-list-3',
                        '/app/admin/succession-ratings'
                    ),
                    new AppMenuItem(
                        'TalentPools',
                        'Pages.SuccessionManagement.TalentPools',
                        'flaticon-list-3',
                        '/app/admin/talent-pools'
                    ),
                    new AppMenuItem(
                        'NineBoxMappings',
                        'Pages.SuccessionManagement.NineBoxMappings',
                        'flaticon-list-3',
                        '/app/admin/nine-box-mappings'
                    ),
                    new AppMenuItem(
                        'SuccessorNominees',
                        'Pages.SuccessionManagement.SuccessorNominees',
                        'flaticon-list-3',
                        '/app/admin/successor-nominees'
                    ),
                ]
            ),
            new AppMenuItem(
                'PersonnelAdministration',
                '',
                'flaticon-user',
                '',
                [],
                [
                    new AppMenuItem(
                        'Employees',
                        'Pages.PersonnelAdministration.Employees',
                        'flaticon-user',
                        '/app/admin/employees'
                    ),
                    new AppMenuItem(
                        'EmployeeChart',
                        'Pages.PersonnelAdministration.EmployeeOrgChart',
                        'flaticon-network',
                        '/app/admin/org-charts/employee'
                    ),
                    new AppMenuItem(
                        'Announcements',
                        'Pages.PersonnelAdministration.News',
                        'flaticon-paper-plane',
                        '/app/admin/announcements'
                    ),
                    new AppMenuItem(
                        'EmployeeLoanExemptions',
                        'Pages.PersonnelAdministration.EmployeeLoanExemptions',
                        'flaticon-list-3',
                        '/app/admin/employee-loan-exemptions'
                    ),
                    new AppMenuItem(
                        'EmployeeLoanRecoveries',
                        'Pages.PersonnelAdministration.EmployeeLoanRecoveries',
                        'flaticon-list-3',
                        '/app/admin/employee-loan-recoveries'
                    ),
                    new AppMenuItem(
                        'LoanInstallments',
                        'Pages.PersonnelAdministration.LoanInstallments',
                        'flaticon-list-3',
                        '/app/admin/loan-installments'
                    ),
                    new AppMenuItem(
                        'EmployeeFundDetails',
                        'Pages.PersonnelAdministration.EmployeeFundDetails',
                        'flaticon-list-3',
                        '/app/admin/employee-fund-details'
                    ),
                    new AppMenuItem(
                        'EmployeeFundExemptions',
                        'Pages.PersonnelAdministration.EmployeeFundExemptions',
                        'flaticon-list-3',
                        '/app/admin/employee-fund-exemptions'
                    ),
                    new AppMenuItem(
                        'EmployeeDependantExceptions',
                        'Pages.PersonnelAdministration.EmployeeDependantExceptions',
                        'flaticon-list-3',
                        '/app/admin/employee-dependant-exceptions'
                    ),
                ]
            ),
            new AppMenuItem(
                'GovtRelationsManagement',
                '',
                'flaticon-map',
                '',
                [],
                [
                    new AppMenuItem(
                        'BusinessTypes',
                        'Pages.GovtRelationsManagement.BusinessTypes',
                        'flaticon-list-3',
                        '/app/admin/business-types'
                    ),
                    new AppMenuItem(
                        'GovtJobCategories',
                        'Pages.GovtRelationsManagement.GovtJobCategories',
                        'flaticon-list-3',
                        '/app/admin/govt-job-categories'
                    ),
                    new AppMenuItem(
                        'GovtJobTitles',
                        'Pages.GovtRelationsManagement.GovtJobTitles',
                        'flaticon-list-3',
                        '/app/admin/govt-job-titles'
                    ),
                    new AppMenuItem(
                        'Signatories',
                        'Pages.GovtRelationsManagement.Signatories',
                        'flaticon-list-3',
                        '/app/admin/signatories'
                    ),
                    new AppMenuItem(
                        'Sponsors',
                        'Pages.GovtRelationsManagement.Sponsors',
                        'flaticon-list-3',
                        '/app/admin/sponsors'
                    ),
                    new AppMenuItem(
                        'OrganizationDocuments',
                        'Pages.GovtRelationsManagement.OrganizationDocuments',
                        'flaticon-list-3',
                        '/app/admin/organization-documents'
                    ),
                ]
            ),
            new AppMenuItem(
                'Recruitment',
                '',
                'flaticon-questions-circular-button',
                '',
                [],
                [
                    new AppMenuItem(
                        'Applicants',
                        'Pages.Recruitment.Applicants',
                        'flaticon-list-3',
                        '/app/admin/applicants'
                    ),
                    new AppMenuItem(
                        'Vacancies',
                        'Pages.Recruitment.Vacancies',
                        'flaticon-list-3',
                        '/app/admin/vacancies'
                    ),
                    new AppMenuItem(
                        'ApplicationStatuses',
                        'Pages.Recruitment.ApplicationStatuses',
                        'flaticon-list-3',
                        '/app/admin/application-statuses'
                    ),
                    new AppMenuItem(
                        'InterviewDetails',
                        'Pages.Recruitment.InterviewDetails',
                        'flaticon-list-3',
                        '/app/admin/interview-details'
                    ),
                    new AppMenuItem(
                        'OnbGroups',
                        'Pages.Recruitment.OnbGroups',
                        'flaticon-list-3',
                        '/app/admin/onb-groups'
                    ),
                    new AppMenuItem(
                        'OnbGroupDetails',
                        'Pages.Recruitment.OnbGroupDetails',
                        'flaticon-list-3',
                        '/app/admin/onb-group-details'
                    ),
                    new AppMenuItem(
                        'OnbTasks',
                        'Pages.Recruitment.OnbTasks',
                        'flaticon-list-3',
                        '/app/admin/onb-tasks'
                    ),
                    new AppMenuItem(
                        'OnbTemplates',
                        'Pages.Recruitment.OnbTemplates',
                        'flaticon-list-3',
                        '/app/admin/onb-templates'
                    ),
                    new AppMenuItem(
                        'OnbTemplateDetails',
                        'Pages.Recruitment.OnbTemplateDetails',
                        'flaticon-list-3',
                        '/app/admin/onb-template-details'
                    ),
                    new AppMenuItem(
                        'OnbApplicantTasks',
                        'Pages.Recruitment.OnbApplicantTasks',
                        'flaticon-list-3',
                        '/app/admin/onb-applicant-tasks'
                    ),
                    new AppMenuItem(
                        'OnbAppTaskComments',
                        'Pages.Recruitment.OnbAppTaskComments',
                        'flaticon-list-3',
                        '/app/admin/onb-app-task-comments'
                    ),
                    new AppMenuItem(
                        'OnbVideoLinks',
                        'Pages.Recruitment.OnbVideoLinks',
                        'flaticon-list-3',
                        '/app/admin/onb-video-links'
                    ),
                ]
            ),
            new AppMenuItem(
                'ProjectManagement',
                '',
                'flaticon-folder-1',
                '',
                [],
                [
                    new AppMenuItem(
                        'ProjectTypes',
                        'Pages.ProjectManagement.ProjectTypes',
                        'flaticon-list-3',
                        '/app/admin/project-types'
                    ),
                    new AppMenuItem(
                        'Projects',
                        'Pages.ProjectManagement.Projects',
                        'flaticon-list-3',
                        '/app/admin/projects'
                    ),
                    new AppMenuItem(
                        'ProjectWageTypes',
                        'Pages.ProjectManagement.ProjectWageTypes',
                        'flaticon-list-3',
                        '/app/admin/project-wage-types'
                    ),
                    new AppMenuItem(
                        'EmployeeProjects',
                        'Pages.ProjectManagement.EmployeeProjects',
                        'flaticon-list-3',
                        '/app/admin/employee-projects'
                    ),
                    new AppMenuItem(
                        'EmployeeProjectDetails',
                        'Pages.ProjectManagement.EmployeeProjectDetails',
                        'flaticon-list-3',
                        '/app/admin/employee-project-details'
                    ),
                    new AppMenuItem(
                        'ProjectRegisters',
                        'Pages.ProjectManagement.ProjectRegisters',
                        'flaticon-list-3',
                        '/app/admin/project-registers'
                    ),
                    new AppMenuItem(
                        'ProjectRegisterDetails',
                        'Pages.ProjectManagement.ProjectRegisterDetails',
                        'flaticon-list-3',
                        '/app/admin/project-register-details'
                    ),
                    new AppMenuItem(
                        'ProjectLogs',
                        'Pages.ProjectManagement.ProjectLogs',
                        'flaticon-list-3',
                        '/app/admin/project-logs'
                    ),
                    new AppMenuItem(
                        'ProjectCostEngine',
                        'Pages.ProjectManagement.ProjectCostEngine',
                        'flaticon-settings',
                        '/app/admin/project-cost-engine'
                    ),
                ]
            ),
            new AppMenuItem(
                'Workflow',
                '',
                'flaticon-refresh',
                '',
                [],
                [
                    new AppMenuItem(
                        'WorkflowRoles',
                        'Pages.Workflow.WorkflowRoles',
                        'flaticon-list-3',
                        '/app/admin/workflow-roles'
                    ),
                    new AppMenuItem(
                        'WorkflowGroups',
                        'Pages.Workflow.WorkflowGroups',
                        'flaticon-list-3',
                        '/app/admin/workflow-groups'
                    ),
                    new AppMenuItem(
                        'RequestMasters',
                        'Pages.Workflow.RequestMasters',
                        'flaticon-list-3',
                        '/app/admin/request-masters'
                    ),
                    new AppMenuItem(
                        'RequestDetails',
                        'Pages.Workflow.RequestDetails',
                        'flaticon-list-3',
                        '/app/admin/request-details'
                    ),
                    new AppMenuItem(
                        'RequestDetailParallels',
                        'Pages.Workflow.RequestDetailParallels',
                        'flaticon-list-3',
                        '/app/admin/request-detail-parallels'
                    ),
                    new AppMenuItem(
                        'WorkflowRequests',
                        'Pages.Workflow.WorkflowRequests',
                        'flaticon-list-3',
                        '/app/admin/workflow-requests'
                    ),
                    new AppMenuItem(
                        'RequestLogs',
                        'Pages.Workflow.RequestLogs',
                        'flaticon-list-3',
                        '/app/admin/request-logs'
                    ),
                    new AppMenuItem(
                        'WorkFlowApprovers',
                        'Pages.Workflow.WorkFlowApprovers',
                        'flaticon-list-3',
                        '/app/admin/work-flow-approvers'
                    ),
                    new AppMenuItem(
                        'WorkflowRemarks',
                        'Pages.Workflow.WorkflowRemarks',
                        'flaticon-list-3',
                        '/app/admin/workflow-remarks'
                    ),
                    new AppMenuItem(
                        'WFReadLogs',
                        'Pages.Workflow.WFReadLogs',
                        'flaticon-list-3',
                        '/app/admin/wf-read-logs'
                    ),
                ]
            ),
            new AppMenuItem(
                'AccommodationManagement',
                '',
                'flaticon-home',
                '',
                [],
                [
                    new AppMenuItem(
                        'Locations',
                        'Pages.AccommodationManagement.Locations',
                        'flaticon-list-3',
                        '/app/admin/locations'
                    ),
                    new AppMenuItem(
                        'Buildings',
                        'Pages.AccommodationManagement.Buildings',
                        'flaticon-list-3',
                        '/app/admin/buildings'
                    ),
                    new AppMenuItem(
                        'BuildingContracts',
                        'Pages.AccommodationManagement.BuildingContracts',
                        'flaticon-list-3',
                        '/app/admin/building-contracts'
                    ),
                    new AppMenuItem(
                        'Floors',
                        'Pages.AccommodationManagement.Floors',
                        'flaticon-list-3',
                        '/app/admin/floors'
                    ),
                    new AppMenuItem(
                        'Flats',
                        'Pages.AccommodationManagement.Flats',
                        'flaticon-list-3',
                        '/app/admin/flats'
                    ),
                    new AppMenuItem(
                        'Rooms',
                        'Pages.AccommodationManagement.Rooms',
                        'flaticon-list-3',
                        '/app/admin/rooms'
                    ),
                    new AppMenuItem('Beds', 'Pages.AccommodationManagement.Beds', 'flaticon-list-3', '/app/admin/beds'),
                    new AppMenuItem(
                        'AccommodationTypes',
                        'Pages.AccommodationManagement.AccommodationTypes',
                        'flaticon-list-3',
                        '/app/admin/accommodation-types'
                    ),
                    new AppMenuItem(
                        'AccommodationServiceTypes',
                        'Pages.AccommodationManagement.AccommodationServiceTypes',
                        'flaticon-list-3',
                        '/app/admin/accommodation-service-types'
                    ),
                    new AppMenuItem(
                        'EmployeeAccommodations',
                        'Pages.AccommodationManagement.EmployeeAccommodations',
                        'flaticon-list-3',
                        '/app/admin/employee-accommodations'
                    ),
                    new AppMenuItem(
                        'AccommodationServices',
                        'Pages.AccommodationManagement.AccommodationServices',
                        'flaticon-list-3',
                        '/app/admin/accommodation-services'
                    ),
                ]
            ),
            new AppMenuItem(
                'SurveyManagement',
                '',
                'flaticon-notepad',
                '',
                [],
                [
                    new AppMenuItem(
                        'Surveys',
                        'Pages.SurveyManagement.Surveys',
                        'flaticon-list-3',
                        '/app/admin/surveys'
                    ),
                    new AppMenuItem(
                        'SurveyDetails',
                        'Pages.SurveyManagement.SurveyDetails',
                        'flaticon-list-3',
                        '/app/admin/survey-details'
                    ),
                    new AppMenuItem(
                        'EmployeeSurveys',
                        'Pages.SurveyManagement.EmployeeSurveys',
                        'flaticon-list-3',
                        '/app/admin/employee-surveys'
                    ),
                    new AppMenuItem(
                        'EmployeeSurveyDetails',
                        'Pages.SurveyManagement.EmployeeSurveyDetails',
                        'flaticon-list-3',
                        '/app/admin/employee-survey-details'
                    ),
                    new AppMenuItem(
                        'TakeSurvey',
                        'Pages.SurveyManagement.EmployeeSurveys',
                        'flaticon-edit',
                        '/app/admin/take-survey'
                    ),
                ]
            ),
            new AppMenuItem(
                'BenefitsManagement',
                '',
                'flaticon-medical',
                '',
                [],
                [
                    new AppMenuItem(
                        'InsurancePlans',
                        'Pages.BenefitsManagement.InsurancePlans',
                        'flaticon-list-3',
                        '/app/admin/insurance-plans'
                    ),
                    new AppMenuItem(
                        'InsuranceProviders',
                        'Pages.BenefitsManagement.InsuranceProviders',
                        'flaticon-list-3',
                        '/app/admin/insurance-providers'
                    ),
                    new AppMenuItem(
                        'EmployeeInsuranceDetails',
                        'Pages.BenefitsManagement.EmployeeInsuranceDetails',
                        'flaticon-list-3',
                        '/app/admin/employee-insurance-details'
                    ),
                    new AppMenuItem(
                        'CompanyInsuranceDetails',
                        'Pages.BenefitsManagement.CompanyInsuranceDetails',
                        'flaticon-list-3',
                        '/app/admin/company-insurance-details'
                    ),
                ]
            ),
            new AppMenuItem(
                'TravelManagement',
                '',
                'flaticon-airplane',
                '',
                [],
                [
                    new AppMenuItem(
                        'ExpenseTypes',
                        'Pages.TravelManagement.ExpenseTypes',
                        'flaticon-list-3',
                        '/app/admin/expense-types'
                    ),
                    new AppMenuItem('Hotels', 'Pages.TravelManagement.Hotels', 'flaticon-list-3', '/app/admin/hotels'),
                    new AppMenuItem(
                        'TravelAirlines',
                        'Pages.TravelManagement.TravelAirlines',
                        'flaticon-list-3',
                        '/app/admin/travel-airlines'
                    ),
                    new AppMenuItem(
                        'TravelCancelReasons',
                        'Pages.TravelManagement.TravelCancelReasons',
                        'flaticon-list-3',
                        '/app/admin/travel-cancel-reasons'
                    ),
                    new AppMenuItem(
                        'TravelDestinations',
                        'Pages.TravelManagement.TravelDestinations',
                        'flaticon-list-3',
                        '/app/admin/travel-destinations'
                    ),
                    new AppMenuItem(
                        'TravelTypes',
                        'Pages.TravelManagement.TravelTypes',
                        'flaticon-list-3',
                        '/app/admin/travel-types'
                    ),
                    new AppMenuItem(
                        'TravelPerDiems',
                        'Pages.TravelManagement.TravelPerDiems',
                        'flaticon-list-3',
                        '/app/admin/travel-per-diems'
                    ),
                    new AppMenuItem(
                        'Travels',
                        'Pages.TravelManagement.Travels',
                        'flaticon-list-3',
                        '/app/admin/travels'
                    ),
                    new AppMenuItem(
                        'TravelDetails',
                        'Pages.TravelManagement.TravelDetails',
                        'flaticon-list-3',
                        '/app/admin/travel-details'
                    ),
                    new AppMenuItem(
                        'TravelDetailCosts',
                        'Pages.TravelManagement.TravelDetailCosts',
                        'flaticon-list-3',
                        '/app/admin/travel-detail-costs'
                    ),
                    new AppMenuItem(
                        'TravelFeedbacks',
                        'Pages.TravelManagement.TravelFeedbacks',
                        'flaticon-list-3',
                        '/app/admin/travel-feedbacks'
                    ),
                ]
            ),
            new AppMenuItem(
                'PerformanceManagement',
                '',
                'flaticon-medal',
                '',
                [],
                [
                    new AppMenuItem(
                        'FormTypes',
                        'Pages.PerformanceManagement.FormTypes',
                        'flaticon-list-3',
                        '/app/admin/form-types'
                    ),
                    new AppMenuItem(
                        'KpiCategories',
                        'Pages.PerformanceManagement.KpiCategories',
                        'flaticon-list-3',
                        '/app/admin/kpi-categories'
                    ),
                    new AppMenuItem(
                        'KpiInfos',
                        'Pages.PerformanceManagement.KpiInfos',
                        'flaticon-list-3',
                        '/app/admin/kpi-infos'
                    ),
                    new AppMenuItem(
                        'PARatings',
                        'Pages.PerformanceManagement.PARatings',
                        'flaticon-list-3',
                        '/app/admin/pa-ratings'
                    ),
                    new AppMenuItem(
                        'PACycles',
                        'Pages.PerformanceManagement.PACycles',
                        'flaticon-list-3',
                        '/app/admin/pa-cycles'
                    ),
                    new AppMenuItem(
                        'PAPeriods',
                        'Pages.PerformanceManagement.PAPeriods',
                        'flaticon-list-3',
                        '/app/admin/pa-periods'
                    ),
                    new AppMenuItem(
                        'PATimelines',
                        'Pages.PerformanceManagement.PATimelines',
                        'flaticon-list-3',
                        '/app/admin/pa-timelines'
                    ),
                    new AppMenuItem(
                        'PATimelineExceptions',
                        'Pages.PerformanceManagement.PATimelineExceptions',
                        'flaticon-list-3',
                        '/app/admin/pa-timeline-exceptions'
                    ),
                    new AppMenuItem(
                        'PAPeerNominees',
                        'Pages.PerformanceManagement.PAPeerNominees',
                        'flaticon-list-3',
                        '/app/admin/pa-peer-nominees'
                    ),
                    new AppMenuItem(
                        'KpiByJobTitles',
                        'Pages.PerformanceManagement.KpiByJobTitles',
                        'flaticon-list-3',
                        '/app/admin/kpi-by-job-titles'
                    ),
                    new AppMenuItem(
                        'KpiByJobTitleDetails',
                        'Pages.PerformanceManagement.KpiByJobTitles',
                        'flaticon-list-3',
                        '/app/admin/kpi-by-job-title-details'
                    ),
                    new AppMenuItem(
                        'KpiByOrgUnits',
                        'Pages.PerformanceManagement.KpiByOrgUnits',
                        'flaticon-list-3',
                        '/app/admin/kpi-by-org-units'
                    ),
                    new AppMenuItem(
                        'KpiByOrgUnitDetails',
                        'Pages.PerformanceManagement.KpiByOrgUnits',
                        'flaticon-list-3',
                        '/app/admin/kpi-by-org-unit-details'
                    ),
                    new AppMenuItem(
                        'PAForms',
                        'Pages.PerformanceManagement.PAForms',
                        'flaticon-list-3',
                        '/app/admin/pa-forms'
                    ),
                    new AppMenuItem(
                        'PerformanceAppraisalDashboard',
                        'Pages.PerformanceManagement.PAForms',
                        'flaticon-analytics',
                        '/app/admin/performance-appraisal-dashboard'
                    ),
                    new AppMenuItem(
                        'PAEmployees',
                        'Pages.PerformanceManagement.PAEmployees',
                        'flaticon-list-3',
                        '/app/admin/pa-employees'
                    ),
                ]
            ),
            new AppMenuItem(
                'CompetencyDevelopment',
                '',
                'flaticon-medal',
                '',
                [],
                [
                    new AppMenuItem(
                        'CompetencyTypes',
                        'Pages.CompetencyDevelopment.CompetencyTypes',
                        'flaticon-list-3',
                        '/app/admin/competency-types'
                    ),
                    new AppMenuItem(
                        'Competencies',
                        'Pages.CompetencyDevelopment.Competencies',
                        'flaticon-list-3',
                        '/app/admin/competencies'
                    ),
                    new AppMenuItem(
                        'CompetencyByJobTitles',
                        'Pages.CompetencyDevelopment.CompetencyByJobTitles',
                        'flaticon-list-3',
                        '/app/admin/competency-by-job-titles'
                    ),
                    new AppMenuItem(
                        'CompetencyByJobTitleDetails',
                        'Pages.CompetencyDevelopment.CompetencyByJobTitles',
                        'flaticon-list-3',
                        '/app/admin/competency-by-job-title-details'
                    ),
                    new AppMenuItem(
                        'CDPeriods',
                        'Pages.CompetencyDevelopment.CDPeriods',
                        'flaticon-list-3',
                        '/app/admin/cd-periods'
                    ),
                    new AppMenuItem(
                        'CDRatings',
                        'Pages.CompetencyDevelopment.CDRatings',
                        'flaticon-list-3',
                        '/app/admin/cd-ratings'
                    ),
                    new AppMenuItem(
                        'CDForms',
                        'Pages.CompetencyDevelopment.CDForms',
                        'flaticon-list-3',
                        '/app/admin/cd-forms'
                    ),
                ]
            ),
            new AppMenuItem(
                'ManpowerPlanningManagement',
                '',
                'flaticon-map',
                '',
                [],
                [
                    new AppMenuItem(
                        'ManpowerPlanningPeriods',
                        'Pages.ManpowerPlanningManagement.ManpowerPlanningPeriods',
                        'flaticon-list-3',
                        '/app/admin/manpower-planning-periods'
                    ),
                    new AppMenuItem(
                        'Positions',
                        'Pages.ManpowerPlanningManagement.Positions',
                        'flaticon-list-3',
                        '/app/admin/positions'
                    ),
                    new AppMenuItem(
                        'PositionChart',
                        'Pages.ManpowerPlanningManagement.PositionOrgChart',
                        'flaticon-network',
                        '/app/admin/org-charts/position'
                    ),
                    new AppMenuItem(
                        'PositionRelationships',
                        'Pages.ManpowerPlanningManagement.PositionRelationships',
                        'flaticon-list-3',
                        '/app/admin/position-relationships'
                    ),
                    new AppMenuItem(
                        'ManpowerPlannings',
                        'Pages.ManpowerPlanningManagement.ManpowerPlannings',
                        'flaticon-list-3',
                        '/app/admin/manpower-plannings'
                    ),
                ]
            ),
            new AppMenuItem(
                'TrainingManagement',
                '',
                'flaticon-book',
                '',
                [],
                [
                    new AppMenuItem(
                        'TrainingCourses',
                        'Pages.TrainingManagement.TrainingCourses',
                        'flaticon-list-3',
                        '/app/admin/training-courses'
                    ),
                    new AppMenuItem(
                        'TrainingSchedules',
                        'Pages.TrainingManagement.TrainingSchedules',
                        'flaticon-list-3',
                        '/app/admin/training-schedules'
                    ),
                    new AppMenuItem(
                        'EmployeeTrainings',
                        'Pages.TrainingManagement.EmployeeTrainings',
                        'flaticon-list-3',
                        '/app/admin/employee-trainings'
                    ),
                    new AppMenuItem(
                        'TrainingAttendances',
                        'Pages.TrainingManagement.TrainingAttendances',
                        'flaticon-list-3',
                        '/app/admin/training-attendances'
                    ),
                ]
            ),
            new AppMenuItem(
                'HealthAndSafetyManagement',
                '',
                'flaticon-safe-shield-protection',
                '',
                [],
                [
                    new AppMenuItem(
                        'ActionStatuses',
                        'Pages.HealthAndSafetyManagement.ActionStatuses',
                        'flaticon-list-3',
                        '/app/admin/action-statuses'
                    ),
                    new AppMenuItem(
                        'FirstAidMedicines',
                        'Pages.HealthAndSafetyManagement.FirstAidMedicines',
                        'flaticon-list-3',
                        '/app/admin/first-aid-medicines'
                    ),
                    new AppMenuItem(
                        'IncidentClaimStatuses',
                        'Pages.HealthAndSafetyManagement.IncidentClaimStatuses',
                        'flaticon-list-3',
                        '/app/admin/incident-claim-statuses'
                    ),
                    new AppMenuItem(
                        'IncidentNatures',
                        'Pages.HealthAndSafetyManagement.IncidentNatures',
                        'flaticon-list-3',
                        '/app/admin/incident-natures'
                    ),
                    new AppMenuItem(
                        'IncidentSeverities',
                        'Pages.HealthAndSafetyManagement.IncidentSeverities',
                        'flaticon-list-3',
                        '/app/admin/incident-severities'
                    ),
                    new AppMenuItem(
                        'IncidentStatuses',
                        'Pages.HealthAndSafetyManagement.IncidentStatuses',
                        'flaticon-list-3',
                        '/app/admin/incident-statuses'
                    ),
                    new AppMenuItem(
                        'IncidentTypes',
                        'Pages.HealthAndSafetyManagement.IncidentTypes',
                        'flaticon-list-3',
                        '/app/admin/incident-types'
                    ),
                    new AppMenuItem(
                        'InvestigationRootCauses',
                        'Pages.HealthAndSafetyManagement.InvestigationRootCauses',
                        'flaticon-list-3',
                        '/app/admin/investigation-root-causes'
                    ),
                    new AppMenuItem(
                        'RiskActions',
                        'Pages.HealthAndSafetyManagement.RiskActions',
                        'flaticon-list-3',
                        '/app/admin/risk-actions'
                    ),
                    new AppMenuItem(
                        'RiskImpacts',
                        'Pages.HealthAndSafetyManagement.RiskImpacts',
                        'flaticon-list-3',
                        '/app/admin/risk-impacts'
                    ),
                    new AppMenuItem(
                        'RiskPossibilities',
                        'Pages.HealthAndSafetyManagement.RiskPossibilities',
                        'flaticon-list-3',
                        '/app/admin/risk-possibilities'
                    ),
                    new AppMenuItem(
                        'RiskStatuses',
                        'Pages.HealthAndSafetyManagement.RiskStatuses',
                        'flaticon-list-3',
                        '/app/admin/risk-statuses'
                    ),
                    new AppMenuItem(
                        'RiskTypes',
                        'Pages.HealthAndSafetyManagement.RiskTypes',
                        'flaticon-list-3',
                        '/app/admin/risk-types'
                    ),
                    new AppMenuItem(
                        'SafetyAreaAssets',
                        'Pages.HealthAndSafetyManagement.SafetyAreaAssets',
                        'flaticon-list-3',
                        '/app/admin/safety-area-assets'
                    ),
                    new AppMenuItem(
                        'ActionRegisters',
                        'Pages.HealthAndSafetyManagement.ActionRegisters',
                        'flaticon-list-3',
                        '/app/admin/action-registers'
                    ),
                    new AppMenuItem(
                        'FirstAidBoxes',
                        'Pages.HealthAndSafetyManagement.FirstAidBoxes',
                        'flaticon-list-3',
                        '/app/admin/first-aid-boxes'
                    ),
                    new AppMenuItem(
                        'IncidentClaims',
                        'Pages.HealthAndSafetyManagement.IncidentClaims',
                        'flaticon-list-3',
                        '/app/admin/incident-claims'
                    ),
                    new AppMenuItem(
                        'IncidentInvestigations',
                        'Pages.HealthAndSafetyManagement.IncidentInvestigations',
                        'flaticon-list-3',
                        '/app/admin/incident-investigations'
                    ),
                    new AppMenuItem(
                        'IncidentRegisters',
                        'Pages.HealthAndSafetyManagement.IncidentRegisters',
                        'flaticon-list-3',
                        '/app/admin/incident-registers'
                    ),
                    new AppMenuItem(
                        'RiskRegisters',
                        'Pages.HealthAndSafetyManagement.RiskRegisters',
                        'flaticon-list-3',
                        '/app/admin/risk-registers'
                    ),
                    new AppMenuItem(
                        'SafetyChecklists',
                        'Pages.HealthAndSafetyManagement.SafetyChecklists',
                        'flaticon-list-3',
                        '/app/admin/safety-checklists'
                    ),
                    new AppMenuItem(
                        'SiteAudits',
                        'Pages.HealthAndSafetyManagement.SiteAudits',
                        'flaticon-list-3',
                        '/app/admin/site-audits'
                    ),
                ]
            ),
            new AppMenuItem(
                'ConfigureBusinessRules',
                'Pages.BusinessRules',
                'flaticon-cogwheel-2',
                '/app/admin/business-rules'
            ),
            new AppMenuItem(
                'SystemSettings',
                '',
                'flaticon-settings',
                '',
                [],
                [
                    new AppMenuItem(
                        'AppraisalSettings',
                        'Pages.SystemSettings.AppraisalSettings',
                        'flaticon-list-3',
                        '/app/admin/manage-appraisal-settings'
                    ),
                    new AppMenuItem(
                        'DocumentSettings',
                        'Pages.SystemSettings.DocumentSettings',
                        'flaticon-list-3',
                        '/app/admin/manage-document-settings'
                    ),
                    new AppMenuItem(
                        'FIPostingSettings',
                        'Pages.SystemSettings.FIPostingSettings',
                        'flaticon-list-3',
                        '/app/admin/manage-fi-posting-settings'
                    ),
                    new AppMenuItem(
                        'GeneralSettings',
                        'Pages.SystemSettings.GeneralSettings',
                        'flaticon-list-3',
                        '/app/admin/manage-general-settings'
                    ),
                    new AppMenuItem(
                        'NotificationSettings',
                        'Pages.SystemSettings.NotificationSettings',
                        'flaticon-list-3',
                        '/app/admin/manage-notification-settings'
                    ),
                    new AppMenuItem(
                        'OrgSettings',
                        'Pages.SystemSettings.OrgSettings',
                        'flaticon-list-3',
                        '/app/admin/manage-org-settings'
                    ),
                    new AppMenuItem(
                        'PayrollSettings',
                        'Pages.SystemSettings.PayrollSettings',
                        'flaticon-list-3',
                        '/app/admin/manage-payroll-settings'
                    ),
                    new AppMenuItem(
                        'PersonnelSettings',
                        'Pages.SystemSettings.PersonnelSettings',
                        'flaticon-list-3',
                        '/app/admin/manage-personnel-settings'
                    ),
                    new AppMenuItem(
                        'ProjectCostingSettings',
                        'Pages.SystemSettings.ProjectCostingSettings',
                        'flaticon-list-3',
                        '/app/admin/manage-project-costing-settings'
                    ),
                    new AppMenuItem(
                        'ReportSettings',
                        'Pages.SystemSettings.ReportSettings',
                        'flaticon-list-3',
                        '/app/admin/manage-report-settings'
                    ),
                    new AppMenuItem(
                        'SecuritySettings',
                        'Pages.SystemSettings.SecuritySettings',
                        'flaticon-list-3',
                        '/app/admin/manage-security-settings'
                    ),
                    new AppMenuItem(
                        'SelfServiceSettings',
                        'Pages.SystemSettings.SelfServiceSettings',
                        'flaticon-list-3',
                        '/app/admin/manage-self-service-settings'
                    ),
                    new AppMenuItem(
                        'TimeSettings',
                        'Pages.SystemSettings.TimeSettings',
                        'flaticon-list-3',
                        '/app/admin/manage-time-settings'
                    ),
                    new AppMenuItem(
                        'WorkflowSettings',
                        'Pages.SystemSettings.WorkflowSettings',
                        'flaticon-list-3',
                        '/app/admin/manage-workflow-settings'
                    ),
                    new AppMenuItem(
                        'PayrollDrivers',
                        'Pages.SystemSettings.PayrollDrivers',
                        'flaticon-list-3',
                        '/app/admin/payroll-drivers'
                    ),
                ]
            ),
            new AppMenuItem(
                'SystemSecurity',
                '',
                'flaticon-lock',
                '',
                [],
                [
                    new AppMenuItem(
                        'SysUserOrgUnitDatas',
                        'Pages.SystemSecurity.SysUserOrgUnitData',
                        'flaticon-list-3',
                        '/app/admin/sys-user-org-unit-datas'
                    ),
                    new AppMenuItem(
                        'EmailTemplates',
                        'Pages.SystemSecurity.EmailTemplates',
                        'flaticon-list-3',
                        '/app/admin/email-templates'
                    ),
                    new AppMenuItem(
                        'EmailTokens',
                        'Pages.SystemSecurity.EmailTokens',
                        'flaticon-list-3',
                        '/app/admin/email-tokens'
                    ),
                    new AppMenuItem(
                        'PayslipConfigs',
                        'Pages.SystemSecurity.PayslipConfigs',
                        'flaticon-list-3',
                        '/app/admin/payslip-configs'
                    ),
                    new AppMenuItem(
                        'NotificationMasters',
                        'Pages.SystemSecurity.NotificationMasters',
                        'flaticon-list-3',
                        '/app/admin/notification-masters'
                    ),
                    new AppMenuItem(
                        'SystemNotifications',
                        'Pages.SystemSecurity.SystemNotifications',
                        'flaticon-list-3',
                        '/app/admin/system-notifications'
                    ),
                    new AppMenuItem(
                        'SysObjects',
                        'Pages.SystemSecurity.SysObjects',
                        'flaticon-list-3',
                        '/app/admin/sys-objects'
                    ),
                    new AppMenuItem(
                        'RetroReasonLogs',
                        'Pages.SystemSecurity.RetroReasonLogs',
                        'flaticon-list-3',
                        '/app/admin/retro-reason-logs'
                    ),
                    new AppMenuItem(
                        'PunchNotificationLogs',
                        'Pages.SystemSecurity.PunchNotificationLogs',
                        'flaticon-list-3',
                        '/app/admin/punch-notification-logs'
                    ),
                    new AppMenuItem(
                        'BulkUploadLogs',
                        'Pages.SystemSecurity.BulkUploadLogs',
                        'flaticon-list-3',
                        '/app/admin/bulk-upload-logs'
                    ),
                    new AppMenuItem(
                        'ExecutionLogs',
                        'Pages.SystemSecurity.ExecutionLogs',
                        'flaticon-list-3',
                        '/app/admin/execution-logs'
                    ),
                    new AppMenuItem(
                        'EmailNotificationLogs',
                        'Pages.SystemSecurity.EmailNotificationLogs',
                        'flaticon-list-3',
                        '/app/admin/email-notification-logs'
                    ),
                    new AppMenuItem(
                        'SysTenantConfigs',
                        'Pages.SystemSecurity.SysTenantConfigs',
                        'flaticon-list-3',
                        '/app/admin/sys-tenant-configs'
                    ),
                ]
            ),
            new AppMenuItem(
                'ReportManagement',
                '',
                'flaticon-list-3',
                '',
                [],
                [
                    new AppMenuItem(
                        'DomainTypes',
                        'Pages.ReportManagement.DomainTypes',
                        'flaticon-list-3',
                        '/app/admin/domain-types'
                    ),
                    new AppMenuItem(
                        'ReportTypes',
                        'Pages.ReportManagement.ReportTypes',
                        'flaticon-list-3',
                        '/app/admin/report-types'
                    ),
                    new AppMenuItem(
                        'PrintReports',
                        'Pages.ReportManagement.PrintReports',
                        'flaticon-list-3',
                        '/app/admin/print-reports'
                    ),
                    new AppMenuItem(
                        'PrintReportPermissions',
                        'Pages.ReportManagement.PrintReportPermissions',
                        'flaticon-list-3',
                        '/app/admin/print-report-permissions'
                    ),
                    new AppMenuItem(
                        'PrintReportPermissionDetails',
                        'Pages.ReportManagement.PrintReportPermissionDetails',
                        'flaticon-list-3',
                        '/app/admin/print-report-permission-details'
                    ),
                    new AppMenuItem(
                        'ReportMasters',
                        'Pages.ReportManagement.ReportMasters',
                        'flaticon-list-3',
                        '/app/admin/report-masters'
                    ),
                    new AppMenuItem(
                        'ReportDetails',
                        'Pages.ReportManagement.ReportDetails',
                        'flaticon-list-3',
                        '/app/admin/report-details'
                    ),
                    new AppMenuItem(
                        'ReportSettings',
                        'Pages.ReportManagement.ReportSettings',
                        'flaticon-list-3',
                        '/app/admin/report-settings'
                    ),
                    new AppMenuItem(
                        'ReportSettingDetails',
                        'Pages.ReportManagement.ReportSettingDetails',
                        'flaticon-list-3',
                        '/app/admin/report-setting-details'
                    ),
                    new AppMenuItem(
                        'ReportInfos',
                        'Pages.ReportManagement.ReportInfos',
                        'flaticon-list-3',
                        '/app/admin/report-infos'
                    ),
                    new AppMenuItem(
                        'ReportInfoDetails',
                        'Pages.ReportManagement.ReportInfoDetails',
                        'flaticon-list-3',
                        '/app/admin/report-info-details'
                    ),
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
                        'ManageScreenCodes',
                        'Pages.Administration.Navigation.ManageScreenCodes',
                        'flaticon-tabs',
                        '/app/admin/screen-codes'
                    ),
                    new AppMenuItem(
                        'ManageQuickLinks',
                        'Pages.Administration.Navigation.ManageQuickLinks',
                        'flaticon-share',
                        '/app/admin/quick-links'
                    ),
                    new AppMenuItem(
                        'ApiDictionary',
                        'Pages.Administration.ApiDictionary',
                        'flaticon-book',
                        '/app/admin/api-dictionary'
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

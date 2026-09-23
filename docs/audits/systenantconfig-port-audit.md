# SysTenantConfig port audit (legacy HCM10 -> HCMv3)

Audit date: 2026-09-23. Scope: every key the legacy code reads from the `SysTenantConfig`
table, and whether the behaviour that key drives exists in HCMv3.

## How legacy uses the table

`SysTenantConfig` (legacy `HCM10.Module/BusinessObjects/SysSecurity/SysTenantConfig.cs`) is a
Key / KeyValue / StartDate / EndDate / ConfigCategory / ConfigDescription table.
`SysService.GetSysTenantConfig(key)` reads it by key only (dates are ignored).

Since the settings refactor, legacy code does not read the keys directly at runtime. It reads one of
14 `Manage*Settings` singleton rows. `SysService.Get*Settings()` creates that row the first time it is
missing, **initialising it from the `SysTenantConfig` keys** (the "fallback initialiser"). So the table's
real impact is: (a) the 60 keys that seed the 14 settings rows, (b) 9 D365 keys read directly by the
costing file screen, (c) two keys read directly by reports/screens, (d) 9 keys seeded by
`DefaultDataService`.

Local data check (read-only, `SysTenantConfig` and `Manage*Settings` tables only): HRMS_RCF has 0 rows in
`SysTenantConfig` and one row in each `Manage*Settings` table; HRMS_CCH has 0 rows; HRMS_ASF has 3
rows (`EMAIL_NOTIFICATION`, `ENABLE_DOC_SAVE_IN_FILE_SYSTEM`, `FILE_SYSTEM_ATTACHMENT_PATH`).

## What is ported

| Item | HCMv3 | Status |
|---|---|---|
| `SysTenantConfig` entity + category enum | `HCMv3.Core/SystemSecurity/SysTenantConfig.cs`, `Core.Shared/SystemSecurity/TenantConfigCategory.cs` | Ported |
| CRUD app service, DTOs, permission, Angular screen | `SysTenantConfigsAppService`, `angular/.../system-security/sys-tenant-configs` | Ported |
| Key read by name | `ISysSettingsAccessor.GetSysTenantConfigAsync(key)` (ignores dates, same as legacy) | Ported |
| 14 `Manage*Settings` entities, accessor, CRUD screens | `HCMv3.Core/SystemSettings/*`, `SysSettingsAccessor` | Ported, property parity with legacy (renames: `DelegateEmployeeListBy` -> `...OrganizationUnitTypeId`, `AbsenceViolationRule` -> `AbsenceViolationRuleId`, byte sizes -> MB) |
| 9 D365 keys (`REQUEST_URL`, `CLIENT_ID`, `GRANT_TYPE`, `RESOURCE`, `CLIENT_SECRET`, `CHANGESET_LIMIT`, `REQUEST_URL_JOURNAL_HEADER`, `REQUEST_URL_JOURNAL_LINE`, `JOURNAL_HEADER_ODATA_ETAG`) | `D365FinanceSettingKeys`, `D365FinanceConfigReader` (IntegrationSetting first, then `SysTenantConfig` fallback) | Ported |
| `SEPARATE_PROVISION_FI_POSTING` direct read in Post JV | `PostJvAppService` via `ManageFIPostingSettings.SeparateProvisionPosting` | Ported |

## Wave 1 (done 2026-09-23)

The three table-level gaps below are closed on branch `feat/ess-workflow-foundation`:

- `SysSettingsAccessor.Get*SettingsAsync()` now builds and saves the tenant's first row from its
  `SysTenantConfig` keys (`SysSettingsTenantConfigFactory`, one method per legacy `Get*Settings`), so a
  settings row is never missing inside a tenant. Number and date values that do not parse are left at
  their default instead of throwing, which is the one deliberate departure from legacy.
- The nine legacy default keys are seeded for every existing tenant at startup
  (`SysTenantConfigDefaultsBuilder` in `SeedHelper`) and for each new tenant at creation
  (`SysTenantConfigDefaultsSeeder` in `TenantManager`). Existing keys are never overwritten.
- `ENABLE_WAGEACCOUNT_BY_ORGUNIT` drives the Payroll JV Custom042 family: when "1", wage accounts come
  from the group mapped to the employee's organization unit (`PayrollJVWageAccountSource.OrgUnitWageAccGroup`
  in `PayrollJVPostingLineLoader`), with the employee-code offset account resolved as legacy did.
- Key names live in `HCMv3.Core.Shared/SystemSecurity/SysTenantConfigKeys.cs`.

Side effect to know about: a tenant that never opened a settings screen used to read `null` and take
each consumer's own default; it now gets the legacy defaults, including 9 working hours, an earliest
time-evaluation date of 01/01/2017 and time attendance integration ON.

## Gaps at the table level (as found, before wave 1)

1. **Fallback initialiser not ported.** `SysSettingsAccessor.Get*SettingsAsync()` returns `null` when
   the settings row is missing; nothing builds it from `SysTenantConfig` keys. Lost with it: the
   `REPORT_GROUP` -> `ClientId` mapping (`IMILL`->004, `LND`->007, `MASHAAN`->008, `CUSTOM19`->019),
   `TIME_EVAL_EARLIEST_DATE` parse (dd/MM/yyyy), `ENABLE_BASICPAY_MORE_THEN_FIXEDDAYS` and
   `ENABLE_OVERTIME_IN_TIME_ENGINE` defaulting to **true** unless the key is "0", and every other
   key-to-flag mapping in `SysService.cs` lines 300-1050.
2. **Default seed not ported.** Legacy `DefaultDataService.CreateDefaultSysTenantConfig` seeds
   `DEFAULT_WORKING_HOURS=9`, `TIME_EVAL_EARLIEST_DATE=01/01/2017`, `CUTOVER_DATE=01/01/2017`,
   `DATE_EDIT_MASK=dd/MM/yyyy`, `DATE_DISPLAY_FORMAT={0:dd/MM/yyyy}`, `TIME_EDIT_MASK=HH:mm`,
   `TIME_DISPLAY_FORMAT={0:HH\:mm}`, `ENABLE_TIMEATTENDANCE_INTEGRATION=1`,
   `LEAVE_CONFLICT_CHECK=Department`. HCMv3 seeds none. `CUTOVER_DATE` and `LEAVE_CONFLICT_CHECK` are
   never read by legacy code, so only the other seven matter.
3. **`ENABLE_WAGEACCOUNT_BY_ORGUNIT`** (read directly by the Payroll JV Custom042 report) has no HCMv3
   equivalent; the ported Custom042 variants do not carry the wage-account-by-org-unit branch.
4. `ENABLE_PAID_LEAVE_IN_PROBATION`, `ALLOW_PAID_LEAVE_BACKDATE_ENTRY` (ESS) and `CaptchaKey` are
   commented out in legacy: no impact to port.
5. HCMv3 adds one key of its own: `PassportWithdrawalDocumentTypeCode`.

## Setting-by-setting impact

Method: for every property of the 14 settings classes, count the legacy files that read it (Module, Web,
ESS, PayrollEngine, TimeEngine, Recruiting, Module.Web) and the HCMv3 backend files that read it
(excluding the settings entities, DTOs, mappers, CRUD services, migrations and doc comments). Angular was
checked separately: none of the gap settings are consumed there.

Totals: 205 properties. 94 read on both sides. 13 read on neither side. 4 HCMv3-only.
**78 are read by legacy and by nothing in HCMv3.** Those 78 follow, grouped, with the legacy impact.

### Business logic missing (port needed)

| Class | Setting | Legacy impact (where) | RCF value |
|---|---|---|---|
| Appraisal | `AppraisalCycle` | PA form activity list per cycle (`PAFormController` 940, 1103; ESS `AppraisalRepository`) | Quarterly |
| Appraisal | `EnablePAPeerReview` | Peer-review step on PA form/employee, manpower planning (12 sites) | off |
| Appraisal | `SetPeerMinLimit`, `SetPeerMaxLimit` | Peer nominee count validation (`PAForm.cs` 460, `PAFormController` 1458, ESS) | 0 / 0 |
| Appraisal | `EnableMarksInPAReview`, `MarksOn` | Marks vs rating/weight scoring (`PAEmployee.cs` 274-403, `PARating.cs` 137) | null |
| Workflow | `EnableOrgUnitTypeInWorkflow` | Approver resolution by org level in `WorkFlowServices`, `WorkflowGeneralService`, `NotificationService`, `EmailService`, `WorkflowRequest` (12 sites) | **on** |
| Workflow | `HideLeavePayDetailsInWorkflow` | Hide pay block on leave request in approval view (ESS, Recruiting API) | **on** |
| Workflow | `DynamicWorkflowSteps` | `RequestDetail` step validation branch (373-377) | **on** |
| Workflow | `SendBackToCreator` | Extra action on roster planner / unified leave approval | off |
| Workflow | `DelegateEmployeeListBy` | Delegate picker scoped by org unit type (ESS `BaseRepository` 1153) | null |
| Workflow | `EnableTravelCostOnWorkflowstep` | Travel request redirects to cost entry at that step (`TravelController` 223) | 0 |
| Time | `EnableBookingQuotaInLeaveRequest` | Booking-quota check on leave create/amend in `WorkFlowServices` (7 sites), `AbsenceRequest`, `LeaveRequest`, `EmployeeAbsenceController` | null |
| Time | `EnableAutoHalfDayLeave` | Auto half-day leave on absence entry + ESS form flag | off |
| Time | `EnableSettleFractionalLeaveBalance` | Fractional balance settlement field on absence | off |
| Time | `EnableAttendanceOnWeekOff`, `EnableAttendanceOnRestDay` | `EmployeeAttendance.cs` 473/488 blocks attendance on week-off / rest day unless on | null |
| Time | `AllowDuplicateInTimeEvent` | Relaxed duplicate check in `TimeEvent.cs` 219 (HCMv3 `TimeEvent.cs` header says unported) | null |
| Time | `FutureEvaluationInDays` | Time engine refuses runs beyond N days ahead (`uscTimeEngine` 281, 322) | 300 |
| Time | `EnablePayFullWithPayroll` | Leave pay default in `WorkFlowServices` 6096 and ESS defaults | off |
| Time | `RosterPlannerFrequency` | Roster planner period (weekly/monthly) | null |
| Time | `AbsenceViolationEffectiveDate` | Absence violation notifications start date (`NotificationService` 2910) | null |
| Time | `TimeEventSyncFreq`, `MidTierSyncFreq` | Hangfire sync cadence for time events / mid-tier (`Startup.cs` 60-61, defaults 10 / 15 min) | null |
| Time | `EnableIsTravelling` | Show "is travelling" field on leave request | off |
| Payroll | `EnablePayrollEngineAutorun` | Payroll engine autorun switch (`PayrollEngine/Program.cs` 5482) | off |
| Payroll | `AllowSimulationRun` | Shows the simulation tick box on the payroll run screen | null |
| Payroll | `CarryAllowanceOnSalaryImport` | Salary import carries allowances (`ImportExcelController` 3994) | off |
| Payroll | `ShowBillingTypeInPaymentScreen` | Billing type column on employee payment | null |
| Payroll | `EditableAutoFillFnFCompensation` | Compensate-days editors enabled on F&F | off |
| Payroll | `DefaultLeavePaymentType` | ESS leave request default payment type | 0 |
| Payroll | `PayslipSignature` | Signature panel on payslip report (`rptPayslipEn` 58-94 and customs) | **on** |
| Payroll | `EnableEffectiveDateInRecPayment` | Recurring payment end-date rule (`EmployeeRecurringPayment.cs` 127) | null |
| Report | `ShowSubordinatesPayslip`, `ShowPayslipOnDay`, `ShowPayslipFromDate` | ESS payslip visibility: managers see team payslips, only from day N of month, only from date | off / 0 / null |
| Report | `DecimalInReports` | Rounding in print and grid reports (`MyXtraReport` 528, `GridReportHelper` 1156) | **3** |
| Report | `ShowPeriodInLeaveProvision`, `PeriodMonthLimitInLeaveProvision` | Leave provision report month range + limit | null |
| Report | `ShowPeriodInIndemnityProvision`, `PeriodMonthLimitInIndemnityProvision` | Indemnity provision report month range + limit | null |
| General | `DisableAutoEmpcode` | Employee code editable instead of number series | off |
| General | `NoticePeriodDaysExcludingSubmissionDate` | Separation notice period calc (`EmployeeSeparation.cs` 254) | off |
| General | `StartupDashboard` | Landing dashboard after login | Leave_Dashboard |
| Org | `EnablePosition` | Org assignment requires a position; position occupancy maintained (`EmployeeOrgAssignment.cs` 766-1100, `EmployeeActionPopupController` 1557) | off |
| Org | `EnablePostingGroup`, `EnableBudgetingGroup` | Show/require posting and budgeting groups on org unit + ESS action request | off |
| Org | `EnableOrgChart3Levels` | Org chart depth for non-admins | null |
| FI | `EnableAutoWageAccountEntry` | Auto-create wage account row on org assignment save (`EmployeeOrgAssignment.cs` 907) | off |
| FI | `AllowDuplicateCodeInCostElement` | Cost element unique-code check (HCMv3 `CostElement.cs` header says unported) | null |
| Personnel | `AutoWageAssignOnHire` | Auto wage assignment on hire (`Employee.cs` 2112) | null |
| Security | `EnableAuditLogInPrintReport`, `EnableReportExportPwd`, `EnableAuditLogInCostEngine` | Audit log on print reports / costing file; password-protect exports | null |
| Self-service | `ShowLeaveInfo`, `ShowCurrentLeaveBalance`, `ShowUpdatedLeaveBalance`, `ShowEmployeeInfo`, `IsAdditionalUnpaidLeaveAllowed`, `ShowPaymentDate` | Unified leave request form toggles, merged with role-based settings (`BaseRepository` 1513-1601) | null |
| Self-service | `IsAttachmentAllowed` | Passport withdrawal attachment toggle | null |
| Document | `WarningThresholdSize` (HCMv3: `StorageWarningThresholdMB`) | Storage warning banner (`StorageWarningController` 70) | null |

### Legacy-only platform behaviour (no port needed, confirm and close)

`EnableGridDataAccessModeServer`, `AutoFilterColumnCondition`, `StartHangfire`, `DisableNotificationWindow`
(XAF/DevExpress UI), `RedirectAfterWFApproval`, `DefaultLoginPage`, `CaptchaKey` (old ESS MVC shell),
`DateDisplayFormat`, `TimeDisplayFormat` (XAF display masks; Angular formats on the client),
`FileSystemAttachmentPath` (HCMv3 uses the per-tenant file store by design),
`MaxFileSizePerAttachment`, `MaxStorageSize` (ported as the MB properties).

### Read on neither side

`EnableFixedDaysLogicChange`, `AllowDeleteBackdatedSalaryRegister`, `ExportGridReportWithHeader`,
`ExportGridReportWithFooter`, `EnablePayWithPayroll`, `EnablePayInAdvance`, `IsPassportWithdrawalRequired`,
`PassportWithdrawalSubmissionStep`, `SickLeaveAttachMandatory`, `MidTierSyncBatchCount`,
`OvertimeRequestModificationStep`, `StorageWarningThresholdMB`, `DelegateEmployeeListByOrganizationUnitTypeId`.

## Suggested porting order

1. Table level: fallback initialiser + default seed (one domain service, tests), `ENABLE_WAGEACCOUNT_BY_ORGUNIT`.
2. Settings that are **on** in RCF: workflow org-unit-type routing, hide leave pay details, dynamic
   steps, payslip signature, `DecimalInReports`, `FutureEvaluationInDays`, `StartupDashboard`.
3. Leave/time rules: booking quota, auto half-day, attendance on week-off/rest day, duplicate time
   events, pay-full-with-payroll default, sync cadence.
4. Appraisal block (6 settings) and the ESS leave-form toggles (6 settings).
5. Remaining payroll/report/org/FI/personnel/security flags.

## Waves 2 to 5 (done 2026-09-23, uncommitted)

Where each legacy setting now takes effect in HCMv3:

| Setting | Where it now works |
|---|---|
| `EnableOrgUnitTypeInWorkflow` | `WorkflowApproverResolver`: approvers narrowed to the requester's unit at the step's level (`WorkflowRoleOrgId`), replacing the role-group scope, as legacy |
| `DynamicWorkflowSteps` | `RequestDetailsAppService`: off keeps the step number, on moves the step and shifts the others; chain always renumbered 1..n |
| `PayslipSignature`, `DecimalInReports` | Report storage and go-live payslip PDF (signature panel, `.000` formats); grid Excel export number format |
| `FutureEvaluationInDays`, `EarliestEvaluationDate` | `TimeEngineAppService.RunTimeEngine` refuses runs outside the limits |
| `StartupDashboard` | Session `tenant.startupDashboard` and the Angular landing guard |
| `AllowDuplicateInTimeEvent` | `TimeEventsAppService` duplicate key includes the source when on |
| `EnableAttendanceOnWeekOff`, `EnableAttendanceOnRestDay` | `EmployeeAttendancesAppService` refuses a start on a week-off or rest day unless allowed |
| `EnableBookingQuotaInLeaveRequest` | Balance subtracts pending days only when on; when off a pending request of the same type blocks a new one |
| `EnableAutoHalfDayLeave`, `EnablePayFullWithPayroll` | `LeaveRuleValidator` refuses a half day / Pay Full With Payroll unless enabled |
| `DefaultLeavePaymentType`, `EnableIsTravelling`, `HideLeavePayDetailsInWorkflow` | `MyLeave.GetLeaveFormSettings` for the leave screens; approvers only ever see the summary line, so pay details are not exposed |
| Appraisal: `AppraisalCycle`, `EnablePAPeerReview`, `SetPeerMin/MaxLimit`, `EnableMarksInPAReview`, `MarksOn` | Peer-count rule on PA form and nominees, rating-range overlap rule, marks scoring in `PAEmployeeRatingsAppService`, `PAForms.GetAppraisalFormSettings` |
| `EnablePayrollEngineAutorun` | New `PayrollEngineAutorunBackgroundWorker` (daily) |
| `EnableEffectiveDateInRecPayment` | Recurring payment auto end date |
| `DisableAutoEmpcode` | Employee code typed in and required when on, generated when off |
| `NoticePeriodDaysExcludingSubmissionDate` | Separation notice period computed from the dates |
| `ShowPeriodIn*/PeriodMonthLimitIn*` (leave, indemnity provision) | Provision reports ignore the TO month when hidden and enforce the month limit |
| `AllowDuplicateCodeInCostElement` | Cost element code unique per type when on |
| `EnableAutoWageAccountEntry` | Wage accounts copied with the new location when an org assignment changes cost centre |
| `AutoWageAssignOnHire` | New employee may copy another employee's open wage accounts (`CopyWageAccountsFromEmployeeId`) |
| `EnableAuditLogInPrintReport` | Execution-log row per print report opened |
| `SendBackToCreator`, `EnableTravelCostOnWorkflowstep` | ESS inbox `CanSendBack`, `EnableTravelCost` |
| `DelegateEmployeeListBy` | ESS delegation candidates limited to the same unit at that level |
| `WarningThresholdSize` | Attachment settings report used storage and a warning flag |
| `EnableTimeAttIntegrationAPI` | New `TimeAttendanceApiSyncBackgroundWorker` (15 min) |
| Presentation flags (posting/budgeting group, position, org chart levels, billing type column, F&F auto-fill, simulation run) | `TenantUiSettingsAppService.GetTenantUiSettings` |

### Still not ported, and why

| Setting | Blocker |
|---|---|
| `CarryAllowanceOnSalaryImport` | HCMv3 has no salary Excel import to apply it to |
| `ShowSubordinatesPayslip`, `ShowPayslipOnDay`, `ShowPayslipFromDate` | No ESS payslip screen yet |
| `RosterPlannerFrequency` | ESS roster planner not ported |
| `AbsenceViolationEffectiveDate` | Absence-violation notification job not ported |
| `EnableSettleFractionalLeaveBalance` | Fractional leave balance settlement not ported |
| `TimeEventSyncFreq`, `MidTierSyncFreq`, `TimeEventSyncBatchCount` | Access-control database sync needs a connection HCMv3 does not store |
| `EnableReportExportPwd` | Grid export library (MiniExcel) cannot password-protect files |
| `EnablePosition` position occupancy (`PositionRelationship` upkeep) | Position relationship maintenance not ported; the flag is exposed to screens only |

Angular screens still have to read the new settings endpoints, and the NSwag proxies must be regenerated.

---
helpId: sys.time-settings.edit
lang: en
title: Time settings
entity: HCMv3.SystemSettings.ManageTimeSettings
surfaces:
  - angular/src/app/admin/system-settings/manage-time-settings/create-or-edit-manage-time-settings-modal.component.html
audience: [admin]
permissions: [Pages.SystemSettings.ManageTimeSettings]
---

## Overview

Company-wide time and attendance behaviour: where evaluation starts, rosters, the attendance integrations, how holidays combine with off days, hourly leave, overtime, forfeiting leave and approval of time registers.

Many of the leave-request rules in the lower half are not enforced at present and say so.

There is one settings record per company; it cannot be deleted.

## EarliestEvaluationDate {#ManageTimeSettings.EarliestEvaluationDate}
<!-- alias: loc:EarliestEvaluationDate, dom:TimeSettings_EarliestEvaluationDate -->

**What it is.** The first date the time engine evaluates.

**Example.** `2026-01-01`

**Why it matters.** The time engine never works out days before it, attendance imports start from it when nothing has been imported yet, and payroll treats master-data changes dated earlier as if made on it. Set it to the go-live date.

**Rules.** Optional. Date value.

## FutureEvaluationInDays {#ManageTimeSettings.FutureEvaluationInDays}
<!-- alias: loc:FutureEvaluationInDays, dom:TimeSettings_FutureEvaluationInDays -->

**What it is.** How many days ahead the time engine evaluates.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Optional. Whole number.

## RosterPlannerFrequency {#ManageTimeSettings.RosterPlannerFrequency}
<!-- alias: loc:RosterPlannerFrequency, dom:TimeSettings_RosterPlannerFrequency -->

**What it is.** How often the roster planner runs.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Required. One of: Weekly, Monthly.

## EnableRosterDuty {#ManageTimeSettings.EnableRosterDuty}
<!-- alias: loc:EnableRosterDuty, dom:TimeSettings_EnableRosterDuty -->

**What it is.** Whether duties come from rosters.

**Why it matters.** Ticked, the time engine and payroll find each day's duty through the roster order -- substitute duty, the employee's duty period with its rotation, then the roster group. Unticked, a substitute duty still wins, otherwise the weekday duties of the employee's own duty period are used; week rotations and roster groups are ignored.

**Rules.** Yes or no.

## EnableTimeEngineAutorun {#ManageTimeSettings.EnableTimeEngineAutorun}
<!-- alias: loc:EnableTimeEngineAutorun, dom:TimeSettings_EnableTimeEngineAutorun -->

**What it is.** Whether the time engine runs automatically.

**Why it matters.** Ticked, a background job runs the time engine on its own, and payroll runs change how they pick employees and off-cycle dates (see Run Payroll). Unticked, the time engine runs only when started by hand.

**Rules.** Yes or no.

## EnableMiddleTierTimeEvents {#ManageTimeSettings.EnableMiddleTierTimeEvents}
<!-- alias: loc:EnableMiddleTierTimeEvents, dom:TimeSettings_EnableMiddleTierTimeEvents -->

**What it is.** Whether punches are taken from the middle-tier attendance database.

**Why it matters.** Ticked, the middle-tier punch sync runs; unticked, it does nothing.

**Rules.** Yes or no.

## EnableGeoFence {#ManageTimeSettings.EnableGeoFence}
<!-- alias: loc:EnableGeoFence, dom:TimeSettings_EnableGeoFence -->

**What it is.** Whether mobile punches must be within the work location's range.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## EnableTimeAttendanceIntegration {#ManageTimeSettings.EnableTimeAttendanceIntegration}
<!-- alias: loc:EnableTimeAttendanceIntegration, dom:TimeSettings_EnableTimeAttendanceIntegration -->

**What it is.** Whether punches are imported from attendance devices.

**Why it matters.** Ticked, the device import and re-pull run; unticked, they do nothing.

**Rules.** Yes or no.

## EnableAttendanceOnHoliday {#ManageTimeSettings.EnableAttendanceOnHoliday}
<!-- alias: loc:EnableAttendanceOnHoliday, dom:TimeSettings_EnableAttendanceOnHoliday -->

**What it is.** Whether the time engine records attendance automatically.

**Why it matters.** Ticked, the time engine creates or updates employee attendance records itself as it processes each day.

**Rules.** Yes or no.

## EnableAttendanceOnWeekOff {#ManageTimeSettings.EnableAttendanceOnWeekOff}
<!-- alias: loc:EnableAttendanceOnWeekOff, dom:TimeSettings_EnableAttendanceOnWeekOff -->

**What it is.** Whether attendance is recorded automatically on weekly offs.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## EnableAttendanceOnRestDay {#ManageTimeSettings.EnableAttendanceOnRestDay}
<!-- alias: loc:EnableAttendanceOnRestDay, dom:TimeSettings_EnableAttendanceOnRestDay -->

**What it is.** Whether attendance is recorded automatically on rest days.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## CountWeekoffOnPH {#ManageTimeSettings.CountWeekoffOnPH}
<!-- alias: loc:CountWeekoffOnPH, dom:TimeSettings_CountWeekoffOnPH -->

**What it is.** Whether a public holiday on a weekly off counts as the weekly off.

**Why it matters.** A day cannot be both. Ticked, it counts as a weekly off; unticked, as a public holiday. This changes what payroll pays for the day.

**Rules.** Yes or no.

## CountRestDayOnPH {#ManageTimeSettings.CountRestDayOnPH}
<!-- alias: loc:CountRestDayOnPH, dom:TimeSettings_CountRestDayOnPH -->

**What it is.** Whether a public holiday on a rest day counts as the rest day.

**Why it matters.** Ticked, it counts as a rest day; unticked, as a public holiday.

**Rules.** Yes or no.

## EnableTimeAttIntegrationAPI {#ManageTimeSettings.EnableTimeAttIntegrationAPI}
<!-- alias: loc:EnableTimeAttIntegrationAPI, dom:TimeSettings_EnableTimeAttIntegrationAPI -->

**What it is.** Whether punches are imported from the attendance system's API (BioTime).

**Why it matters.** The same switch as **Integration Center → BioTime**.

**Rules.** Yes or no.

## AllowDuplicateInTimeEvent {#ManageTimeSettings.AllowDuplicateInTimeEvent}
<!-- alias: loc:AllowDuplicateInTimeEvent, dom:TimeSettings_AllowDuplicateInTimeEvent -->

**What it is.** Whether two identical punches may be kept.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## UseActualTransInPlannedTimePolicy {#ManageTimeSettings.UseActualTransInPlannedTimePolicy}
<!-- alias: loc:UseActualTransInPlannedTimePolicy, dom:TimeSettings_UseActualTransInPlannedTimePolicy -->

**What it is.** Whether planned-time employees still have their punches read.

**Why it matters.** Unticked, employees on a *Planned Time* policy are taken to have worked the planned shift. Ticked, their clock-in and clock-out punches are read too.

**Rules.** Yes or no.

## EnableMySqlIntegration {#ManageTimeSettings.EnableMySqlIntegration}
<!-- alias: loc:EnableMySqlIntegration, dom:TimeSettings_EnableMySqlIntegration -->

**What it is.** Whether punches are imported from a MySQL attendance database.

**Why it matters.** Ticked, the MySQL import runs; unticked, it does nothing.

**Rules.** Yes or no.

## TimeEventImportPath {#ManageTimeSettings.TimeEventImportPath}
<!-- alias: loc:TimeEventImportPath, dom:TimeSettings_TimeEventImportPath -->

**What it is.** The folder punch files are imported from.

**Example.** `D:\Attendance\Import`

**Why it matters.** The file import scans this folder for punch files.

**Rules.** Optional.

## TimeEventSyncBatchCount {#ManageTimeSettings.TimeEventSyncBatchCount}
<!-- alias: loc:TimeEventSyncBatchCount, dom:TimeSettings_TimeEventSyncBatchCount -->

**What it is.** How many punches are synced at a time.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Optional. Whole number.

## TimeEventSyncFreq {#ManageTimeSettings.TimeEventSyncFreq}
<!-- alias: loc:TimeEventSyncFreq, dom:TimeSettings_TimeEventSyncFreq -->

**What it is.** How often punches are synced.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Optional. Whole number.

## MidTierSyncBatchCount {#ManageTimeSettings.MidTierSyncBatchCount}
<!-- alias: loc:MidTierSyncBatchCount, dom:TimeSettings_MidTierSyncBatchCount -->

**What it is.** How many middle-tier punches are synced at a time.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Optional. Whole number.

## MidTierSyncFreq {#ManageTimeSettings.MidTierSyncFreq}
<!-- alias: loc:MidTierSyncFreq, dom:TimeSettings_MidTierSyncFreq -->

**What it is.** How often middle-tier punches are synced.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Optional. Whole number.

## ExcludePaidLeaveAccrualOnAbsent {#ManageTimeSettings.ExcludePaidLeaveAccrualOnAbsent}
<!-- alias: loc:ExcludePaidLeaveAccrualOnAbsent, dom:TimeSettings_ExcludePaidLeaveAccrualOnAbsent -->

**What it is.** Whether absent days earn no annual leave.

**Why it matters.** Ticked, the time engine accrues no annual leave for a day the employee was absent.

**Rules.** Yes or no.

## HourlyLeavelogicEnable {#ManageTimeSettings.HourlyLeavelogicEnable}
<!-- alias: loc:HourlyLeavelogicEnable, dom:TimeSettings_HourlyLeavelogicEnable -->

**What it is.** Whether hourly leave at the start or end of the shift moves the expected punch.

**Why it matters.** Ticked, for actual-time employees the time engine takes hourly leave into account when reading the clock-in and clock-out, so leaving early or arriving late on leave is not counted as lateness.

**Rules.** Yes or no.

## HourlyLeaveShouldSignIn {#ManageTimeSettings.HourlyLeaveShouldSignIn}
<!-- alias: loc:HourlyLeaveShouldSignIn, dom:TimeSettings_HourlyLeaveShouldSignIn -->

**What it is.** Whether the employee must punch in after hourly leave at the start of the shift.

**Why it matters.** Used with hourly leave logic.

**Rules.** Yes or no.

## HourlyLeaveShouldSignOut {#ManageTimeSettings.HourlyLeaveShouldSignOut}
<!-- alias: loc:HourlyLeaveShouldSignOut, dom:TimeSettings_HourlyLeaveShouldSignOut -->

**What it is.** Whether the employee must punch out before hourly leave at the end of the shift.

**Why it matters.** Used with hourly leave logic.

**Rules.** Yes or no.

## RestrictUnpaidleaveIfAnnualleaveAvailable {#ManageTimeSettings.RestrictUnpaidleaveIfAnnualleaveAvailable}
<!-- alias: loc:RestrictUnpaidleaveIfAnnualleaveAvailable, dom:TimeSettings_RestrictUnpaidleaveIfAnnualleaveAvailable -->

**What it is.** Whether unpaid leave is refused while annual leave remains.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## AllowPaidleaveBackdateEntry {#ManageTimeSettings.AllowPaidleaveBackdateEntry}
<!-- alias: loc:AllowPaidleaveBackdateEntry, dom:TimeSettings_AllowPaidleaveBackdateEntry -->

**What it is.** Whether paid leave may be entered back-dated.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## SickLeaveAttachMandatory {#ManageTimeSettings.SickLeaveAttachMandatory}
<!-- alias: loc:SickLeaveAttachMandatory, dom:TimeSettings_SickLeaveAttachMandatory -->

**What it is.** Whether sick leave needs an attachment.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## EnableAutoReturnInConsecutiveLeave {#ManageTimeSettings.EnableAutoReturnInConsecutiveLeave}
<!-- alias: loc:EnableAutoReturnInConsecutiveLeave, dom:TimeSettings_EnableAutoReturnInConsecutiveLeave -->

**What it is.** Whether return is recorded automatically between consecutive leaves.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## AllowFutureDatedReturnFromLeave {#ManageTimeSettings.AllowFutureDatedReturnFromLeave}
<!-- alias: loc:AllowFutureDatedReturnFromLeave, dom:TimeSettings_AllowFutureDatedReturnFromLeave -->

**What it is.** Whether a return from leave may be dated in the future.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## EnableAutoHalfDayLeave {#ManageTimeSettings.EnableAutoHalfDayLeave}
<!-- alias: loc:EnableAutoHalfDayLeave, dom:TimeSettings_EnableAutoHalfDayLeave -->

**What it is.** Whether half-day leave is created automatically.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## EnableSettleFractionalLeaveBalance {#ManageTimeSettings.EnableSettleFractionalLeaveBalance}
<!-- alias: loc:EnableSettleFractionalLeaveBalance, dom:TimeSettings_EnableSettleFractionalLeaveBalance -->

**What it is.** Whether fractional leave balances are settled.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## EnableBookingQuotaInLeaveRequest {#ManageTimeSettings.EnableBookingQuotaInLeaveRequest}
<!-- alias: loc:EnableBookingQuotaInLeaveRequest, dom:TimeSettings_EnableBookingQuotaInLeaveRequest -->

**What it is.** Whether a leave request books quota straight away.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## LeaveDeductionStartMonth {#ManageTimeSettings.LeaveDeductionStartMonth}
<!-- alias: loc:LeaveDeductionStartMonth, dom:TimeSettings_LeaveDeductionStartMonth -->

**What it is.** Whether leave is deducted from the month it starts in.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Optional. Whole number.

## ForfeitRule {#ManageTimeSettings.ForfeitRule}
<!-- alias: loc:ForfeitRule, dom:TimeSettings_ForfeitRule -->

**What it is.** When annual leave above the grade's maximum is forfeited.

**Example.** None

**Why it matters.** *None* cuts the balance back to the maximum day by day as the time engine runs. *Monthly* and *Yearly* let it run over and post a forfeit at the end of each month or year. The leave provision and annual leave reports follow the same rule.

**Rules.** Required. One of: None, Monthly, Yearly.

## AbsenceViolationRuleId {#ManageTimeSettings.AbsenceViolationRuleId}
<!-- alias: loc:AbsenceViolationRuleId, dom:TimeSettings_AbsenceViolationRuleId -->

**What it is.** The absence violation rule applied to employees.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Optional. Chosen from a list.

## AbsenceViolationEffectiveDate {#ManageTimeSettings.AbsenceViolationEffectiveDate}
<!-- alias: loc:AbsenceViolationEffectiveDate, dom:TimeSettings_AbsenceViolationEffectiveDate -->

**What it is.** The date from which absence violations are checked.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Optional. Date value.

## EnableOvertimeInTimeEngine {#ManageTimeSettings.EnableOvertimeInTimeEngine}
<!-- alias: loc:EnableOvertimeInTimeEngine, dom:TimeSettings_EnableOvertimeInTimeEngine -->

**What it is.** Whether the time engine works out overtime.

**Why it matters.** Unticked, no overtime minutes or overtime records are produced, whatever the duties and policies say.

**Rules.** Yes or no.

## EnableIsTravelling {#ManageTimeSettings.EnableIsTravelling}
<!-- alias: loc:EnableIsTravelling, dom:TimeSettings_EnableIsTravelling -->

**What it is.** Whether employees can be marked as travelling.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## EnablePayFullWithPayroll {#ManageTimeSettings.EnablePayFullWithPayroll}
<!-- alias: loc:EnablePayFullWithPayroll, dom:TimeSettings_EnablePayFullWithPayroll -->

**What it is.** Whether leave can be paid in full with payroll.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## EnableTimeRegisterApproval {#ManageTimeSettings.EnableTimeRegisterApproval}
<!-- alias: loc:EnableTimeRegisterApproval, dom:TimeSettings_EnableTimeRegisterApproval -->

**What it is.** Whether time registers must be approved before payroll.

**Why it matters.** Ticked, payroll uses the approved figures and refuses to run for an employee with any unapproved day in the period.

**Rules.** Yes or no.

## AutoApproveTimeLog {#ManageTimeSettings.AutoApproveTimeLog}
<!-- alias: loc:AutoApproveTimeLog, dom:TimeSettings_AutoApproveTimeLog -->

**What it is.** Whether days with nothing to review are approved automatically.

**Why it matters.** Used with time register approval: the time engine approves a day itself when there is nothing on it worth approving.

**Rules.** Yes or no.

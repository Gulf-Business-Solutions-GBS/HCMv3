---
helpId: tm.time-register.edit
lang: en
title: Time Registers — Create / Edit
entity: HCMv3.TimeManagement.TimeRegister
surfaces:
  - angular/src/app/admin/time-management/time-registers/create-or-edit-time-register-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.TimeRegisters.Create]
---

## Overview

One employee's day as the time engine worked it out: the duty and policy that applied, the planned and actual times, the punches used, and what the day counts as -- worked, absent, on leave, holiday, weekly off -- with late, early and overtime minutes. Payroll takes its day counts from these rows.

The time engine writes and rewrites these rows when it runs; edit one by hand only to correct a day, and expect a later run to recalculate it.

When the time setting *EnableTimeRegisterApproval* is on, payroll uses the **Approved** figures and refuses to run for an employee who has any day in the period that is not approved.

## Employee {#TimeRegister.EmployeeId}
<!-- alias: loc:Employee, dom:TimeRegister_EmployeeId -->

**What it is.** The employee.

**Rules.** Required. Chosen from the employee list.

## DayDate {#TimeRegister.DayDate}
<!-- alias: loc:DayDate, dom:TimeRegister_DayDate -->

**What it is.** The day.

**Example.** `2026-04-14`

**Rules.** Required. Date value.

## DutyPeriod {#TimeRegister.DutyPeriodId}
<!-- alias: loc:DutyPeriod, dom:TimeRegister_DutyPeriodId -->

**What it is.** The duty period that applied.

**Rules.** Optional. Chosen from Duty Period master data.

## Duty {#TimeRegister.DutyId}
<!-- alias: loc:Duty, dom:TimeRegister_DutyId -->

**What it is.** The duty that applied.

**Rules.** Optional. Chosen from Duty master data.

## TimePolicy {#TimeRegister.TimePolicyId}
<!-- alias: loc:TimePolicy, dom:TimeRegister_TimePolicyId -->

**What it is.** The time policy that applied.

**Rules.** Optional. Chosen from Time Policy master data.

## ShiftStartTime {#TimeRegister.ShiftStartTime1}
<!-- alias: loc:ShiftStartTime, dom:TimeRegister_ShiftStartTime1 -->

**What it is.** The planned shift start.

**Example.** `07:30`

**Rules.** Optional. Date value.

## ShiftEndTime {#TimeRegister.ShiftEndTime1}
<!-- alias: loc:ShiftEndTime, dom:TimeRegister_ShiftEndTime1 -->

**What it is.** The planned shift end.

**Example.** `14:30`

**Rules.** Optional. Date value.

## BreakStartTime {#TimeRegister.BreakStartTime}
<!-- alias: loc:BreakStartTime, dom:TimeRegister_BreakStartTime -->

**What it is.** The planned break start.

**Rules.** Optional. Date value.

## BreakEndTime {#TimeRegister.BreakEndTime}
<!-- alias: loc:BreakEndTime, dom:TimeRegister_BreakEndTime -->

**What it is.** The planned break end.

**Rules.** Optional. Date value.

## ActualIn {#TimeRegister.ActualIn}
<!-- alias: loc:ActualIn, dom:TimeRegister_ActualIn -->

**What it is.** The clock-in punch used.

**Example.** `07:42`

**Rules.** Optional. Date value.

## ActualOut {#TimeRegister.ActualOut}
<!-- alias: loc:ActualOut, dom:TimeRegister_ActualOut -->

**What it is.** The clock-out punch used.

**Example.** `14:35`

**Rules.** Optional. Date value.

## BreakOut {#TimeRegister.BreakOut}
<!-- alias: loc:BreakOut, dom:TimeRegister_BreakOut -->

**What it is.** The out-for-break punch used.

**Rules.** Optional. Date value.

## BreakIn {#TimeRegister.BreakIn}
<!-- alias: loc:BreakIn, dom:TimeRegister_BreakIn -->

**What it is.** The back-from-break punch used.

**Rules.** Optional. Date value.

## ActualStartTime {#TimeRegister.ActualStartTime1}
<!-- alias: loc:ActualStartTime, dom:TimeRegister_ActualStartTime1 -->

**What it is.** The start of counted working time -- the later of the shift start and the clock-in.

**Rules.** Optional. Date value.

## ActualEndTime {#TimeRegister.ActualEndTime1}
<!-- alias: loc:ActualEndTime, dom:TimeRegister_ActualEndTime1 -->

**What it is.** The end of counted working time.

**Rules.** Optional. Date value.

## ActualBreakStartTime {#TimeRegister.ActualBreakStartTime}
<!-- alias: loc:ActualBreakStartTime, dom:TimeRegister_ActualBreakStartTime -->

**What it is.** The counted start of the break.

**Rules.** Optional. Date value.

## ActualBreakEndTime {#TimeRegister.ActualBreakEndTime}
<!-- alias: loc:ActualBreakEndTime, dom:TimeRegister_ActualBreakEndTime -->

**What it is.** The counted end of the break.

**Rules.** Optional. Date value.

## PlannedWorkingMins {#TimeRegister.PlannedWorkingMins}
<!-- alias: loc:PlannedWorkingMins, dom:TimeRegister_PlannedWorkingMins -->

**What it is.** The minutes the employee was planned to work.

**Example.** `420`

**Rules.** Optional. Number.

## ActualWorkingMins {#TimeRegister.ActualWorkingMins}
<!-- alias: loc:ActualWorkingMins, dom:TimeRegister_ActualWorkingMins -->

**What it is.** The minutes worked within the shift.

**Example.** `408`

**Rules.** Optional. Number.

## TotalWorkingMins {#TimeRegister.TotalWorkingMins}
<!-- alias: loc:TotalWorkingMins, dom:TimeRegister_TotalWorkingMins -->

**What it is.** All minutes between clock-in and clock-out, less the break, plus attendance minutes.

**Example.** `413`

**Rules.** Optional. Number.

## ActualAbsenceMins {#TimeRegister.ActualAbsenceMins}
<!-- alias: loc:ActualAbsenceMins, dom:TimeRegister_ActualAbsenceMins -->

**What it is.** The planned minutes the employee was absent for.

**Example.** `12`

**Rules.** Optional. Number.

## PlannedWorkingDay {#TimeRegister.PlannedWorkingDay}
<!-- alias: loc:PlannedWorkingDay, dom:TimeRegister_PlannedWorkingDay -->

**What it is.** 1 if the day was a planned working day.

**Example.** `1`

**Rules.** Optional. Number.

## ActualWorkingDay {#TimeRegister.ActualWorkingDay}
<!-- alias: loc:ActualWorkingDay, dom:TimeRegister_ActualWorkingDay -->

**What it is.** The part of the day actually worked.

**Example.** `1`

**Rules.** Optional. Number.

## AttendanceDay {#TimeRegister.AttendanceDay}
<!-- alias: loc:AttendanceDay, dom:TimeRegister_AttendanceDay -->

**What it is.** 1 if the day was recorded as attendance (business trip, training).

**Rules.** Optional. Number.

## AttendanceType {#TimeRegister.AttendanceTypeId}
<!-- alias: loc:AttendanceType, dom:TimeRegister_AttendanceTypeId -->

**What it is.** The attendance type, on an attendance day.

**Rules.** Optional. Chosen from a list.

## AttendanceMinutes {#TimeRegister.AttendanceMinutes}
<!-- alias: loc:AttendanceMinutes, dom:TimeRegister_AttendanceMinutes -->

**What it is.** Minutes counted for attendance.

**Rules.** Optional. Number.

## AbsentDay {#TimeRegister.AbsentDay}
<!-- alias: loc:AbsentDay, dom:TimeRegister_AbsentDay -->

**What it is.** 1 if the employee was absent.

**Rules.** Optional. Number.

## ActiveDay {#TimeRegister.ActiveDay}
<!-- alias: loc:ActiveDay, dom:TimeRegister_ActiveDay -->

**What it is.** 1 if the employee was employed on the day.

**Example.** `1`

**Rules.** Optional. Number.

## InactiveDay {#TimeRegister.InactiveDay}
<!-- alias: loc:InactiveDay, dom:TimeRegister_InactiveDay -->

**What it is.** 1 if the employee was not yet employed or had left.

**Rules.** Optional. Number.

## LeaveDay {#TimeRegister.LeaveDay}
<!-- alias: loc:LeaveDay, dom:TimeRegister_LeaveDay -->

**What it is.** The part of the day on leave.

**Rules.** Optional. Number.

## LeaveType {#TimeRegister.LeaveTypeId}
<!-- alias: loc:LeaveType, dom:TimeRegister_LeaveTypeId -->

**What it is.** The leave type, on a leave day.

**Rules.** Optional. Chosen from a list.

## LeaveMinutes {#TimeRegister.LeaveMinutes}
<!-- alias: loc:LeaveMinutes, dom:TimeRegister_LeaveMinutes -->

**What it is.** Minutes of hourly leave.

**Rules.** Optional. Number.

## AnnualLeaveDay {#TimeRegister.AnnualLeaveDay}
<!-- alias: loc:AnnualLeaveDay, dom:TimeRegister_AnnualLeaveDay -->

**What it is.** 1 if the day was annual leave.

**Rules.** Optional. Number.

## UnpaidLeaveDay {#TimeRegister.UnpaidLeaveDay}
<!-- alias: loc:UnpaidLeaveDay, dom:TimeRegister_UnpaidLeaveDay -->

**What it is.** 1 if the day was unpaid leave.

**Rules.** Optional. Number.

## SickLeavePaidDay {#TimeRegister.SickLeavePaidDay}
<!-- alias: loc:SickLeavePaidDay, dom:TimeRegister_SickLeavePaidDay -->

**What it is.** The paid part of a sick leave day.

**Rules.** Optional. Number.

## SickLeaveUnpaidDay {#TimeRegister.SickLeaveUnpaidDay}
<!-- alias: loc:SickLeaveUnpaidDay, dom:TimeRegister_SickLeaveUnpaidDay -->

**What it is.** The unpaid part of a sick leave day.

**Rules.** Optional. Number.

## PublicHoliday {#TimeRegister.PublicHoliday}
<!-- alias: loc:PublicHoliday, dom:TimeRegister_PublicHoliday -->

**What it is.** 1 if the day was a public holiday in the employee's calendar.

**Rules.** Optional. Number.

## WeeklyOff {#TimeRegister.WeeklyOff}
<!-- alias: loc:WeeklyOff, dom:TimeRegister_WeeklyOff -->

**What it is.** 1 if the day was a weekly off.

**Rules.** Optional. Number.

## RestDay {#TimeRegister.RestDay}
<!-- alias: loc:RestDay, dom:TimeRegister_RestDay -->

**What it is.** 1 if the day was a rest day.

**Rules.** Optional. Number.

## AnnualLeaveAccrualPerDay {#TimeRegister.AnnualLeaveAccrualPerDay}
<!-- alias: loc:AnnualLeaveAccrualPerDay, dom:TimeRegister_AnnualLeaveAccrualPerDay -->

**What it is.** Annual leave earned on this day.

**Example.** `0.0822`

**Rules.** Optional. Number.

## TicketAccrualPerDay {#TimeRegister.TicketAccrualPerDay}
<!-- alias: loc:TicketAccrualPerDay, dom:TimeRegister_TicketAccrualPerDay -->

**What it is.** Air ticket entitlement earned on this day.

**Example.** `0.0027`

**Rules.** Optional. Number.

## AirTicketPaidDay {#TimeRegister.AirTicketPaidDay}
<!-- alias: loc:AirTicketPaidDay, dom:TimeRegister_AirTicketPaidDay -->

**What it is.** 1 if an air ticket was paid on this day.

**Rules.** Optional. Number.

## LeaveAdjDay {#TimeRegister.LeaveAdjDay}
<!-- alias: loc:LeaveAdjDay, dom:TimeRegister_LeaveAdjDay -->

**What it is.** A leave adjustment counted on this day.

**Rules.** Optional. Number.

## IncludeInLeavePay {#TimeRegister.IncludeInLeavePay}
<!-- alias: loc:IncludeInLeavePay, dom:TimeRegister_IncludeInLeavePay -->

**What it is.** The part of the day paid through leave pay rather than basic pay.

**Rules.** Optional. Number.

## OvertimeMinutes {#TimeRegister.OvertimeMinutes}
<!-- alias: loc:OvertimeMinutes, dom:TimeRegister_OvertimeMinutes -->

**What it is.** Overtime minutes counted, after the policy's minimum.

**Example.** `45`

**Rules.** Optional. Number.

## OvertimeType {#TimeRegister.OvertimeTypeId}
<!-- alias: loc:OvertimeType, dom:TimeRegister_OvertimeTypeId -->

**What it is.** The overtime type the overtime is recorded under.

**Rules.** Optional. Chosen from Overtime Type master data.

## LateIn {#TimeRegister.LateIn}
<!-- alias: loc:LateIn, dom:TimeRegister_LateIn -->

**What it is.** Minutes late at the start of the shift.

**Example.** `12`

**Rules.** Optional. Number.

## DeductionLateIn {#TimeRegister.DeductionLateIn}
<!-- alias: loc:DeductionLateIn, dom:TimeRegister_DeductionLateIn -->

**What it is.** Late minutes to deduct, after tolerance.

**Example.** `2`

**Rules.** Optional. Number.

## EarlyOut {#TimeRegister.EarlyOut}
<!-- alias: loc:EarlyOut, dom:TimeRegister_EarlyOut -->

**What it is.** Minutes left early at the end of the shift.

**Rules.** Optional. Number.

## DeductionEarlyOut {#TimeRegister.DeductionEarlyOut}
<!-- alias: loc:DeductionEarlyOut, dom:TimeRegister_DeductionEarlyOut -->

**What it is.** Early-leaving minutes to deduct, after tolerance.

**Rules.** Optional. Number.

## BreakEarlyOut {#TimeRegister.BreakEarlyOut}
<!-- alias: loc:BreakEarlyOut, dom:TimeRegister_BreakEarlyOut -->

**What it is.** Minutes left early for the break.

**Rules.** Optional. Number.

## BreakLateIn {#TimeRegister.BreakLateIn}
<!-- alias: loc:BreakLateIn, dom:TimeRegister_BreakLateIn -->

**What it is.** Minutes late back from the break.

**Rules.** Optional. Number.

## BreakDeductionEarlyOut {#TimeRegister.BreakDeductionEarlyOut}
<!-- alias: loc:BreakDeductionEarlyOut, dom:TimeRegister_BreakDeductionEarlyOut -->

**What it is.** Break early-out minutes to deduct.

**Rules.** Optional. Number.

## BreakDeductionLateIn {#TimeRegister.BreakDeductionLateIn}
<!-- alias: loc:BreakDeductionLateIn, dom:TimeRegister_BreakDeductionLateIn -->

**What it is.** Break late-in minutes to deduct.

**Rules.** Optional. Number.

## DedLateInEarlyOutMins {#TimeRegister.DedLateInEarlyOutMins}
<!-- alias: loc:DedLateInEarlyOutMins, dom:TimeRegister_DedLateInEarlyOutMins -->

**What it is.** All late and early minutes to deduct for the day.

**Rules.** Optional. Number.

## DedLateInEarlyOutDay {#TimeRegister.DedLateInEarlyOutDay}
<!-- alias: loc:DedLateInEarlyOutDay, dom:TimeRegister_DedLateInEarlyOutDay -->

**What it is.** Those minutes as a part of a day.

**Rules.** Optional. Number.

## DedAfterMonthlyTol {#TimeRegister.DedAfterMonthlyTol}
<!-- alias: loc:DedAfterMonthlyTol, dom:TimeRegister_DedAfterMonthlyTol -->

**What it is.** The month's late and early minutes above the monthly tolerance, carried on one day of the month.

**Rules.** Optional. Number.

## Status {#TimeRegister.Status}
<!-- alias: loc:Status, dom:TimeRegister_Status -->

**What it is.** Whether the day is open, approved or reopened.

**Example.** Opened

**Why it matters.** With time register approval switched on, payroll needs every day in the period approved.

**Rules.** Required. One of: Opened, Approved, Reopened.

## ApprovedActualWorkingDay {#TimeRegister.ApprovedActualWorkingDay}
<!-- alias: loc:ApprovedActualWorkingDay, dom:TimeRegister_ApprovedActualWorkingDay -->

**What it is.** The approved figure for the day worked, used by payroll when approval is on.

**Rules.** Optional. Number.

## ApprovedAbsentDay {#TimeRegister.ApprovedAbsentDay}
<!-- alias: loc:ApprovedAbsentDay, dom:TimeRegister_ApprovedAbsentDay -->

**What it is.** The approved figure for absence, used by payroll when approval is on.

**Rules.** Optional. Number.

## ApprovedDedLateInEarlyOutMins {#TimeRegister.ApprovedDedLateInEarlyOutMins}
<!-- alias: loc:ApprovedDedLateInEarlyOutMins, dom:TimeRegister_ApprovedDedLateInEarlyOutMins -->

**What it is.** The approved late and early minutes to deduct.

**Rules.** Optional. Number.

## Remarks {#TimeRegister.Remarks}
<!-- alias: loc:Remarks, dom:TimeRegister_Remarks -->

**What it is.** A note on the day.

**Rules.** Optional. Maximum 1000 characters.

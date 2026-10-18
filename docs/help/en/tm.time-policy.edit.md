---
helpId: tm.time-policy.edit
lang: en
title: Time Policies — Create / Edit
entity: HCMv3.TimeManagement.TimePolicy
surfaces:
  - angular/src/app/admin/time-management/time-policies/create-or-edit-time-policy-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.TimePolicies.Create]
---

## Overview

How attendance is worked out for the employees on the policy: whether it comes from punches, a plan or timesheets, which punches are required, how late arrival and early leaving are tolerated, and the minimum overtime.

Employees get a time policy on their own time policy records. The **Time Management Status** decides the method: *Actual Time* reads punches around the shift start and end; *Flexi Time* reads them inside the duty's flexi window; *Planned Time* takes the planned shift as worked; anything else, including *Time Sheet*, is worked out from timesheets. *Variable Time* is not processed by the time engine -- days on it are skipped with a message.

The punch-location and punch-notification settings at the end of the form are not read at present.

## TimeMgmtStatus {#TimePolicy.TimeMgmtStatus}
<!-- alias: loc:TimeMgmtStatus, dom:TimePolicy_TimeMgmtStatus -->

**What it is.** How attendance is worked out for employees on this policy.

**Example.** Actual Time

**Why it matters.** See the overview for what each status does.

**Rules.** Required. One of: N/A, Planned Time, Actual Time, Time Sheet, Flexi Time, Variable Time.

## TimeEventRule {#TimePolicy.TimeEventRule}
<!-- alias: loc:TimeEventRule, dom:TimePolicy_TimeEventRule -->

**What it is.** Which punch counts when there are several in the window.

**Example.** First In Last Out

**Why it matters.** The first or last clock-in, and the first or last clock-out, as named. *N/A* takes no punch at all, so every day looks unpunched.

**Rules.** Required. One of: N/A, First In First Out, First In Last Out, Last In First Out, Last In Last Out.

## MonthlyToleranceType {#TimePolicy.MonthlyToleranceType}
<!-- alias: loc:MonthlyToleranceType, dom:TimePolicy_MonthlyToleranceType -->

**What it is.** Whether lateness and early leaving are also tolerated across the month, and on what figure.

**Example.** Calculate Monthly Tolerance On Deduction

**Why it matters.** At the end of each month the time engine adds up the employee's lateness and early-leaving minutes -- the deducted minutes (*On Deduction*), or the deducted minutes plus each day's tolerance (*On Actual*) -- and only the total above **Monthly After Tolerance** is carried as the month's deduction. *Not selected* switches this off.

**Rules.** Required. One of: Not selected, Calculate Monthly Tolerance On Actual, Calculate Monthly Tolerance On Deduction.

## MonthlyAfterTolerance {#TimePolicy.MonthlyAfterTolerance}
<!-- alias: loc:MonthlyAfterTolerance, dom:TimePolicy_MonthlyAfterTolerance -->

**What it is.** The minutes of lateness and early leaving allowed across the month.

**Example.** `60`

**Why it matters.** Used with a monthly tolerance type; only the month's total above it is deducted.

**Rules.** Optional. Number.

## OtMinLimit {#TimePolicy.OTMinLimit}
<!-- alias: loc:OtMinLimit, dom:TimePolicy_OtMinLimit -->

**What it is.** The least overtime, in minutes, that counts.

**Example.** `30`

**Why it matters.** Overtime shorter than this on a day is ignored.

**Rules.** Optional. Number.

## MarkWeekoffOnHoliday {#TimePolicy.MarkWeekoffOnHoliday}
<!-- alias: loc:MarkWeekoffOnHoliday, dom:TimePolicy_MarkWeekoffOnHoliday -->

**What it is.** Whether a public holiday that falls on a weekly off counts as a weekly off.

**Why it matters.** Ticked, and only when basic pay (wagetype `1000`) is on *Fixed Days Per Month*, such a day is treated as a weekly off rather than a public holiday.

**Rules.** Yes or no.

## AllowPunchInFromWorkLocationOnly {#TimePolicy.AllowPunchInFromWorkLocationOnly}
<!-- alias: loc:AllowPunchInFromWorkLocationOnly, dom:TimePolicy_AllowPunchInFromWorkLocationOnly -->

**What it is.** Whether clock-in is allowed only from the work location.

**Why it matters.** Read by the self-service Geo Punch screen. When it is on, the employee's own work location (from their current organisation assignment) is offered and pre-selected for clock-in, and the punch is checked against that location's range. If no setting offers any work location, the employee may punch from anywhere.

**Rules.** Yes or no.

## AllowPunchOutFromWorkLocationOnly {#TimePolicy.AllowPunchOutFromWorkLocationOnly}
<!-- alias: loc:AllowPunchOutFromWorkLocationOnly, dom:TimePolicy_AllowPunchOutFromWorkLocationOnly -->

**What it is.** Whether clock-out is allowed only from the work location.

**Why it matters.** Read by the self-service Geo Punch screen. When it is on, the employee's own work location (from their current organisation assignment) is offered and pre-selected for clock-out, and the punch is checked against that location's range.

**Rules.** Yes or no.

## AllowPunchInFromExtWorklocation {#TimePolicy.AllowPunchInFromExtWorklocation}
<!-- alias: loc:AllowPunchInFromExtWorklocation, dom:TimePolicy_AllowPunchInFromExtWorklocation -->

**What it is.** Whether clock-in is allowed from an external work location.

**Why it matters.** Read by the self-service Geo Punch screen. When it is on, the employee's extended work locations that are valid today (including their last day) are offered for clock-in, and the punch is checked against the chosen location's range.

**Rules.** Yes or no.

## AllowPunchOutFromExtWorklocation {#TimePolicy.AllowPunchOutFromExtWorklocation}
<!-- alias: loc:AllowPunchOutFromExtWorklocation, dom:TimePolicy_AllowPunchOutFromExtWorklocation -->

**What it is.** Whether clock-out is allowed from an external work location.

**Why it matters.** Read by the self-service Geo Punch screen. When it is on, the employee's extended work locations that are valid today (including their last day) are offered for clock-out, and the punch is checked against the chosen location's range.

**Rules.** Yes or no.

## IsFaceRequired {#TimePolicy.IsFaceRequired}
<!-- alias: loc:IsFaceRequired, dom:TimePolicy_IsFaceRequired -->

**What it is.** Whether face recognition is required when punching.

**Why it matters.** Read by the self-service Geo Punch screen, together with the self-service setting Allow Geolocation Selfie: a selfie is required with each geo punch only when both are on.

**Rules.** Yes or no.

## ShouldSignIn {#TimePolicy.ShouldSignIn}
<!-- alias: loc:ShouldSignIn, dom:TimePolicy_ShouldSignIn -->

**What it is.** Whether employees on this policy must punch in at the start of the shift.

**Why it matters.** Ticked, a working day with this punch missing counts as absent for the planned working time, and lateness or early leaving is measured only when the punch exists. Unticked, the punch is not expected.

**Rules.** Yes or no.

## ShouldSignOut {#TimePolicy.ShouldSignOut}
<!-- alias: loc:ShouldSignOut, dom:TimePolicy_ShouldSignOut -->

**What it is.** Whether employees on this policy must punch out at the end of the shift.

**Why it matters.** Ticked, a working day with this punch missing counts as absent for the planned working time, and lateness or early leaving is measured only when the punch exists. Unticked, the punch is not expected.

**Rules.** Yes or no.

## CalcLateInAfterTolerance {#TimePolicy.CalcLateInAfterTolerance}
<!-- alias: loc:CalcLateInAfterTolerance, dom:TimePolicy_CalcLateInAfterTolerance -->

**What it is.** Whether lateness beyond the tolerance is deducted only for the minutes above it.

**Why it matters.** Ticked, a 15-minute late arrival with a 10-minute tolerance deducts 5 minutes. Unticked, it deducts all 15.

**Rules.** Yes or no.

## CalcEarlyOutAfterTolerance {#TimePolicy.CalcEarlyOutAfterTolerance}
<!-- alias: loc:CalcEarlyOutAfterTolerance, dom:TimePolicy_CalcEarlyOutAfterTolerance -->

**What it is.** Whether early leaving beyond the tolerance is deducted only for the minutes above it.

**Why it matters.** Ticked, a 15-minute late arrival with a 10-minute tolerance deducts 5 minutes. Unticked, it deducts all 15.

**Rules.** Yes or no.

## ClockInLimit {#TimePolicy.ClockInLimit}
<!-- alias: loc:ClockInLimit, dom:TimePolicy_ClockInLimit -->

**What it is.** How many minutes either side of the shift start the time engine looks for that punch.

**Example.** `120`

**Why it matters.** Punches outside the window are not taken as that punch. On public holidays, weekly offs and rest days the whole day is searched instead.

**Rules.** Optional. Number.

## ClockOutLimit {#TimePolicy.ClockOutLimit}
<!-- alias: loc:ClockOutLimit, dom:TimePolicy_ClockOutLimit -->

**What it is.** How many minutes either side of the shift end the time engine looks for that punch.

**Example.** `240`

**Why it matters.** Punches outside the window are not taken as that punch. On public holidays, weekly offs and rest days the whole day is searched instead.

**Rules.** Optional. Number.

## LateInTolerance {#TimePolicy.LateInTolerance}
<!-- alias: loc:LateInTolerance, dom:TimePolicy_LateInTolerance -->

**What it is.** The minutes of lateness allowed before anything is deducted.

**Example.** `10`

**Why it matters.** Lateness up to this many minutes is not deducted. Beyond it, the whole lateness is deducted -- or only the part above the tolerance, if the matching **after tolerance** box is ticked.

**Rules.** Optional. Number.

## EarlyOutTolerance {#TimePolicy.EarlyOutTolerance}
<!-- alias: loc:EarlyOutTolerance, dom:TimePolicy_EarlyOutTolerance -->

**What it is.** The minutes of early leaving allowed before anything is deducted.

**Example.** `10`

**Why it matters.** Early leaving up to this many minutes is not deducted. Beyond it, the whole early leaving is deducted -- or only the part above the tolerance, if the matching **after tolerance** box is ticked.

**Rules.** Optional. Number.

## MonthlyLateInTolerance {#TimePolicy.MonthlyLateInTolerance}
<!-- alias: loc:MonthlyLateInTolerance, dom:TimePolicy_MonthlyLateInTolerance -->

**What it is.** Monthly minutes of lateness allowed.

**Why it matters.** Used only by the custom monthly attendance with tolerance report (019); the time engine does not read it.

**Rules.** Optional. Number.

## MonthlyEarlyOutTolerance {#TimePolicy.MonthlyEarlyOutTolerance}
<!-- alias: loc:MonthlyEarlyOutTolerance, dom:TimePolicy_MonthlyEarlyOutTolerance -->

**What it is.** Monthly minutes of early leaving allowed.

**Why it matters.** Used only by the custom monthly attendance with tolerance report (019); the time engine does not read it.

**Rules.** Optional. Number.

## IsMissedPunchInNotiEnabled {#TimePolicy.IsMissedPunchInNotiEnabled}
<!-- alias: loc:IsMissedPunchInNotiEnabled, dom:TimePolicy_IsMissedPunchInNotiEnabled -->

**What it is.** Whether a missed clock-in sends a notification.

**Why it matters.** Not read at present.

**Rules.** Yes or no.

## MissedPunchInNotiTimeLimit {#TimePolicy.MissedPunchInNotiTimeLimit}
<!-- alias: loc:MissedPunchInNotiTimeLimit, dom:TimePolicy_MissedPunchInNotiTimeLimit -->

**What it is.** Minutes after the shift start before a missed clock-in is notified.

**Why it matters.** Not read at present.

**Rules.** Optional. Number.

## IsLatePunchInNotiEnabled {#TimePolicy.IsLatePunchInNotiEnabled}
<!-- alias: loc:IsLatePunchInNotiEnabled, dom:TimePolicy_IsLatePunchInNotiEnabled -->

**What it is.** Whether a late clock-in sends a notification.

**Why it matters.** Not read at present.

**Rules.** Yes or no.

## LatePunchInNotiTimeLimit {#TimePolicy.LatePunchInNotiTimeLimit}
<!-- alias: loc:LatePunchInNotiTimeLimit, dom:TimePolicy_LatePunchInNotiTimeLimit -->

**What it is.** Minutes of lateness before it is notified.

**Why it matters.** Not read at present.

**Rules.** Optional. Number.

## IsMissedPunchOutNotiEnabled {#TimePolicy.IsMissedPunchOutNotiEnabled}
<!-- alias: loc:IsMissedPunchOutNotiEnabled, dom:TimePolicy_IsMissedPunchOutNotiEnabled -->

**What it is.** Whether a missed clock-out sends a notification.

**Why it matters.** Not read at present.

**Rules.** Yes or no.

## MissedPunchOutNotiTimeLimit {#TimePolicy.MissedPunchOutNotiTimeLimit}
<!-- alias: loc:MissedPunchOutNotiTimeLimit, dom:TimePolicy_MissedPunchOutNotiTimeLimit -->

**What it is.** Minutes after the shift end before a missed clock-out is notified.

**Why it matters.** Not read at present.

**Rules.** Optional. Number.

## IsEarlyPunchOutNotiEnabled {#TimePolicy.IsEarlyPunchOutNotiEnabled}
<!-- alias: loc:IsEarlyPunchOutNotiEnabled, dom:TimePolicy_IsEarlyPunchOutNotiEnabled -->

**What it is.** Whether an early clock-out sends a notification.

**Why it matters.** Not read at present.

**Rules.** Yes or no.

## EarlyPunchOutNotiTimeLimit {#TimePolicy.EarlyPunchOutNotiTimeLimit}
<!-- alias: loc:EarlyPunchOutNotiTimeLimit, dom:TimePolicy_EarlyPunchOutNotiTimeLimit -->

**What it is.** Minutes of early leaving before it is notified.

**Why it matters.** Not read at present.

**Rules.** Optional. Number.

## ShouldBreakSignIn {#TimePolicy.ShouldBreakSignIn}
<!-- alias: loc:ShouldBreakSignIn, dom:TimePolicy_ShouldBreakSignIn -->

**What it is.** Whether employees on this policy must punch back in after the break.

**Why it matters.** Ticked, a working day with this punch missing counts as absent for the planned working time, and lateness or early leaving is measured only when the punch exists. Unticked, the punch is not expected.

**Rules.** Yes or no.

## BreakInLimit {#TimePolicy.BreakInLimit}
<!-- alias: loc:BreakInLimit, dom:TimePolicy_BreakInLimit -->

**What it is.** How many minutes either side of the break end the time engine looks for the back-from-break punch.

**Example.** `30`

**Rules.** Optional. Number.

## ShouldBreakSignOut {#TimePolicy.ShouldBreakSignOut}
<!-- alias: loc:ShouldBreakSignOut, dom:TimePolicy_ShouldBreakSignOut -->

**What it is.** Whether employees on this policy must punch out for the break.

**Why it matters.** Ticked, a working day with this punch missing counts as absent for the planned working time, and lateness or early leaving is measured only when the punch exists. Unticked, the punch is not expected.

**Rules.** Yes or no.

## BreakOutLimit {#TimePolicy.BreakOutLimit}
<!-- alias: loc:BreakOutLimit, dom:TimePolicy_BreakOutLimit -->

**What it is.** How many minutes either side of the break start the time engine looks for the out-for-break punch.

**Example.** `30`

**Rules.** Optional. Number.

## CalcBreakLateInAfterTolerance {#TimePolicy.CalcBreakLateInAfterTolerance}
<!-- alias: loc:CalcBreakLateInAfterTolerance, dom:TimePolicy_CalcBreakLateInAfterTolerance -->

**What it is.** Whether lateness back from the break beyond the tolerance is deducted only for the minutes above it.

**Why it matters.** Ticked, a 15-minute late arrival with a 10-minute tolerance deducts 5 minutes. Unticked, it deducts all 15.

**Rules.** Yes or no.

## BreakLateInTolerance {#TimePolicy.BreakLateInTolerance}
<!-- alias: loc:BreakLateInTolerance, dom:TimePolicy_BreakLateInTolerance -->

**What it is.** The minutes of lateness back from the break allowed before anything is deducted.

**Example.** `5`

**Why it matters.** Lateness back from the break up to this many minutes is not deducted. Beyond it, the whole lateness back from the break is deducted -- or only the part above the tolerance, if the matching **after tolerance** box is ticked.

**Rules.** Optional. Number.

## CalcBreakEarlyOutAfterTolerance {#TimePolicy.CalcBreakEarlyOutAfterTolerance}
<!-- alias: loc:CalcBreakEarlyOutAfterTolerance, dom:TimePolicy_CalcBreakEarlyOutAfterTolerance -->

**What it is.** Whether leaving early for the break beyond the tolerance is deducted only for the minutes above it.

**Why it matters.** Ticked, a 15-minute late arrival with a 10-minute tolerance deducts 5 minutes. Unticked, it deducts all 15.

**Rules.** Yes or no.

## BreakEarlyOutTolerance {#TimePolicy.BreakEarlyOutTolerance}
<!-- alias: loc:BreakEarlyOutTolerance, dom:TimePolicy_BreakEarlyOutTolerance -->

**What it is.** The minutes of leaving early for the break allowed before anything is deducted.

**Example.** `5`

**Why it matters.** Leaving early for the break up to this many minutes is not deducted. Beyond it, the whole leaving early for the break is deducted -- or only the part above the tolerance, if the matching **after tolerance** box is ticked.

**Rules.** Optional. Number.

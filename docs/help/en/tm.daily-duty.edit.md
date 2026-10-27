---
helpId: tm.daily-duty.edit
lang: en
title: Daily Duties — Create / Edit
entity: HCMv3.TimeManagement.DailyDuty
surfaces:
  - angular/src/app/admin/time-management/daily-duties/create-or-edit-daily-duty-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.DailyDuties.Create]
---

## Overview

A duty for the roster lists (roster duty periods and roster daily duties), with its shift times.

Not read at present: the time engine and payroll take employees' duties from duty periods and roster groups, not from this list. The roster generator that would have used it was never live in the legacy system and was not ported. The roster schedule viewer shows its times when one appears in a roster.

## StartTime {#DailyDuty.StartTime}
<!-- alias: loc:StartTime, dom:DailyDuty_StartTime -->

**What it is.** When the shift starts.

**Example.** `07:00`

**Rules.** Optional. Time of day.

## EndTime {#DailyDuty.EndTime}
<!-- alias: loc:EndTime, dom:DailyDuty_EndTime -->

**What it is.** When the shift ends.

**Example.** `19:00`

**Rules.** Optional. Time of day.

## BreakShift {#DailyDuty.BreakShiftId}
<!-- alias: loc:BreakShift, dom:DailyDuty_BreakShiftId -->

**What it is.** The break taken during this duty.

**Rules.** Optional. Chosen from Break Shift master data.

## IsNightShift {#DailyDuty.IsNightShift}
<!-- alias: loc:IsNightShift, dom:DailyDuty_IsNightShift -->

**What it is.** Whether the shift ends on the next day.

**Rules.** Yes or no.

## AllowTransStart {#DailyDuty.AllowTransStart}
<!-- alias: loc:AllowTransStart, dom:DailyDuty_AllowTransStart -->

**What it is.** Whether a leave may start on this duty's off days.

**Rules.** Yes or no.

## AllowTransEnd {#DailyDuty.AllowTransEnd}
<!-- alias: loc:AllowTransEnd, dom:DailyDuty_AllowTransEnd -->

**What it is.** Whether this duty's off days can count towards a leave.

**Rules.** Yes or no.

---
helpId: tm.duty.edit
lang: en
title: Duties — Create / Edit
entity: HCMv3.TimeManagement.Duty
surfaces:
  - angular/src/app/admin/time-management/duties/create-or-edit-duty-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.Duties.Create]
---

## Overview

One working day's pattern: when the shift starts and ends, its break, and whether the day is a working day, a weekly off or a rest day. Duties are arranged into duty periods, which give each employee a duty for each day.

## StartTime {#Duty.StartTime}
<!-- alias: loc:StartTime, dom:Duty_StartTime -->

**What it is.** When the shift starts.

**Example.** `07:30`

**Why it matters.** Lateness is measured from it, and it is the start of the planned working time.

**Rules.** Optional. Time of day.

## EndTime {#Duty.EndTime}
<!-- alias: loc:EndTime, dom:Duty_EndTime -->

**What it is.** When the shift ends.

**Example.** `14:30`

**Why it matters.** Early leaving is measured against it. For a night shift this time is on the next day.

**Rules.** Optional. Time of day.

## FlexiStartTime {#Duty.FlexiStartTime}
<!-- alias: loc:FlexiStartTime, dom:Duty_FlexiStartTime -->

**What it is.** The start of the window for reading punches on a flexible day.

**Example.** `06:00`

**Why it matters.** For employees whose time policy is flexi time or planned time, the time engine reads the day's clock-in and clock-out from between the flexi start and flexi end.

**Rules.** Optional. Time of day.

## FlexiEndTime {#Duty.FlexiEndTime}
<!-- alias: loc:FlexiEndTime, dom:Duty_FlexiEndTime -->

**What it is.** The end of the window for reading punches on a flexible day.

**Example.** `20:00`

**Why it matters.** An end earlier than the start means the window runs past midnight.

**Rules.** Optional. Time of day.

## BreakShift {#Duty.BreakShiftId}
<!-- alias: loc:BreakShift, dom:Duty_BreakShiftId -->

**What it is.** The break taken during this duty.

**Example.** Lunch 12:00–12:30

**Why it matters.** With a break, working time is counted before and after it, so the break is not counted as work; planned hours and hourly leave leave it out too.

**Rules.** Optional. Chosen from Break Shift master data.

## OvertimeType {#Duty.OvertimeTypeId}
<!-- alias: loc:OvertimeType, dom:Duty_OvertimeTypeId -->

**What it is.** The overtime type for overtime worked on this duty.

**Example.** Normal Overtime

**Why it matters.** Overtime found on a day of this duty is recorded under this type. For a grade that pays overtime, a day with overtime and no overtime type makes the time engine raise an error.

**Rules.** Optional. Chosen from Overtime Type master data.

## AllowTransStart {#Duty.AllowTransStart}
<!-- alias: loc:AllowTransStart, dom:Duty_AllowTransStart -->

**What it is.** Whether a leave may start on this duty's days when they are off days.

**Why it matters.** Not read at present.

**Rules.** Yes or no.

## AllowTransEnd {#Duty.AllowTransEnd}
<!-- alias: loc:AllowTransEnd, dom:Duty_AllowTransEnd -->

**What it is.** Whether weekly offs and rest days on this duty can count towards a leave's days.

**Why it matters.** When the system counts days forward to find where a leave ends, a weekly off or rest day on a duty without this ticked is skipped. Public holidays are always skipped.

**Rules.** Yes or no.

## IsNightShift {#Duty.IsNightShift}
<!-- alias: loc:IsNightShift, dom:Duty_IsNightShift -->

**What it is.** Whether the shift ends on the next day.

**Example.** Ticked for 22:00–06:00.

**Why it matters.** The end time, and a break that crosses midnight, are placed on the following day.

**Rules.** Yes or no.

## IsWeeklyOff {#Duty.IsWeeklyOff}
<!-- alias: loc:IsWeeklyOff, dom:Duty_IsWeeklyOff -->

**What it is.** Whether this duty is a weekly off day.

**Why it matters.** Weekly offs are counted for payroll day counts (for example the working days per month divisor), for leave that excludes weekends, and for the weekly-off and rest-day rules of pay.

**Rules.** Yes or no.

## IsRestDay {#Duty.IsRestDay}
<!-- alias: loc:IsRestDay, dom:Duty_IsRestDay -->

**What it is.** Whether this duty is a rest day.

**Why it matters.** Rest days are kept apart from weekly offs: leave types and attendance types decide separately whether a rest day counts, and some prorate methods pay rest days.

**Rules.** Yes or no.

## IsHalfDay {#Duty.IsHalfDay}
<!-- alias: loc:IsHalfDay, dom:Duty_IsHalfDay -->

**What it is.** Whether this duty is a half working day.

**Why it matters.** Shown on the roster schedule viewer only; the time engine does not treat the day differently.

**Rules.** Yes or no.

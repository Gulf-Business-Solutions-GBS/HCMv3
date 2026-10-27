---
helpId: tm.break-shift.edit
lang: en
title: Break Shifts — Create / Edit
entity: HCMv3.TimeManagement.BreakShift
surfaces:
  - angular/src/app/admin/time-management/break-shifts/create-or-edit-break-shift-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.BreakShifts.Create]
---

## Overview

A break within a duty -- for lunch or prayer -- with its start and end time. A duty with a break counts working time before and after the break, not across it.

## StartTime {#BreakShift.StartTime}
<!-- alias: loc:StartTime, dom:BreakShift_StartTime -->

**What it is.** When the break starts.

**Example.** `12:00`

**Rules.** Optional. Time of day.

## EndTime {#BreakShift.EndTime}
<!-- alias: loc:EndTime, dom:BreakShift_EndTime -->

**What it is.** When the break ends.

**Example.** `12:30`

**Rules.** Optional. Time of day.

## IsPaid {#BreakShift.IsPaid}
<!-- alias: loc:IsPaid, dom:BreakShift_IsPaid -->

**What it is.** Whether the break is paid.

**Why it matters.** Not read at present: the break is left out of working time whether this is ticked or not.

**Rules.** Yes or no.

## IsBreakStartNextDay {#BreakShift.IsBreakStartNextDay}
<!-- alias: loc:IsBreakStartNextDay, dom:BreakShift_IsBreakStartNextDay -->

**What it is.** On a night shift, whether the break starts on the next day.

**Example.** Ticked for a 02:00 break on a 22:00–06:00 shift.

**Rules.** Yes or no.

## IsBreakEndNextDay {#BreakShift.IsBreakEndNextDay}
<!-- alias: loc:IsBreakEndNextDay, dom:BreakShift_IsBreakEndNextDay -->

**What it is.** Whether the break ends on the next day.

**Why it matters.** Tick it for a break that crosses midnight. A break that starts on the next day is always treated as ending on it.

**Rules.** Yes or no.

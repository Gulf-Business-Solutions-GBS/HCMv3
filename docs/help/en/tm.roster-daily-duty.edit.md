---
helpId: tm.roster-daily-duty.edit
lang: en
title: Roster Daily Duties — Create / Edit
entity: HCMv3.TimeManagement.RosterDailyDuty
surfaces:
  - angular/src/app/admin/time-management/roster-daily-duties/create-or-edit-roster-daily-duty-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.RosterDailyDuties.Create]
---

## Overview

A daily duty on a date within a roster schedule.

Not read at present: the time engine and payroll take employees' duties from duty periods and roster groups, not from this list. The roster generator that would have used it was never live in the legacy system and was not ported.

## CalendarDate {#RosterDailyDuty.CalendarDate}
<!-- alias: loc:CalendarDate, dom:RosterDailyDuty_CalendarDate -->

**What it is.** The date.

**Example.** `2026-04-14`

**Rules.** Required. Date value.

## RosterDutySchedule {#RosterDailyDuty.RosterDutyScheduleId}
<!-- alias: loc:RosterDutySchedule, dom:RosterDailyDuty_RosterDutyScheduleId -->

**What it is.** The roster schedule the date belongs to.

**Rules.** Required. Chosen from Roster Duty Schedule master data.

## DailyDuty {#RosterDailyDuty.DailyDutyId}
<!-- alias: loc:DailyDuty, dom:RosterDailyDuty_DailyDutyId -->

**What it is.** The daily duty on that date.

**Rules.** Required. Chosen from Daily Duty master data.

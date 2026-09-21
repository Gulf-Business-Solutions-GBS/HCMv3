---
helpId: tm.roster-duty-schedule.edit
lang: en
title: Roster Duty Schedules — Create / Edit
entity: HCMv3.TimeManagement.RosterDutySchedule
surfaces:
  - angular/src/app/admin/time-management/roster-duty-schedules/create-or-edit-roster-duty-schedule-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.RosterDutySchedules.Create]
---

## Overview

A roster schedule: a duty period with a start point and holiday calendar, meant for generating roster daily duties.

Not read at present: the time engine and payroll take employees' duties from duty periods and roster groups, not from this list. The roster generator that would have used it was never live in the legacy system and was not ported.

## DutyPeriod {#RosterDutySchedule.DutyPeriodId}
<!-- alias: loc:DutyPeriod, dom:RosterDutySchedule_DutyPeriodId -->

**What it is.** The duty period the schedule follows.

**Rules.** Required. Chosen from Duty Period master data.

## StartPoint {#RosterDutySchedule.StartPoint}
<!-- alias: loc:StartPoint, dom:RosterDutySchedule_StartPoint -->

**What it is.** The day of the rotation the schedule starts on.

**Example.** `1`

**Rules.** Required. Whole number.

## HolidayCalendar {#RosterDutySchedule.HolidayCalendarId}
<!-- alias: loc:HolidayCalendar, dom:RosterDutySchedule_HolidayCalendarId -->

**What it is.** The holiday calendar for the schedule.

**Rules.** Optional. Chosen from Holiday Calendar master data.

---
helpId: tm.roster-group-schedule.edit
lang: en
title: Roster Group Schedules — Create / Edit
entity: HCMv3.TimeManagement.RosterGroupSchedule
surfaces:
  - angular/src/app/admin/time-management/roster-group-schedules/create-or-edit-roster-group-schedule-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.RosterGroupSchedules.Create]
---

## Overview

The duty period a roster group follows for a period, and where in the rotation it starts.

## RosterGroup {#RosterGroupSchedule.RosterGroupId}
<!-- alias: loc:RosterGroup, dom:RosterGroupSchedule_RosterGroupId -->

**What it is.** The roster group.

**Example.** Team A

**Rules.** Required. Chosen from Roster Group master data.

## DutyPeriod {#RosterGroupSchedule.DutyPeriodId}
<!-- alias: loc:DutyPeriod, dom:RosterGroupSchedule_DutyPeriodId -->

**What it is.** The duty period the group follows.

**Example.** 4-on 4-off Rotation

**Rules.** Required. Chosen from Duty Period master data.

## StartPoint {#RosterGroupSchedule.StartPoint}
<!-- alias: loc:StartPoint, dom:RosterGroupSchedule_StartPoint -->

**What it is.** The day of the rotation the group is on at the schedule's start date.

**Example.** `5` -- Team B starts halfway through an 8-day cycle.

**Why it matters.** Above 0, the group follows the duty period's week rotation from this day. 0 uses the duty period's weekday duties instead.

**Rules.** Required. Whole number.

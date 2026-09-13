---
helpId: pa.employee-duty-schedule.edit
lang: en
title: Employee Duty Schedules — Create / Edit
entity: HCMv3.TimeManagement.EmployeeDutySchedule
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/duty-schedules/create-or-edit-employee-duty-schedule-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.EmployeeDutySchedules.Create]
---

## Overview

Assigns the employee to a roster duty schedule for a period. Schedules for the same employee cannot overlap.

It is recorded for reference; the time engine and payroll take the employee's duties from duty periods and substitute duties, and do not read this assignment at present.

## RosterDutySchedule {#EmployeeDutySchedule.RosterDutyScheduleId}
<!-- alias: loc:RosterDutySchedule, dom:EmployeeDutySchedule_RosterDutyScheduleId -->

**What it is.** The roster duty schedule.

**Example.** Security Night Roster

**Rules.** Required. Chosen from Roster Duty Schedule master data.

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

The same records are listed for all employees under Time Management → Duty Schedule, which can be searched by employee code or name and by schedule code or name. Adding a record there asks for the employee; from the employee record the employee is already set.

## Employee {#EmployeeDutySchedule.EmployeeId}
<!-- alias: loc:Employee, dom:EmployeeDutySchedule_EmployeeId -->

**What it is.** The employee the schedule is assigned to. Shown only when adding a record from the all-employees Duty Schedule screen.

**Example.** 1024 - Fatima Al-Sabah

**Rules.** Required. Chosen from the employee list.

## RosterDutySchedule {#EmployeeDutySchedule.RosterDutyScheduleId}
<!-- alias: loc:RosterDutySchedule, dom:EmployeeDutySchedule_RosterDutyScheduleId -->

**What it is.** The roster duty schedule.

**Example.** Security Night Roster

**Rules.** Required. Chosen from Roster Duty Schedule master data.

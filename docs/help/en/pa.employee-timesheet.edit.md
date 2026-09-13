---
helpId: pa.employee-timesheet.edit
lang: en
title: Employee Timesheets — Create / Edit
entity: HCMv3.TimeManagement.EmployeeTimesheet
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/timesheets/create-or-edit-employee-timesheet-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.EmployeeTimesheets.Create]
---

## Overview

A manual timesheet for the employee on one date: actual in, out and break times. One timesheet per employee per date.

For employees whose time policy takes punches from timesheets, the time engine builds the day's in and break times from these entries, and reads the total hours.

## TransDate {#EmployeeTimesheet.TransDate}
<!-- alias: loc:TransDate, dom:EmployeeTimesheet_TransDate -->

**What it is.** The date the timesheet is for.

**Example.** 10 Sep 2026

**Rules.** Required. Date value.

:::admin
When building a day from timesheets, the time engine looks the timesheet up by date only, not by employee. On a date where more than one employee has a timesheet, an employee can be given another employee's times. This matches the legacy system and is awaiting a decision.
:::

## ActualStartTime {#EmployeeTimesheet.ActualStartTime}
<!-- alias: loc:ActualStartTime, dom:EmployeeTimesheet_ActualStartTime -->

**What it is.** The time the employee started work.

**Example.** 07:55

**Why it matters.** Used as the day's actual in time.

**Rules.** Optional. Time of day.

## ActualEndTime {#EmployeeTimesheet.ActualEndTime}
<!-- alias: loc:ActualEndTime, dom:EmployeeTimesheet_ActualEndTime -->

**What it is.** The time the employee finished work.

**Example.** 16:10

**Why it matters.** Recorded with the timesheet; the time engine does not read it at present.

**Rules.** Optional. Time of day.

## BreakStartTime {#EmployeeTimesheet.BreakStartTime}
<!-- alias: loc:BreakStartTime, dom:EmployeeTimesheet_BreakStartTime -->

**What it is.** The time the break started.

**Example.** 12:00

**Rules.** Optional. Time of day.

## BreakEndTime {#EmployeeTimesheet.BreakEndTime}
<!-- alias: loc:BreakEndTime, dom:EmployeeTimesheet_BreakEndTime -->

**What it is.** The time the break ended.

**Example.** 12:30

**Rules.** Optional. Time of day.

## TotalHours {#EmployeeTimesheet.TotalHours}
<!-- alias: loc:TotalHours, dom:EmployeeTimesheet_TotalHours -->

**What it is.** The total hours worked on the date.

**Example.** 7.75

**Why it matters.** The time engine reads it for this employee and date.

**Rules.** Optional. Number.

## Approved {#EmployeeTimesheet.Approved}
<!-- alias: loc:Approved, dom:EmployeeTimesheet_Approved -->

**What it is.** An approval marker recorded with the timesheet.

**Why it matters.** Recorded only; the time engine does not check it at present.

**Rules.** Optional. Whole number.

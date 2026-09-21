---
helpId: tm.time-log.edit
lang: en
title: Time Logs — Create / Edit
entity: HCMv3.TimeManagement.TimeLog
surfaces:
  - angular/src/app/admin/time-management/time-logs/create-or-edit-time-log-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.TimeLogs.Create]
---

## Overview

Entries written by a time engine run as it processes employees. The run history counts them to report successes and failures, and a failed entry for an employee also shows up when payroll runs the time engine automatically.

## RunTimeStamp {#TimeLog.RunTimeStamp}
<!-- alias: loc:RunTimeStamp, dom:TimeLog_RunTimeStamp -->

**What it is.** When the entry was written.

**Rules.** Optional. Date value.

## DayDate {#TimeLog.DayDate}
<!-- alias: loc:DayDate, dom:TimeLog_DayDate -->

**What it is.** The day being processed.

**Example.** `2026-04-14`

**Rules.** Optional. Date value.

## Employee {#TimeLog.EmployeeId}
<!-- alias: loc:Employee, dom:TimeLog_EmployeeId -->

**What it is.** The employee the entry is about, if any.

**Rules.** Optional. Chosen from the employee list.

## Status {#TimeLog.Status}
<!-- alias: loc:Status, dom:TimeLog_Status -->

**What it is.** The outcome: 0 is success, 1 is a failure.

**Example.** `0`

**Rules.** Optional. Whole number.

## NoOfEmployee {#TimeLog.NoOfEmployee}
<!-- alias: loc:NoOfEmployee, dom:TimeLog_NoOfEmployee -->

**What it is.** How many employees the run was processing.

**Why it matters.** Not read at present.

**Rules.** Optional. Whole number.

## StopFlag {#TimeLog.StopFlag}
<!-- alias: loc:StopFlag, dom:TimeLog_StopFlag -->

**What it is.** Whether the run was asked to stop.

**Why it matters.** The time engine stops when it finds an entry flagged, and the run is reported as aborted.

**Rules.** Yes or no.

## Message {#TimeLog.Message}
<!-- alias: loc:Message, dom:TimeLog_Message -->

**What it is.** What the run recorded.

**Example.** Employee has VariableTime Policy found on 14/04/2026

**Rules.** Optional.

---
helpId: pay.payroll-log.edit
lang: en
title: Payroll Logs — Create / Edit
entity: HCMv3.PayrollManagement.PayrollLog
surfaces:
  - angular/src/app/admin/payroll-management/payroll-logs/create-or-edit-payroll-log-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.PayrollLogs.Create]
---

## Overview

Entries written by a payroll run as it processes employees. The run history on the Run Payroll page counts these to report how many succeeded and failed.

## Message {#PayrollLog.Message}
<!-- alias: loc:Message, dom:PayrollLog_Message -->

**What it is.** What the run recorded.

**Example.** Employee 1002: Payroll Control Record not locked or is not current.

**Rules.** Optional.

## RunTimeStamp {#PayrollLog.RunTimeStamp}
<!-- alias: loc:RunTimeStamp, dom:PayrollLog_RunTimeStamp -->

**What it is.** When the entry was written.

**Rules.** Required. Date value.

## EmployeeId {#PayrollLog.EmployeeId}
<!-- alias: loc:EmployeeId, dom:PayrollLog_EmployeeId -->

**What it is.** The employee the entry is about, if any.

**Rules.** Optional. Chosen from the employee list.

## Status {#PayrollLog.Status}
<!-- alias: loc:Status, dom:PayrollLog_Status -->

**What it is.** The outcome: 0 is success, anything else a failure.

**Example.** `0`

**Rules.** Optional. Whole number.

## InPeriod {#PayrollLog.InPeriod}
<!-- alias: loc:InPeriod, dom:PayrollLog_InPeriod -->

**What it is.** The pay month of the run.

**Example.** `2026-04-30`

**Rules.** Required. Date value.

## NoOfEmployee {#PayrollLog.NoOfEmployee}
<!-- alias: loc:NoOfEmployee, dom:PayrollLog_NoOfEmployee -->

**What it is.** How many employees the run was processing.

**Example.** `240`

**Rules.** Optional. Whole number.

## StopFlag {#PayrollLog.StopFlag}
<!-- alias: loc:StopFlag, dom:PayrollLog_StopFlag -->

**What it is.** Whether the run was asked to stop.

**Why it matters.** A run with any entry flagged is reported as aborted.

**Rules.** Yes or no.

---
helpId: pa.employee-task.edit
lang: en
title: Employee Tasks — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeTask
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/tasks/create-or-edit-employee-task-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeTasks.Create]
---

## Overview

A task or follow-up for the employee, with a reminder date.

It is recorded for reference; no reminder is sent from it and nothing reads it at present.

## TaskTypeId {#EmployeeTask.TaskTypeId}
<!-- alias: loc:TaskTypeId, dom:EmployeeTask_TaskTypeId -->

**What it is.** The kind of task.

**Example.** Probation review

**Rules.** Required. Chosen from Task Type master data.

:::admin
Maintained under General Master Info → Task Types.
:::

## TaskDate {#EmployeeTask.TaskDate}
<!-- alias: loc:TaskDate, dom:EmployeeTask_TaskDate -->

**What it is.** The date the task is due.

**Example.** 30 Nov 2026

**Rules.** Required. Date value.

## TaskStatus {#EmployeeTask.TaskStatus}
<!-- alias: loc:TaskStatus, dom:EmployeeTask_TaskStatus -->

**What it is.** The task's status, as a number.

**Example.** 0

**Rules.** Optional. Whole number.

## ReminderDate {#EmployeeTask.ReminderDate}
<!-- alias: loc:ReminderDate, dom:EmployeeTask_ReminderDate -->

**What it is.** The date to be reminded.

**Example.** 23 Nov 2026

**Why it matters.** No reminder is sent from it at present.

**Rules.** Required. Date value.

## MonitoringType {#EmployeeTask.MonitoringType}
<!-- alias: loc:MonitoringType, dom:EmployeeTask_MonitoringType -->

**What it is.** How the task is monitored, as a number.

**Example.** 0

**Rules.** Optional. Whole number.

## MonitoringCount {#EmployeeTask.MonitoringCount}
<!-- alias: loc:MonitoringCount, dom:EmployeeTask_MonitoringCount -->

**What it is.** How many monitoring units.

**Example.** 1

**Rules.** Optional. Whole number.

## MonitoringUnit {#EmployeeTask.MonitoringUnit}
<!-- alias: loc:MonitoringUnit, dom:EmployeeTask_MonitoringUnit -->

**What it is.** The monitoring unit, as a number.

**Example.** 0

**Rules.** Optional. Whole number.

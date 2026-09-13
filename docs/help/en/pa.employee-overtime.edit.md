---
helpId: pa.employee-overtime.edit
lang: en
title: Employee Overtimes — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeOvertime
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/overtimes/create-or-edit-employee-overtime-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeOvertimes.Create]
---

## Overview

Overtime for the employee on one date: hours requested, hours the time engine measured, and hours approved. One overtime record per employee per date.

Payroll pays the approved hours multiplied by the overtime type's rate. The time engine creates these records itself when it finds overtime, and fills in the approved hours from the measured hours if none were approved.

## EffectiveDate {#EmployeeOvertime.EffectiveDate}
<!-- alias: loc:EffectiveDate, dom:EmployeeOvertime_EffectiveDate -->

**What it is.** The date the overtime was worked.

**Example.** 12 Sep 2026

**Why it matters.** Payroll pays overtime falling within the pay period.

**Rules.** Required. Date value.

## OvertimeTypeId {#EmployeeOvertime.OvertimeTypeId}
<!-- alias: loc:OvertimeTypeId, dom:EmployeeOvertime_OvertimeTypeId -->

**What it is.** The kind of overtime, which sets the rate.

**Example.** Weekend Overtime

**Rules.** Required. Chosen from Overtime Type master data.

:::admin
Maintained under General Master Info → Overtime Types.
:::

## OvertimeHours {#EmployeeOvertime.OvertimeHours}
<!-- alias: loc:OvertimeHours, dom:EmployeeOvertime_OvertimeHours -->

**What it is.** The hours requested.

**Example.** 4

**Why it matters.** Shown in the approval request. Payroll does not pay these hours directly.

**Rules.** Optional. Number.

## ActualOvertimeHours {#EmployeeOvertime.ActualOvertimeHours}
<!-- alias: loc:ActualOvertimeHours, dom:EmployeeOvertime_ActualOvertimeHours -->

**What it is.** The hours the time engine measured.

**Example.** 3.5

**Why it matters.** The time engine overwrites it each time it processes the date.

**Rules.** Optional. Number.

## ApprovedOvertimeHours {#EmployeeOvertime.ApprovedOvertimeHours}
<!-- alias: loc:ApprovedOvertimeHours, dom:EmployeeOvertime_ApprovedOvertimeHours -->

**What it is.** The hours approved for payment.

**Example.** 3.5

**Why it matters.** Payroll pays these hours times the overtime rate. If left at zero, the time engine fills them from the measured hours.

**Rules.** Optional. Number.

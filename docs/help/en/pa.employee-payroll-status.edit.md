---
helpId: pa.employee-payroll-status.edit
lang: en
title: Employee Payroll Statuses — Create / Edit
entity: HCMv3.PayrollManagement.EmployeePayrollStatus
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/payroll-status/create-or-edit-employee-payroll-status-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.EmployeePayrollStatuses.Create]
---

## Overview

The employee's payroll status record. An employee can have only one.

The payroll and time engines create and update this record themselves to track how far the employee has been processed. The two options on this form are stored but not read by either engine at present.

## PersonLocked {#EmployeePayrollStatus.PersonLocked}
<!-- alias: loc:PersonLocked, dom:EmployeePayrollStatus_PersonLocked -->

**What it is.** Marks the employee as locked for payroll.

**Why it matters.** Stored only; payroll does not currently skip a locked employee.

**Rules.** Yes or no.

## PayrollCorrection {#EmployeePayrollStatus.PayrollCorrection}
<!-- alias: loc:PayrollCorrection, dom:EmployeePayrollStatus_PayrollCorrection -->

**What it is.** Marks the employee as needing a payroll correction.

**Why it matters.** Stored only; no engine reads it at present.

**Rules.** Yes or no.

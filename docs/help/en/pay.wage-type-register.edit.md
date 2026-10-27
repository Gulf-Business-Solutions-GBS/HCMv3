---
helpId: pay.wage-type-register.edit
lang: en
title: Wagetype Registers — Create / Edit
entity: HCMv3.PayrollManagement.WageTypeRegister
surfaces:
  - angular/src/app/admin/payroll-management/wage-type-registers/create-or-edit-wage-type-register-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.WageTypeRegisters.Create]
---

## Overview

A log row for a payroll period. Not read at present: payroll neither writes nor reads these rows, and they are kept for reference only.

## EmployeeId {#WageTypeRegister.EmployeeId}
<!-- alias: loc:EmployeeId, dom:WageTypeRegister_EmployeeId -->

**What it is.** The employee the row is about.

**Rules.** Optional. Chosen from the employee list.

## ForPeriod {#WageTypeRegister.ForPeriod}
<!-- alias: loc:ForPeriod, dom:WageTypeRegister_ForPeriod -->

**What it is.** The period the row is for.

**Example.** `202604`

**Rules.** Required. Whole number.

## InPeriod {#WageTypeRegister.InPeriod}
<!-- alias: loc:InPeriod, dom:WageTypeRegister_InPeriod -->

**What it is.** The period the row was recorded in.

**Example.** `202605`

**Rules.** Required. Whole number.

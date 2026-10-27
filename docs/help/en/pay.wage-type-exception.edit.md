---
helpId: pay.wage-type-exception.edit
lang: en
title: Wagetype Exceptions — Create / Edit
entity: HCMv3.PayrollManagement.WageTypeException
surfaces:
  - angular/src/app/admin/payroll-management/wage-type-exceptions/create-or-edit-wage-type-exception-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.WageTypeExceptions.Create]
---

## Overview

Pairs of wagetypes that should not both be paid. When payroll reaches the wagetype, it skips it if any of its excepted wagetypes already has an amount, number or rate on the employee's payslip.

## WageTypes {#WageTypeException.WageTypeId}
<!-- alias: loc:WageTypes, dom:WageTypeException_WageTypeId -->

**What it is.** The wagetype that is skipped.

**Example.** Transport Allowance

**Why it matters.** Payroll leaves it out for an employee whose payslip already has the excepted wagetype.

**Rules.** Required. Chosen from Wage Type master data.

## ExceptWageType {#WageTypeException.ExceptWageTypeId}
<!-- alias: loc:ExceptWageType, dom:WageTypeException_ExceptWageTypeId -->

**What it is.** The wagetype whose presence stops it.

**Example.** Company Car Benefit

**Why it matters.** It must already be on the payslip when the first wagetype is reached, so give it a lower payroll order.

**Rules.** Required. Chosen from Wage Type master data.

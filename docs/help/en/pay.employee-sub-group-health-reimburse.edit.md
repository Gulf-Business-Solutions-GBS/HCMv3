---
helpId: pay.employee-sub-group-health-reimburse.edit
lang: en
title: Employee Sub Group Health Reimburses — Create / Edit
entity: HCMv3.PayrollManagement.EmployeeSubGroupHealthReimburse
surfaces:
  - angular/src/app/admin/payroll-management/employee-sub-group-detail/health-reimburses/create-or-edit-employee-sub-group-health-reimburse-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.EmployeeSubGroupHealthReimburses.Create]
---

## Overview

The sub group's health reimbursement entitlement per person, by dependant type -- one row for the employee and one for each kind of dependant.

The employee's eligible amount adds up the employee's own row and the rows for their dependants, up to the sub group's maximum number of dependants. The yearly cap and the insurance year are set on the sub group itself.

## DependantType {#EmployeeSubGroupHealthReimburse.DependantTypeId}
<!-- alias: loc:DependantType, dom:EmployeeSubGroupHealthReimburse_DependantTypeId -->

**What it is.** Who this row is for: the employee, or a kind of dependant.

**Example.** Spouse

**Rules.** Optional. Chosen from Dependant Type master data.

## Amount {#EmployeeSubGroupHealthReimburse.Amount}
<!-- alias: loc:Amount, dom:EmployeeSubGroupHealthReimburse_Amount -->

**What it is.** The amount this person adds to the eligible amount.

**Example.** `150`

**Rules.** Optional. Number.

:::admin
A benefit exception for the employee replaces the employee's rows. A dependant's exception replaces the whole total counted so far rather than adding to it -- legacy behaviour, kept on purpose.
:::

## AgeLimit {#EmployeeSubGroupHealthReimburse.AgeLimit}
<!-- alias: loc:AgeLimit, dom:EmployeeSubGroupHealthReimburse_AgeLimit -->

**What it is.** For a dependant, the oldest age at which they count.

**Example.** `18`

**Rules.** Optional. Number.

:::admin
An age limit of 0 means no dependant qualifies -- it is not "no limit". Set the oldest age that is entitled.
:::

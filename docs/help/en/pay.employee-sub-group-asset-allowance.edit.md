---
helpId: pay.employee-sub-group-asset-allowance.edit
lang: en
title: Employee Sub Group Asset Allowances — Create / Edit
entity: HCMv3.PayrollManagement.EmployeeSubGroupAssetAllowance
surfaces:
  - angular/src/app/admin/payroll-management/employee-sub-group-detail/asset-allowances/create-or-edit-employee-sub-group-asset-allowance-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.EmployeeSubGroupAssetAllowances.Create]
---

## Overview

An allowance for company assets, paid through a wagetype.

Not read at present: payroll does not use this list, and it is kept for reference only.

## WageType {#EmployeeSubGroupAssetAllowance.WageTypeId}
<!-- alias: loc:WageType, dom:EmployeeSubGroupAssetAllowance_WageTypeId -->

**What it is.** The wagetype of the allowance.

**Example.** Car Allowance

**Why it matters.** Each wagetype can be listed once per sub group.

**Rules.** Optional. Chosen from a list.

## Amount {#EmployeeSubGroupAssetAllowance.Amount}
<!-- alias: loc:Amount, dom:EmployeeSubGroupAssetAllowance_Amount -->

**What it is.** The allowance amount.

**Example.** `100`

**Rules.** Optional. Number.

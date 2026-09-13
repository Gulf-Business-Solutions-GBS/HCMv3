---
helpId: pa.employee-wage-account.edit
lang: en
title: Employee Wage Accounts — Create / Edit
entity: HCMv3.PayrollManagement.EmployeeWageAccount
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/wage-accounts/create-or-edit-employee-wage-account-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.EmployeeWageAccounts.Create]
---

## Overview

Which ledger account one of the employee's wage types posts to, from a given date. The cost engine and the journal voucher exports read these to post the employee's pay.

Records for the same employee and wage type form a history: saving a new one closes the previous one on the day before the new start date. A wage type can be split across several accounts starting on the same date, as long as the percentages add up to no more than 100.

## WageTypeId {#EmployeeWageAccount.WageTypeId}
<!-- alias: loc:WageTypeId, dom:EmployeeWageAccount_WageTypeId -->

**What it is.** The wage type this mapping is for.

**Example.** Housing Allowance

**Rules.** Required. Chosen from Wage Type master data.

:::admin
Maintained under Payroll Management → Wage Types. Only wage types included in FI posting are posted.
:::

## CostPercentage {#EmployeeWageAccount.CostPercentage}
<!-- alias: loc:CostPercentage, dom:EmployeeWageAccount_CostPercentage -->

**What it is.** The share of this wage type's amount posted to this account.

**Example.** 100

**Why it matters.** The cost engine posts this percentage of the wage type's total. Records for the same employee, wage type and start date cannot add up to more than 100.

**Rules.** Optional. Number.

## AccountInfoId {#EmployeeWageAccount.AccountInfoId}
<!-- alias: loc:AccountInfoId, dom:EmployeeWageAccount_AccountInfoId -->

**What it is.** The ledger account the amount is posted to.

**Example.** 510100 — Salaries and Wages

**Rules.** Required. Chosen from Account Info master data.

## OffsetAccountInfoId {#EmployeeWageAccount.OffsetAccountInfoId}
<!-- alias: loc:OffsetAccountInfoId, dom:EmployeeWageAccount_OffsetAccountInfoId -->

**What it is.** The balancing account for the journal entry.

**Example.** 210500 — Salaries Payable

**Why it matters.** When set, the journal voucher posts the other side of the entry to this account.

**Rules.** Optional. Chosen from Account Info master data.

## MergeOffsetAccount {#EmployeeWageAccount.MergeOffsetAccount}
<!-- alias: loc:MergeOffsetAccount, dom:EmployeeWageAccount_MergeOffsetAccount -->

**What it is.** Whether offset lines to the same account are combined into one line in the journal voucher.

**Why it matters.** Read when the journal voucher is built.

**Rules.** Yes or no.

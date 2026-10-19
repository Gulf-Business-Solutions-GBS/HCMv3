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

Which ledger account one of the employee's wagetypes posts to, from a given date, and under which cost elements. The cost engine and the journal voucher exports read these to post the employee's pay.

Records for the same employee and wagetype form a history: saving a new one closes the previous one on the day before the new start date. A wagetype can be split across several accounts starting on the same date, as long as the percentages add up to no more than 100.

## WageTypeId {#EmployeeWageAccount.WageTypeId}
<!-- alias: loc:WageTypeId, dom:EmployeeWageAccount_WageTypeId -->

**What it is.** The wagetype this mapping is for.

**Example.** Housing Allowance

**Rules.** Required. Chosen from Wage Type master data.

:::admin
Maintained under Payroll Management → Wagetypes. Only wagetypes included in FI posting are posted.
:::

## CostPercentage {#EmployeeWageAccount.CostPercentage}
<!-- alias: loc:CostPercentage, dom:EmployeeWageAccount_CostPercentage -->

**What it is.** The share of this wagetype's amount posted to this account.

**Example.** 100

**Why it matters.** The cost engine posts this percentage of the wagetype's total. Records for the same employee, wagetype and start date cannot add up to more than 100.

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

## CostElement1Id {#EmployeeWageAccount.CostElement1Id}
<!-- alias: loc:CostElement1Id, dom:EmployeeWageAccount_costElement1Id -->

**What it is.** Cost element 1 -- one of up to seven accounting dimensions. Captioned with the name of the cost element type whose code is CostElement1, and lists only cost elements of that type; not shown at all when no such type exists.

**Why it matters.** The cost engine copies it onto the employee's cost register lines, and the journal voucher export prints its code. Offset lines merge only when all seven cost elements match.

**Rules.** Optional. Chosen from Cost Element master data.

:::admin
The slot captions come from Organization Management → Cost Element Types: give a type the code CostElement1 to CostElement7 to switch that slot on. A saved value whose cost element has since moved to another type is still shown, by its number, so opening the row does not clear it.
:::

## CostElement2Id {#EmployeeWageAccount.CostElement2Id}
<!-- alias: loc:CostElement2Id, dom:EmployeeWageAccount_costElement2Id -->

**What it is.** Cost element 2 -- one of up to seven accounting dimensions. Captioned with the name of the cost element type whose code is CostElement2, and lists only cost elements of that type; not shown at all when no such type exists.

**Why it matters.** The cost engine copies it onto the employee's cost register lines, and the journal voucher export prints its code. Offset lines merge only when all seven cost elements match.

**Rules.** Optional. Chosen from Cost Element master data.

## CostElement3Id {#EmployeeWageAccount.CostElement3Id}
<!-- alias: loc:CostElement3Id, dom:EmployeeWageAccount_costElement3Id -->

**What it is.** Cost element 3 -- one of up to seven accounting dimensions. Captioned with the name of the cost element type whose code is CostElement3, and lists only cost elements of that type; not shown at all when no such type exists.

**Why it matters.** The cost engine copies it onto the employee's cost register lines, and the journal voucher export prints its code. Offset lines merge only when all seven cost elements match.

**Rules.** Optional. Chosen from Cost Element master data.

## CostElement4Id {#EmployeeWageAccount.CostElement4Id}
<!-- alias: loc:CostElement4Id, dom:EmployeeWageAccount_costElement4Id -->

**What it is.** Cost element 4 -- one of up to seven accounting dimensions. Captioned with the name of the cost element type whose code is CostElement4, and lists only cost elements of that type; not shown at all when no such type exists.

**Why it matters.** The cost engine copies it onto the employee's cost register lines, and the journal voucher export prints its code. Offset lines merge only when all seven cost elements match.

**Rules.** Optional. Chosen from Cost Element master data.

## CostElement5Id {#EmployeeWageAccount.CostElement5Id}
<!-- alias: loc:CostElement5Id, dom:EmployeeWageAccount_costElement5Id -->

**What it is.** Cost element 5 -- one of up to seven accounting dimensions. Captioned with the name of the cost element type whose code is CostElement5, and lists only cost elements of that type; not shown at all when no such type exists.

**Why it matters.** The cost engine copies it onto the employee's cost register lines, and the journal voucher export prints its code. Offset lines merge only when all seven cost elements match.

**Rules.** Optional. Chosen from Cost Element master data.

## CostElement6Id {#EmployeeWageAccount.CostElement6Id}
<!-- alias: loc:CostElement6Id, dom:EmployeeWageAccount_costElement6Id -->

**What it is.** Cost element 6 -- one of up to seven accounting dimensions. Captioned with the name of the cost element type whose code is CostElement6, and lists only cost elements of that type; not shown at all when no such type exists.

**Why it matters.** The cost engine copies it onto the employee's cost register lines, and the journal voucher export prints its code. Offset lines merge only when all seven cost elements match.

**Rules.** Optional. Chosen from Cost Element master data.

## CostElement7Id {#EmployeeWageAccount.CostElement7Id}
<!-- alias: loc:CostElement7Id, dom:EmployeeWageAccount_costElement7Id -->

**What it is.** Cost element 7 -- one of up to seven accounting dimensions. Captioned with the name of the cost element type whose code is CostElement7, and lists only cost elements of that type; not shown at all when no such type exists.

**Why it matters.** The cost engine copies it onto the employee's cost register lines, and the journal voucher export prints its code. Offset lines merge only when all seven cost elements match.

**Rules.** Optional. Chosen from Cost Element master data.

---
helpId: pay.wage-account.edit
lang: en
title: Wage Accounts — Create / Edit
entity: HCMv3.PayrollManagement.WageAccount
surfaces:
  - angular/src/app/admin/payroll-management/wage-accounts/create-or-edit-wage-account-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.WageAccounts.Create]
---

## Overview

A company-wide mapping of a wagetype to a ledger account, from a given date, with its offset account and cost elements.

Not read at present: the cost engine and the journal voucher take their accounts from each employee's own **Wage Accounts** (on the employee record), and nothing reads this list. A wagetype with no employee wage account for the period stops costing for that employee ("WageType : '…' not configured for Employee"), whatever is set here.

## WageTypeId {#WageAccount.WageTypeId}
<!-- alias: loc:WageTypeId, dom:WageAccount_WageTypeId -->

**What it is.** The wagetype this mapping is for.

**Example.** Housing Allowance

**Rules.** Required. Chosen from Wagetype master data.

## AccountInfoId {#WageAccount.AccountInfoId}
<!-- alias: loc:AccountInfoId, dom:WageAccount_AccountInfoId -->

**What it is.** The ledger account the wagetype would post to.

**Example.** 510100 -- Salaries and Wages

**Rules.** Required. Chosen from Account Info master data.

## OffsetAccountInfoId {#WageAccount.OffsetAccountInfoId}
<!-- alias: loc:OffsetAccountInfoId, dom:WageAccount_OffsetAccountInfoId -->

**What it is.** The balancing account for the entry.

**Example.** 210500 -- Salaries Payable

**Rules.** Optional. Chosen from Account Info master data.

## CostPercentage {#WageAccount.CostPercentage}
<!-- alias: loc:CostPercentage, dom:WageAccount_CostPercentage -->

**What it is.** The share of the wagetype's amount this mapping would carry.

**Example.** `100`

**Rules.** Optional. Number.

## CostElement1Id {#WageAccount.CostElement1Id}
<!-- alias: loc:CostElement1Id, dom:WageAccount_costElement1Id -->

**What it is.** Cost element 1 -- one of up to seven accounting dimensions.

**Rules.** Optional. Chosen from Cost Element master data.

## CostElement2Id {#WageAccount.CostElement2Id}
<!-- alias: loc:CostElement2Id, dom:WageAccount_costElement2Id -->

**What it is.** Cost element 2 -- one of up to seven accounting dimensions.

**Rules.** Optional. Chosen from Cost Element master data.

## CostElement3Id {#WageAccount.CostElement3Id}
<!-- alias: loc:CostElement3Id, dom:WageAccount_costElement3Id -->

**What it is.** Cost element 3 -- one of up to seven accounting dimensions.

**Rules.** Optional. Chosen from Cost Element master data.

## CostElement4Id {#WageAccount.CostElement4Id}
<!-- alias: loc:CostElement4Id, dom:WageAccount_costElement4Id -->

**What it is.** Cost element 4 -- one of up to seven accounting dimensions.

**Rules.** Optional. Chosen from Cost Element master data.

## CostElement5Id {#WageAccount.CostElement5Id}
<!-- alias: loc:CostElement5Id, dom:WageAccount_costElement5Id -->

**What it is.** Cost element 5 -- one of up to seven accounting dimensions.

**Rules.** Optional. Chosen from Cost Element master data.

## CostElement6Id {#WageAccount.CostElement6Id}
<!-- alias: loc:CostElement6Id, dom:WageAccount_costElement6Id -->

**What it is.** Cost element 6 -- one of up to seven accounting dimensions.

**Rules.** Optional. Chosen from Cost Element master data.

## CostElement7Id {#WageAccount.CostElement7Id}
<!-- alias: loc:CostElement7Id, dom:WageAccount_costElement7Id -->

**What it is.** Cost element 7 -- one of up to seven accounting dimensions.

**Rules.** Optional. Chosen from Cost Element master data.

---
helpId: pay.cost-register-detail.edit
lang: en
title: Cost Register Details — Create / Edit
entity: HCMv3.PayrollManagement.CostRegisterDetail
surfaces:
  - angular/src/app/admin/payroll-management/cost-register-details/create-or-edit-cost-register-detail-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.CostRegisterDetails.Create]
---

## Overview

One cost line: part of a wagetype's amount posted to a ledger account under a set of cost elements. A wagetype split across several wage accounts gives several lines, each carrying its cost percentage of the amount.

## CostRegisterId {#CostRegisterDetail.CostRegisterId}
<!-- alias: loc:CostRegisterId, dom:CostRegisterDetail_CostRegisterId -->

**What it is.** The cost register this line belongs to.

**Rules.** Required. Chosen from Cost Register master data.

## WageTypeId {#CostRegisterDetail.WageTypeId}
<!-- alias: loc:WageTypeId, dom:CostRegisterDetail_WageTypeId -->

**What it is.** The wagetype costed. An arrear is costed under its source wagetype.

**Example.** Basic Salary

**Rules.** Required. Chosen from Wage Type master data.

## AccountInfoId {#CostRegisterDetail.AccountInfoId}
<!-- alias: loc:AccountInfoId, dom:CostRegisterDetail_AccountInfoId -->

**What it is.** The ledger account the amount posts to.

**Example.** 510100 -- Salaries and Wages

**Rules.** Required. Chosen from Account Info master data.

## Amount {#CostRegisterDetail.Amount}
<!-- alias: loc:Amount, dom:CostRegisterDetail_Amount -->

**What it is.** The amount posted.

**Example.** `800.000`

**Rules.** Required. Number.

## CurrencyId {#CostRegisterDetail.CurrencyId}
<!-- alias: loc:CurrencyId, dom:CostRegisterDetail_CurrencyId -->

**What it is.** The currency of the employee's basic pay.

**Example.** Kuwaiti Dinar

**Rules.** Optional. Chosen from Currency master data.

## CostElement1Id {#CostRegisterDetail.CostElement1Id}
<!-- alias: loc:CostElement1Id, dom:CostRegisterDetail_costElement1Id -->

**What it is.** Cost element 1 -- one of up to seven accounting dimensions, copied from the employee's wage account.

**Rules.** Optional. Chosen from Cost Element master data.

## CostElement2Id {#CostRegisterDetail.CostElement2Id}
<!-- alias: loc:CostElement2Id, dom:CostRegisterDetail_costElement2Id -->

**What it is.** Cost element 2 -- one of up to seven accounting dimensions, copied from the employee's wage account.

**Rules.** Optional. Chosen from Cost Element master data.

## CostElement3Id {#CostRegisterDetail.CostElement3Id}
<!-- alias: loc:CostElement3Id, dom:CostRegisterDetail_costElement3Id -->

**What it is.** Cost element 3 -- one of up to seven accounting dimensions, copied from the employee's wage account.

**Rules.** Optional. Chosen from Cost Element master data.

## CostElement4Id {#CostRegisterDetail.CostElement4Id}
<!-- alias: loc:CostElement4Id, dom:CostRegisterDetail_costElement4Id -->

**What it is.** Cost element 4 -- one of up to seven accounting dimensions, copied from the employee's wage account.

**Rules.** Optional. Chosen from Cost Element master data.

## CostElement5Id {#CostRegisterDetail.CostElement5Id}
<!-- alias: loc:CostElement5Id, dom:CostRegisterDetail_costElement5Id -->

**What it is.** Cost element 5 -- one of up to seven accounting dimensions, copied from the employee's wage account.

**Rules.** Optional. Chosen from Cost Element master data.

## CostElement6Id {#CostRegisterDetail.CostElement6Id}
<!-- alias: loc:CostElement6Id, dom:CostRegisterDetail_costElement6Id -->

**What it is.** Cost element 6 -- one of up to seven accounting dimensions, copied from the employee's wage account.

**Rules.** Optional. Chosen from Cost Element master data.

## CostElement7Id {#CostRegisterDetail.CostElement7Id}
<!-- alias: loc:CostElement7Id, dom:CostRegisterDetail_costElement7Id -->

**What it is.** Cost element 7 -- one of up to seven accounting dimensions, copied from the employee's wage account.

**Rules.** Optional. Chosen from Cost Element master data.

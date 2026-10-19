---
helpId: pay.wage-acc-group-detail.edit
lang: en
title: Wage Account Group Details — Create / Edit
entity: HCMv3.PayrollManagement.WageAccGroupDetail
surfaces:
  - angular/src/app/admin/payroll-management/wage-acc-group-details/create-or-edit-wage-acc-group-detail-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.WageAccGroupDetails.Create]
---

## Overview

One line of a wage account group: which ledger account a wagetype posts to, its offset account and cost elements.

Not read at present: the cost engine and the journal voucher take their accounts from each employee's own wage accounts, and nothing reads these lines. They are kept for reference and for future use.

## WageAccGroupId {#WageAccGroupDetail.WageAccGroupId}
<!-- alias: loc:WageAccGroupId, dom:WageAccGroupDetail_WageAccGroupId -->

**What it is.** The wage account group this line belongs to.

**Example.** Head Office Accounts

**Rules.** Required. Chosen from Wage Acc Group master data.

## WageTypeId {#WageAccGroupDetail.WageTypeId}
<!-- alias: loc:WageTypeId, dom:WageAccGroupDetail_WageTypeId -->

**What it is.** The wagetype this line maps.

**Example.** Basic Salary

**Rules.** Required. Chosen from Wage Type master data.

## AccountInfoId {#WageAccGroupDetail.AccountInfoId}
<!-- alias: loc:AccountInfoId, dom:WageAccGroupDetail_AccountInfoId -->

**What it is.** The ledger account the wagetype would post to.

**Example.** 510100 -- Salaries and Wages

**Rules.** Required. Chosen from Account Info master data.

## OffsetAccountInfoType {#WageAccGroupDetail.OffsetAccountInfoType}
<!-- alias: loc:OffsetAccountInfoType, dom:WageAccGroupDetail_OffsetAccountInfoType -->

**What it is.** Where the balancing account comes from: a fixed account chosen below, or one worked out from the employee code.

**Example.** Fixed

**Rules.** Required. One of: Fixed, Employee Code.

## OffsetAccountInfoId {#WageAccGroupDetail.OffsetAccountInfoId}
<!-- alias: loc:OffsetAccountInfoId, dom:WageAccGroupDetail_OffsetAccountInfoId -->

**What it is.** The balancing account, when the offset type is Fixed.

**Example.** 210500 -- Salaries Payable

**Rules.** Optional. Chosen from Account Info master data.

## CostPercentage {#WageAccGroupDetail.CostPercentage}
<!-- alias: loc:CostPercentage, dom:WageAccGroupDetail_CostPercentage -->

**What it is.** The share of the wagetype's amount this line would carry.

**Example.** `100`

**Rules.** Optional. Number.

## MergeOffsetAccount {#WageAccGroupDetail.MergeOffsetAccount}
<!-- alias: loc:MergeOffsetAccount, dom:WageAccGroupDetail_MergeOffsetAccount -->

**What it is.** Whether offset lines for this mapping would be merged into one.

**Rules.** Yes or no.

## CostElement1Id {#WageAccGroupDetail.CostElement1Id}
<!-- alias: loc:CostElement1Id, dom:WageAccGroupDetail_costElement1Id -->

**What it is.** Cost element 1 -- one of up to seven accounting dimensions. Captioned with the name of the cost element type whose code is CostElement1, and lists only cost elements of that type; not shown at all when no such type exists.

**Rules.** Optional. Chosen from Cost Element master data.

:::admin
The slot captions come from Organization Management → Cost Element Types: give a type the code CostElement1 to CostElement7 to switch that slot on. A saved value whose cost element has since moved to another type is still shown, by its number, so opening the row does not clear it.
:::

## CostElement2Id {#WageAccGroupDetail.CostElement2Id}
<!-- alias: loc:CostElement2Id, dom:WageAccGroupDetail_costElement2Id -->

**What it is.** Cost element 2 -- one of up to seven accounting dimensions. Captioned with the name of the cost element type whose code is CostElement2, and lists only cost elements of that type; not shown at all when no such type exists.

**Rules.** Optional. Chosen from Cost Element master data.

## CostElement3Id {#WageAccGroupDetail.CostElement3Id}
<!-- alias: loc:CostElement3Id, dom:WageAccGroupDetail_costElement3Id -->

**What it is.** Cost element 3 -- one of up to seven accounting dimensions. Captioned with the name of the cost element type whose code is CostElement3, and lists only cost elements of that type; not shown at all when no such type exists.

**Rules.** Optional. Chosen from Cost Element master data.

## CostElement4Id {#WageAccGroupDetail.CostElement4Id}
<!-- alias: loc:CostElement4Id, dom:WageAccGroupDetail_costElement4Id -->

**What it is.** Cost element 4 -- one of up to seven accounting dimensions. Captioned with the name of the cost element type whose code is CostElement4, and lists only cost elements of that type; not shown at all when no such type exists.

**Rules.** Optional. Chosen from Cost Element master data.

## CostElement5Id {#WageAccGroupDetail.CostElement5Id}
<!-- alias: loc:CostElement5Id, dom:WageAccGroupDetail_costElement5Id -->

**What it is.** Cost element 5 -- one of up to seven accounting dimensions. Captioned with the name of the cost element type whose code is CostElement5, and lists only cost elements of that type; not shown at all when no such type exists.

**Rules.** Optional. Chosen from Cost Element master data.

## CostElement6Id {#WageAccGroupDetail.CostElement6Id}
<!-- alias: loc:CostElement6Id, dom:WageAccGroupDetail_costElement6Id -->

**What it is.** Cost element 6 -- one of up to seven accounting dimensions. Captioned with the name of the cost element type whose code is CostElement6, and lists only cost elements of that type; not shown at all when no such type exists.

**Rules.** Optional. Chosen from Cost Element master data.

## CostElement7Id {#WageAccGroupDetail.CostElement7Id}
<!-- alias: loc:CostElement7Id, dom:WageAccGroupDetail_costElement7Id -->

**What it is.** Cost element 7 -- one of up to seven accounting dimensions. Captioned with the name of the cost element type whose code is CostElement7, and lists only cost elements of that type; not shown at all when no such type exists.

**Rules.** Optional. Chosen from Cost Element master data.

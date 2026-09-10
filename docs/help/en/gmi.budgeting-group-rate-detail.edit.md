---
helpId: gmi.budgeting-group-rate-detail.edit
lang: en
title: Budgeting Group Rate Details — Create / Edit
entity: HCMv3.GeneralMasterInfo.BudgetingGroupRateDetail
surfaces:
  - angular/src/app/admin/general-master-info/budgeting-group-rate-details/create-or-edit-budgeting-group-rate-detail-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.BudgetingGroupRateDetails.Create]
---

## Overview

Bonus rates for a budgeting group over a period of time. They are recorded for reference only: no calculation reads these rates at present.

## BudgetingGroup {#BudgetingGroupRateDetail.BudgetingGroupId}
<!-- alias: loc:BudgetingGroup, dom:BudgetingGroupRateDetail_BudgetingGroupId -->

**What it is.** The budgeting group this rate belongs to.

**Rules.** Optional. Chosen from Budgeting Group master data.

## StartDate {#BudgetingGroupRateDetail.StartDate}
<!-- alias: loc:StartDate, dom:BudgetingGroupRateDetail_StartDate -->

**What it is.** The first date the rate applies to.

**Rules.** Optional. Date value.

## EndDate {#BudgetingGroupRateDetail.EndDate}
<!-- alias: loc:EndDate, dom:BudgetingGroupRateDetail_EndDate -->

**What it is.** The last date the rate applies to.

**Rules.** Optional. Date value.

## BonusRate {#BudgetingGroupRateDetail.BonusRate}
<!-- alias: loc:BonusRate, dom:BudgetingGroupRateDetail_BonusRate -->

**What it is.** The bonus rate for the group during the period.

**Rules.** Required. Number.

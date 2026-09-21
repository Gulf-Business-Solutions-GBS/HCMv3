---
helpId: trv.travel-detail-cost.edit
lang: en
title: Travel Detail Costs — Create / Edit
entity: HCMv3.TravelManagement.TravelDetailCost
surfaces:
  - angular/src/app/admin/travel-management/travel-detail-costs/create-or-edit-travel-detail-cost-modal.component.html
audience: [admin]
permissions: [Pages.TravelManagement.TravelDetailCosts.Create]
---

## Overview

One cost of a trip leg, such as the air fare or hotel. The trip's costs are added up for the approver and for the employee's self-service view.

## TravelDetail {#TravelDetailCost.TravelDetailId}
<!-- alias: loc:TravelDetail, dom:TravelDetailCost_TravelDetailId -->

**What it is.** The leg.

**Rules.** Required. Chosen from Travel Detail master data.

## ExpenseType {#TravelDetailCost.ExpenseTypeId}
<!-- alias: loc:ExpenseType, dom:TravelDetailCost_ExpenseTypeId -->

**What it is.** The kind of cost.

**Example.** Air fare

**Rules.** Required. Chosen from Expense Type master data.

## Amount {#TravelDetailCost.Amount}
<!-- alias: loc:Amount, dom:TravelDetailCost_Amount -->

**What it is.** The amount.

**Example.** 180.000

**Rules.** Required. Number.

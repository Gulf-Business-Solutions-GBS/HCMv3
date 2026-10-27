---
helpId: org.cost-center.edit
lang: en
title: Cost Centers — Create / Edit
entity: HCMv3.OrganizationManagement.CostCenter
surfaces:
  - angular/src/app/admin/organization-management/cost-centers/create-or-edit-cost-center-modal.component.html
audience: [admin]
permissions: [Pages.OrganizationManagement.CostCenters.Create]
---

## Overview

A cost centre that salary costs are booked to. Employees are placed in one on their organisation assignment. A workflow step can limit its approvers to the requester's cost centre, and some customer-specific payroll journal reports group costs by it.

## CostCenterGroup {#CostCenter.CostCenterGroupId}
<!-- alias: loc:CostCenterGroup, dom:CostCenter_CostCenterGroupId -->

**What it is.** A group the cost centre belongs to.

**Why it matters.** Not read at present.

**Rules.** Optional. Chosen from Cost Center Group master data.

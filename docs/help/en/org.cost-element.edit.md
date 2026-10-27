---
helpId: org.cost-element.edit
lang: en
title: Cost Elements — Create / Edit
entity: HCMv3.OrganizationManagement.CostElement
surfaces:
  - angular/src/app/admin/organization-management/cost-elements/create-or-edit-cost-element-modal.component.html
audience: [admin]
permissions: [Pages.OrganizationManagement.CostElements.Create]
---

## Overview

An extra accounting dimension, such as a department code or activity, set on wage accounts and employee account assignments. Its code is written onto payroll journal lines alongside the account. The type cannot be blank.

## CostElementType {#CostElement.CostElementTypeId}
<!-- alias: loc:CostElementType, dom:CostElement_CostElementTypeId -->

**What it is.** The kind of dimension.

**Example.** Department

**Rules.** Optional. Chosen from Cost Element Type master data.

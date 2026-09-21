---
helpId: pay.org-unit-wage-acc-group.edit
lang: en
title: Organization Unit Wage Account Groups — Create / Edit
entity: HCMv3.PayrollManagement.OrgUnitWageAccGroup
surfaces:
  - angular/src/app/admin/payroll-management/org-unit-wage-acc-groups/create-or-edit-org-unit-wage-acc-group-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.OrgUnitWageAccGroups.Create]
---

## Overview

Which wage account group an organisation unit uses, from a given date.

Not read at present: the cost engine and the journal voucher take their accounts from each employee's own wage accounts, and nothing reads this assignment. It is kept for reference and for future use.

## OrganizationUnitId {#OrgUnitWageAccGroup.OrganizationUnitId}
<!-- alias: loc:OrganizationUnitId, dom:OrgUnitWageAccGroup_OrganizationUnitId -->

**What it is.** The organisation unit.

**Example.** Finance Department

**Rules.** Required. Chosen from Organization Unit master data.

## WageAccGroupId {#OrgUnitWageAccGroup.WageAccGroupId}
<!-- alias: loc:WageAccGroupId, dom:OrgUnitWageAccGroup_WageAccGroupId -->

**What it is.** The wage account group the unit uses from the start date.

**Example.** Head Office Accounts

**Rules.** Required. Chosen from Wage Acc Group master data.

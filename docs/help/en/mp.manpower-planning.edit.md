---
helpId: mp.manpower-planning.edit
lang: en
title: Manpower Plannings — Create / Edit
entity: HCMv3.ManpowerPlanningManagement.ManpowerPlanning
surfaces:
  - angular/src/app/admin/manpower-planning-management/manpower-plannings/create-or-edit-manpower-planning-modal.component.html
audience: [admin]
permissions: [Pages.ManpowerPlanningManagement.ManpowerPlannings.Create]
---

## Overview

The headcount a department is asking for over a planning period: one line per job title with a monthly budget. A department can have one plan per period. The plan goes to approval, which needs a period and at least one line; the approver sees the department, period and amount. When it is approved, a vacant, budgeted position is created for every line that does not already have one.

## Employee {#ManpowerPlanning.EmployeeId}
<!-- alias: loc:Employee, dom:ManpowerPlanning_EmployeeId -->

**What it is.** The employee raising the plan.

**Rules.** Required. Chosen from the employee list.

## OrganizationUnit {#ManpowerPlanning.OrganizationUnitId}
<!-- alias: loc:OrganizationUnit, dom:ManpowerPlanning_OrganizationUnitId -->

**What it is.** The department the plan is for.

**Example.** Finance

**Rules.** Required. Chosen from Organization unit master data.

## Period {#ManpowerPlanning.PeriodId}
<!-- alias: loc:Period, dom:ManpowerPlanning_PeriodId -->

**What it is.** The planning period.

**Rules.** Required. Chosen from Manpower Planning Period master data.

## RequestComments {#ManpowerPlanning.RequestComments}
<!-- alias: loc:RequestComments, dom:ManpowerPlanning_RequestComments -->

**What it is.** Notes for the approver.

**Rules.** Optional. Maximum 1000 characters.

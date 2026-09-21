---
helpId: mp.position.edit
lang: en
title: Positions — Create / Edit
entity: HCMv3.ManpowerPlanningManagement.Position
surfaces:
  - angular/src/app/admin/manpower-planning-management/positions/create-or-edit-position-modal.component.html
audience: [admin]
permissions: [Pages.ManpowerPlanningManagement.Positions.Create]
---

## Overview

A position in the establishment: one seat, or several, for a job title. Positions are created by hand or from an approved manpower plan, and the Position Chart draws them from each position's Reports to position. Leave the code blank to have the next number given. A position cannot report to itself.

## JobTitle {#Position.JobTitleId}
<!-- alias: loc:JobTitle, dom:Position_JobTitleId -->

**What it is.** The job title of the position.

**Example.** Accountant

**Rules.** Optional. Chosen from Job Title master data.

## OrganizationUnit {#Position.OrganizationUnitId}
<!-- alias: loc:OrganizationUnit, dom:Position_OrganizationUnitId -->

**What it is.** The department the position sits in.

**Rules.** Optional. Chosen from Organization unit master data.

## ParentPosition {#Position.ParentPositionId}
<!-- alias: loc:ParentPosition, dom:Position_ParentPositionId -->

**What it is.** The position this one reports to.

**Example.** Finance Manager

**Why it matters.** Builds the Position Chart.

**Rules.** Optional. Chosen from Position master data.

## CostCenter {#Position.CostCenterId}
<!-- alias: loc:CostCenter, dom:Position_CostCenterId -->

**What it is.** The cost centre of the position.

**Why it matters.** Not read at present.

**Rules.** Optional. Chosen from Cost Center master data.

## EmployeeGroup {#Position.EmployeeGroupId}
<!-- alias: loc:EmployeeGroup, dom:Position_EmployeeGroupId -->

**What it is.** The employee group of the position.

**Why it matters.** Not read at present.

**Rules.** Optional. Chosen from a list.

## EmployeeSubGroup {#Position.EmployeeSubGroupId}
<!-- alias: loc:EmployeeSubGroup, dom:Position_EmployeeSubGroupId -->

**What it is.** The employee sub group of the position.

**Why it matters.** Not read at present.

**Rules.** Optional. Chosen from a list.

## PayrollAreaId {#Position.PayrollAreaId}
<!-- alias: loc:PayrollAreaId, dom:Position_PayrollAreaId -->

**What it is.** The payroll area of the position.

**Why it matters.** Not read at present.

**Rules.** Optional. Chosen from Payroll Area master data.

## WorkLocationId {#Position.WorkLocationId}
<!-- alias: loc:WorkLocationId, dom:Position_WorkLocationId -->

**What it is.** The work location of the position.

**Why it matters.** Not read at present.

**Rules.** Optional. Chosen from Work Location master data.

## FTELimit {#Position.FTELimit}
<!-- alias: loc:FTELimit, dom:Position_FTELimit -->

**What it is.** How many people can hold the position at the same time.

**Example.** 1

**Why it matters.** Must be above 0. Adding a holder whose dates would take the position past this number is refused.

**Rules.** Required. Whole number.

## MinSalary {#Position.MinSalary}
<!-- alias: loc:MinSalary, dom:Position_MinSalary -->

**What it is.** The lowest salary for the position.

**Example.** 700.000

**Why it matters.** Manpower plan lines for this position must stay within the minimum and maximum.

**Rules.** Required. Number.

## MidSalary {#Position.MidSalary}
<!-- alias: loc:MidSalary, dom:Position_MidSalary -->

**What it is.** The midpoint salary for the position.

**Example.** 800.000

**Rules.** Required. Number.

## MaxSalary {#Position.MaxSalary}
<!-- alias: loc:MaxSalary, dom:Position_MaxSalary -->

**What it is.** The highest salary for the position.

**Example.** 1,000.000

**Why it matters.** See Min Salary.

**Rules.** Required. Number.

## Vacant {#Position.Vacant}
<!-- alias: loc:Vacant, dom:Position_Vacant -->

**What it is.** Whether nobody holds the position today.

**Why it matters.** Set by the system whenever a holder is added, changed or removed.

**Rules.** Yes or no.

## Active {#Position.Active}
<!-- alias: loc:Active, dom:Position_Active -->

**What it is.** Whether the position is in use.

**Why it matters.** Not read at present.

**Rules.** Yes or no.

## IsBudgeted {#Position.IsBudgeted}
<!-- alias: loc:IsBudgeted, dom:Position_IsBudgeted -->

**What it is.** Whether the position is in the budget.

**Why it matters.** Not read at present.

**Rules.** Yes or no.

---
helpId: pa.employee-org-assignment.edit
lang: en
title: Employee Org Assignments — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeOrgAssignment
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/org-assignments/create-or-edit-employee-org-assignment-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeOrgAssignments.Create]
---

## Overview

Where the employee sits in the organisation from a given date: their department, job title, cost centre and work location.

Assignments form a history. Saving one closes the employee's previous assignment on the day before the new start date, so there is always exactly one assignment in force. Leaving a field blank on a new assignment copies it from the previous one.

Approval routing, the time engine, reports and business rules all read the assignment in force on the date they need.

## JobTitle {#EmployeeOrgAssignment.JobTitleId}
<!-- alias: loc:JobTitle, dom:EmployeeOrgAssignment_JobTitleId -->

**What it is.** The employee's job title from this date.

**Example.** Senior Accountant

**Why it matters.** Shown on reports and available to business rules. Left blank on a new assignment, it is copied from the previous one.

**Rules.** Optional. Chosen from Job Title master data.

:::admin
Maintained under Organization Management → Job Titles.
:::

## CostCenter {#EmployeeOrgAssignment.CostCenterId}
<!-- alias: loc:CostCenter, dom:EmployeeOrgAssignment_CostCenterId -->

**What it is.** The cost centre the employee is charged to from this date.

**Example.** CC-100 Finance

**Why it matters.** Approval steps assigned to a cost-centre role go to the approver for this cost centre. Left blank on a new assignment, it is copied from the previous one.

**Rules.** Optional. Chosen from Cost Center master data.

## WorkLocation {#EmployeeOrgAssignment.WorkLocationId}
<!-- alias: loc:WorkLocation, dom:EmployeeOrgAssignment_WorkLocationId -->

**What it is.** The employee's normal place of work from this date.

**Example.** Head Office

**Why it matters.** Approval steps assigned to a branch role go to the approver for this work location. Left blank on a new assignment, it is copied from the previous one.

**Rules.** Optional. Chosen from Work Location master data.

## PostingGroup {#EmployeeOrgAssignment.PostingGroupId}
<!-- alias: loc:PostingGroup, dom:EmployeeOrgAssignment_PostingGroupId -->

**What it is.** The posting group recorded for the employee from this date.

**Why it matters.** Kept for reference; no calculation or posting reads it from the assignment at present.

**Rules.** Optional. Chosen from Posting Group master data.

## BudgetingGroup {#EmployeeOrgAssignment.BudgetingGroupId}
<!-- alias: loc:BudgetingGroup, dom:EmployeeOrgAssignment_BudgetingGroupId -->

**What it is.** The budgeting group recorded for the employee from this date.

**Why it matters.** Kept for reference; no calculation reads it from the assignment at present.

**Rules.** Optional. Chosen from Budgeting Group master data.

## OrganizationUnit {#EmployeeOrgAssignment.OrganizationUnitId}
<!-- alias: loc:OrganizationUnit, dom:EmployeeOrgAssignment_OrganizationUnitId -->

**What it is.** The department or unit the employee belongs to from this date, chosen from the organisation tree.

**Example.** Finance › Accounts Payable

**Why it matters.** Decides the employee's place in the organisation structure, and approval steps assigned to a department role go to that department's approver. Payroll amounts for the employee are rounded to the decimal places of the currency on this unit's payroll area.

**Rules.** Optional. Chosen from Organization Unit master data.

:::admin
This form does not set the employee group, employee sub group, payroll area or workflow group, and does not copy them from the previous assignment. A new assignment saved here therefore has none of them. The time engine looks for the employee sub group on the assignments covering the period it runs, and approval routing reads the workflow group on today's assignment; for dates covered only by an assignment saved here, neither finds one.
:::

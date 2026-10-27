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

## EmployeeGroup {#EmployeeOrgAssignment.EmployeeGroupId}
<!-- alias: loc:EmployeeGroup, dom:EmployeeOrgAssignment_EmployeeGroupId -->

**What it is.** The employee's salary profile for the dates of this assignment.

**Example.** Staff

**Why it matters.** Indemnity and end-of-service rules that depend on the salary profile are taken from it. Left blank, the value on the previous assignment is kept. If only a grade is chosen, the salary profile is filled in from it.

**Rules.** Optional. Chosen from a list.

## EmployeeSubGroup {#EmployeeOrgAssignment.EmployeeSubGroupId}
<!-- alias: loc:EmployeeSubGroup, dom:EmployeeOrgAssignment_EmployeeSubGroupId -->

**What it is.** The employee's grade for the dates of this assignment.

**Example.** Grade 5

**Why it matters.** Most entitlements hang from the grade: leave quotas, air tickets, indemnity, fund deductions, service bonus and health reimbursement. The time engine and payroll read the grade on the assignment covering each date. The grade must belong to the chosen salary profile; left blank, the previous assignment's grade is kept.

**Rules.** Optional. Chosen from a list.

## PayrollArea {#EmployeeOrgAssignment.PayrollAreaId}
<!-- alias: loc:PayrollArea, dom:EmployeeOrgAssignment_PayrollAreaId -->

**What it is.** The payroll area the employee is paid in for the dates of this assignment.

**Example.** Kuwait Monthly

**Why it matters.** Decides which payroll run picks the employee up, and which payroll calendar and lock apply. Left blank, the previous assignment's area is kept, or failing that the area set for the organisation unit.

**Rules.** Optional. Chosen from Payroll Area master data.

## WorkflowGroup {#EmployeeOrgAssignment.WorkflowGroupId}
<!-- alias: loc:WorkflowGroup, dom:EmployeeOrgAssignment_WorkflowGroupId -->

**What it is.** The workflow group whose approval chains the employee's requests follow.

**Example.** Head Office

**Why it matters.** When the employee submits a request, the chain is chosen from the workflow group on today's assignment. Left blank, the workflow group marked as default is used.

**Rules.** Optional. Chosen from Workflow Group master data.

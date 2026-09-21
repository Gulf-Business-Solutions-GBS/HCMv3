---
helpId: prj.employee-project.edit
lang: en
title: Employee Projects — Create / Edit
entity: HCMv3.ProjectManagement.EmployeeProject
surfaces:
  - angular/src/app/admin/project-management/employee-projects/create-or-edit-employee-project-modal.component.html
audience: [admin]
permissions: [Pages.ProjectManagement.EmployeeProjects.Create]
---

## Overview

Which projects an employee works on over a period, and how their pay is split between them. The start date must be the first day of a month and the end date the last day of a month, an employee's periods cannot overlap, and at least one project must be added.

## Employee {#EmployeeProject.EmployeeId}
<!-- alias: loc:Employee, dom:EmployeeProject_EmployeeId -->

**What it is.** The employee.

**Rules.** Required. Chosen from the employee list.

## CostingMethod {#EmployeeProject.CostingMethod}
<!-- alias: loc:CostingMethod, dom:EmployeeProject_CostingMethod -->

**What it is.** How pay is split between the projects.

**Example.** Percentage

**Why it matters.** Percentage: each pay line is split by the percentages given on the projects. Actual Days: each line is split by the working days on each project's date range, taken from the time register, while leave is split equally. Actual Hours produces no project cost at all, as in the old system.

**Rules.** Required. One of: Actual Hours, Actual Days, Percentage.

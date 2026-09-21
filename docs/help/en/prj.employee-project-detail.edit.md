---
helpId: prj.employee-project-detail.edit
lang: en
title: Employee Project Details — Create / Edit
entity: HCMv3.ProjectManagement.EmployeeProjectDetail
surfaces:
  - angular/src/app/admin/project-management/employee-project-details/create-or-edit-employee-project-detail-modal.component.html
audience: [admin]
permissions: [Pages.ProjectManagement.EmployeeProjectDetails.Create]
---

## Overview

One project within an employee's project assignment. Its dates must fall within the assignment's dates.

## EmployeeProject {#EmployeeProjectDetail.EmployeeProjectId}
<!-- alias: loc:EmployeeProject, dom:EmployeeProjectDetail_EmployeeProjectId -->

**What it is.** The employee's project assignment.

**Rules.** Required. Chosen from a list.

## Project {#EmployeeProjectDetail.ProjectId}
<!-- alias: loc:Project, dom:EmployeeProjectDetail_ProjectId -->

**What it is.** The project.

**Rules.** Required. Chosen from Project master data.

## Percentage {#EmployeeProjectDetail.Percentage}
<!-- alias: loc:Percentage, dom:EmployeeProjectDetail_Percentage -->

**What it is.** The share of pay charged to this project.

**Example.** 60

**Why it matters.** Used with the Percentage method: must be above 0 and up to 100, and the projects together cannot exceed 100. Each costed pay line's number and amount are multiplied by it.

**Rules.** Required. Number.

---
helpId: prj.project-wage-type.edit
lang: en
title: Project Wagetypes — Create / Edit
entity: HCMv3.ProjectManagement.ProjectWageType
surfaces:
  - angular/src/app/admin/project-management/project-wage-types/create-or-edit-project-wage-type-modal.component.html
audience: [admin]
permissions: [Pages.ProjectManagement.ProjectWageTypes.Create]
---

## Overview

A wagetype costed to a project. Only pay lines whose wagetype is listed here are split onto the project; everything else in the salary register is left out. A wagetype can be listed once per project.

## Project {#ProjectWageType.ProjectId}
<!-- alias: loc:Project, dom:ProjectWageType_ProjectId -->

**What it is.** The project.

**Rules.** Required. Chosen from Project master data.

## WageType {#ProjectWageType.WageTypeId}
<!-- alias: loc:WageType, dom:ProjectWageType_WageTypeId -->

**What it is.** The wagetype.

**Example.** Basic Salary

**Rules.** Required. Chosen from Wage Type master data.

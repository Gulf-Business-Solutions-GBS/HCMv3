---
helpId: perf.paemployee.edit
lang: en
title: PA Employees — Create / Edit
entity: HCMv3.PerformanceManagement.PAEmployee
surfaces:
  - angular/src/app/admin/performance-management/pa-employees/create-or-edit-pa-employee-modal.component.html
audience: [admin]
permissions: [Pages.PerformanceManagement.PAEmployees.Create]
---

## Overview

A 360-style appraisal of one employee: the cycle, the form type, and the outcome -- proposed increment, proposed job title and areas to improve. Its KPIs, reviewers and ratings are held on its details.

An appraisal is raised about the employee, not by them, and goes through workflow for approval. The employee appraisal report shows its outcome fields.

## Employee {#PAEmployee.EmployeeId}
<!-- alias: loc:Employee, dom:PAEmployee_EmployeeId -->

**What it is.** The employee being appraised.

**Rules.** Required. Chosen from the employee list.

## PACycle {#PAEmployee.PACycleId}
<!-- alias: loc:PACycle, dom:PAEmployee_PACycleId -->

**What it is.** The appraisal cycle. Required to submit the appraisal.

**Example.** 2026 Annual

**Rules.** Required. Chosen from PA Cycle master data.

## FormType {#PAEmployee.FormTypeId}
<!-- alias: loc:FormType, dom:PAEmployee_FormTypeId -->

**What it is.** The form type.

**Why it matters.** Its maximum reviewers limits how many reviewers can be added. Required to submit the appraisal.

**Rules.** Required. Chosen from Form Type master data.

## ProposedIncrement {#PAEmployee.ProposedIncrement}
<!-- alias: loc:ProposedIncrement, dom:PAEmployee_ProposedIncrement -->

**What it is.** The salary increase proposed.

**Example.** `5%`

**Rules.** Optional. Maximum 256 characters.

## ProposedJobTitle {#PAEmployee.ProposedJobTitle}
<!-- alias: loc:ProposedJobTitle, dom:PAEmployee_ProposedJobTitle -->

**What it is.** The job title proposed.

**Example.** Senior Accountant

**Rules.** Optional. Maximum 256 characters.

## AreaOfImprovement {#PAEmployee.AreaOfImprovement}
<!-- alias: loc:AreaOfImprovement, dom:PAEmployee_AreaOfImprovement -->

**What it is.** Where the employee should improve.

**Rules.** Optional.

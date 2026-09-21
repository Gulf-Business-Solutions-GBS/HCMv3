---
helpId: cd.cdplan.edit
lang: en
title: CD Plans — Create / Edit
entity: HCMv3.CompetencyDevelopment.CDPlan
surfaces:
  - angular/src/app/admin/competency-development/cd-form-detail/cd-plans/create-or-edit-cd-plan-modal.component.html
audience: [admin]
permissions: []
---

## Overview

One competency in the employee's plan for the period. A competency can be listed once in this part of the form. Weights are recorded as entered; they are not totalled or checked here.

## Competency {#CDPlan.CompetencyId}
<!-- alias: loc:Competency, dom:CDPlan_CompetencyId -->

**What it is.** The competency.

**Example.** Teamwork

**Rules.** Optional. Chosen from Competency master data.

## Weight {#CDPlan.Weight}
<!-- alias: loc:Weight, dom:CDPlan_Weight -->

**What it is.** How much the competency counts, as a percentage.

**Example.** 20

**Rules.** Optional. Number.

## Target {#CDPlan.Target}
<!-- alias: loc:Target, dom:CDPlan_Target -->

**What it is.** What the employee is expected to achieve.

**Rules.** Optional. Maximum 512 characters.

## EmployeeComments {#CDPlan.EmployeeComments}
<!-- alias: loc:EmployeeComments, dom:CDPlan_EmployeeComments -->

**What it is.** The employee's comments.

**Rules.** Optional. Maximum 500 characters.

## ManagerComments {#CDPlan.ManagerComments}
<!-- alias: loc:ManagerComments, dom:CDPlan_ManagerComments -->

**What it is.** The manager's comments.

**Rules.** Optional. Maximum 500 characters.

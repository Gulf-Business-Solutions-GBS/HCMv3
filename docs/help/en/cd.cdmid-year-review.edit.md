---
helpId: cd.cdmid-year-review.edit
lang: en
title: CD Mid Year Reviews — Create / Edit
entity: HCMv3.CompetencyDevelopment.CDMidYearReview
surfaces:
  - angular/src/app/admin/competency-development/cd-form-detail/cd-mid-year-reviews/create-or-edit-cd-mid-year-review-modal.component.html
audience: [admin]
permissions: [Pages.CompetencyDevelopment.CDMidYearReviews.Create]
---

## Overview

One competency reviewed at mid-year. A competency can be listed once in this part of the form. Weights are recorded as entered; they are not totalled or checked here.

## Competency {#CDMidYearReview.CompetencyId}
<!-- alias: loc:Competency, dom:CDMidYearReview_CompetencyId -->

**What it is.** The competency.

**Example.** Teamwork

**Rules.** Optional. Chosen from Competency master data.

## Weight {#CDMidYearReview.Weight}
<!-- alias: loc:Weight, dom:CDMidYearReview_Weight -->

**What it is.** How much the competency counts, as a percentage.

**Example.** 20

**Rules.** Optional. Number.

## Target {#CDMidYearReview.Target}
<!-- alias: loc:Target, dom:CDMidYearReview_Target -->

**What it is.** What the employee is expected to achieve.

**Rules.** Optional. Maximum 512 characters.

## EmployeeComments {#CDMidYearReview.EmployeeComments}
<!-- alias: loc:EmployeeComments, dom:CDMidYearReview_EmployeeComments -->

**What it is.** The employee's comments.

**Rules.** Optional. Maximum 500 characters.

## ManagerComments {#CDMidYearReview.ManagerComments}
<!-- alias: loc:ManagerComments, dom:CDMidYearReview_ManagerComments -->

**What it is.** The manager's comments.

**Rules.** Optional. Maximum 500 characters.

---
helpId: cd.cdannual-year-review.edit
lang: en
title: CD Annual Year Reviews — Create / Edit
entity: HCMv3.CompetencyDevelopment.CDAnnualYearReview
surfaces:
  - angular/src/app/admin/competency-development/cd-form-detail/cd-annual-year-reviews/create-or-edit-cd-annual-year-review-modal.component.html
audience: [admin]
permissions: [Pages.CompetencyDevelopment.CDAnnualYearReviews.Create]
---

## Overview

One competency reviewed and rated at the end of the year. A competency can be listed once in this part of the form. Weights are recorded as entered; they are not totalled or checked here.

## Competency {#CDAnnualYearReview.CompetencyId}
<!-- alias: loc:Competency, dom:CDAnnualYearReview_CompetencyId -->

**What it is.** The competency.

**Example.** Teamwork

**Rules.** Optional. Chosen from Competency master data.

## Weight {#CDAnnualYearReview.Weight}
<!-- alias: loc:Weight, dom:CDAnnualYearReview_Weight -->

**What it is.** How much the competency counts, as a percentage.

**Example.** 20

**Rules.** Optional. Number.

## Target {#CDAnnualYearReview.Target}
<!-- alias: loc:Target, dom:CDAnnualYearReview_Target -->

**What it is.** What the employee is expected to achieve.

**Rules.** Optional. Maximum 512 characters.

## Rating {#CDAnnualYearReview.CDRatingId}
<!-- alias: loc:Rating, dom:CDAnnualYearReview_CDRatingId -->

**What it is.** The rating given for the year.

**Example.** Meets expectations

**Rules.** Optional. Chosen from CDRating master data.

## EmployeeComments {#CDAnnualYearReview.EmployeeComments}
<!-- alias: loc:EmployeeComments, dom:CDAnnualYearReview_EmployeeComments -->

**What it is.** The employee's comments.

**Rules.** Optional. Maximum 500 characters.

## ManagerComments {#CDAnnualYearReview.ManagerComments}
<!-- alias: loc:ManagerComments, dom:CDAnnualYearReview_ManagerComments -->

**What it is.** The manager's comments.

**Rules.** Optional. Maximum 500 characters.

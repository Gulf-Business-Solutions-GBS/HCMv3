---
helpId: perf.paannual-year-review.edit
lang: en
title: PA Annual Year Reviews — Create / Edit
entity: HCMv3.PerformanceManagement.PAAnnualYearReview
surfaces:
  - angular/src/app/admin/performance-management/pa-form-detail/pa-annual-year-reviews/create-or-edit-pa-annual-year-review-modal.component.html
audience: [admin]
permissions: []
---

## Overview

One KPI line of the year-end review on a PA Form: the KPI, its weight and target, the ratings from the appraisee and the appraiser, and their comments. Part of the older period-based appraisal form (PA Form).

Stored for the appraisal record; no score is worked out from these lines at present. Each KPI can appear once per form.

## KpiInfo {#PAAnnualYearReview.KpiInfoId}
<!-- alias: loc:KpiInfo, dom:PAAnnualYearReview_KpiInfoId -->

**What it is.** The KPI.

**Example.** Customer satisfaction

**Rules.** Optional. Chosen from KPI master data.

## Weight {#PAAnnualYearReview.Weight}
<!-- alias: loc:Weight, dom:PAAnnualYearReview_Weight -->

**What it is.** The KPI's share of the appraisal, as a percentage.

**Example.** `25`

**Rules.** Optional. Number.

## Target {#PAAnnualYearReview.Target}
<!-- alias: loc:Target, dom:PAAnnualYearReview_Target -->

**What it is.** The target set for the KPI.

**Example.** `90% on-time delivery`

**Rules.** Optional.

## AppraiseeRating {#PAAnnualYearReview.AppraiseeRatingId}
<!-- alias: loc:AppraiseeRating, dom:PAAnnualYearReview_AppraiseeRatingId -->

**What it is.** The employee's own rating.

**Example.** Meets expectations

**Rules.** Optional. Chosen from PA Rating master data.

## Rating {#PAAnnualYearReview.RatingId}
<!-- alias: loc:Rating, dom:PAAnnualYearReview_RatingId -->

**What it is.** The appraiser's rating.

**Example.** Exceeds expectations

**Rules.** Optional. Chosen from PA Rating master data.

## AppraiseeComments {#PAAnnualYearReview.AppraiseeComments}
<!-- alias: loc:AppraiseeComments, dom:PAAnnualYearReview_AppraiseeComments -->

**What it is.** The employee's comments.

**Rules.** Optional. Maximum 500 characters.

## AppraiserComments {#PAAnnualYearReview.AppraiserComments}
<!-- alias: loc:AppraiserComments, dom:PAAnnualYearReview_AppraiserComments -->

**What it is.** The appraiser's comments.

**Rules.** Optional. Maximum 500 characters.

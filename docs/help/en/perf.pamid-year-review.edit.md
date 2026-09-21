---
helpId: perf.pamid-year-review.edit
lang: en
title: PA Mid Year Reviews — Create / Edit
entity: HCMv3.PerformanceManagement.PAMidYearReview
surfaces:
  - angular/src/app/admin/performance-management/pa-form-detail/pa-mid-year-reviews/create-or-edit-pa-mid-year-review-modal.component.html
audience: [admin]
permissions: []
---

## Overview

One KPI line of the mid-year review on a PA Form: the KPI, its weight and target, the ratings from the appraisee and the appraiser, and their comments. Part of the older period-based appraisal form (PA Form).

Stored for the appraisal record; no score is worked out from these lines at present. Each KPI can appear once per form.

## KpiInfo {#PAMidYearReview.KpiInfoId}
<!-- alias: loc:KpiInfo, dom:PAMidYearReview_KpiInfoId -->

**What it is.** The KPI.

**Example.** Customer satisfaction

**Rules.** Optional. Chosen from KPI master data.

## Weight {#PAMidYearReview.Weight}
<!-- alias: loc:Weight, dom:PAMidYearReview_Weight -->

**What it is.** The KPI's share of the appraisal, as a percentage.

**Example.** `25`

**Rules.** Optional. Number.

## Target {#PAMidYearReview.Target}
<!-- alias: loc:Target, dom:PAMidYearReview_Target -->

**What it is.** The target set for the KPI.

**Example.** `90% on-time delivery`

**Rules.** Optional.

## AppraiseeRating {#PAMidYearReview.AppraiseeRatingId}
<!-- alias: loc:AppraiseeRating, dom:PAMidYearReview_AppraiseeRatingId -->

**What it is.** The employee's own rating.

**Example.** Meets expectations

**Rules.** Optional. Chosen from PA Rating master data.

## Rating {#PAMidYearReview.RatingId}
<!-- alias: loc:Rating, dom:PAMidYearReview_RatingId -->

**What it is.** The appraiser's rating.

**Example.** Exceeds expectations

**Rules.** Optional. Chosen from PA Rating master data.

## AppraiseeComments {#PAMidYearReview.AppraiseeComments}
<!-- alias: loc:AppraiseeComments, dom:PAMidYearReview_AppraiseeComments -->

**What it is.** The employee's comments.

**Rules.** Optional. Maximum 500 characters.

## AppraiserComments {#PAMidYearReview.AppraiserComments}
<!-- alias: loc:AppraiserComments, dom:PAMidYearReview_AppraiserComments -->

**What it is.** The appraiser's comments.

**Rules.** Optional. Maximum 500 characters.

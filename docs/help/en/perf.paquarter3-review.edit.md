---
helpId: perf.paquarter3-review.edit
lang: en
title: PA Quarter 3 Reviews — Create / Edit
entity: HCMv3.PerformanceManagement.PAQuarter3Review
surfaces:
  - angular/src/app/admin/performance-management/pa-form-detail/pa-quarter3-reviews/create-or-edit-pa-quarter3-review-modal.component.html
audience: [admin]
permissions: []
---

## Overview

One KPI line of the third-quarter review on a PA Form: the KPI, its weight and target, the ratings from the appraisee and the appraiser, and their comments. Part of the older period-based appraisal form (PA Form).

Stored for the appraisal record; no score is worked out from these lines at present. Each KPI can appear once per form.

## KpiInfo {#PAQuarter3Review.KpiInfoId}
<!-- alias: loc:KpiInfo, dom:PAQuarter3Review_KpiInfoId -->

**What it is.** The KPI.

**Example.** Customer satisfaction

**Rules.** Optional. Chosen from KPI master data.

## Weight {#PAQuarter3Review.Weight}
<!-- alias: loc:Weight, dom:PAQuarter3Review_Weight -->

**What it is.** The KPI's share of the appraisal, as a percentage.

**Example.** `25`

**Rules.** Optional. Number.

## Target {#PAQuarter3Review.Target}
<!-- alias: loc:Target, dom:PAQuarter3Review_Target -->

**What it is.** The target set for the KPI.

**Example.** `90% on-time delivery`

**Rules.** Optional.

## AppraiseeRating {#PAQuarter3Review.AppraiseeRatingId}
<!-- alias: loc:AppraiseeRating, dom:PAQuarter3Review_AppraiseeRatingId -->

**What it is.** The employee's own rating.

**Example.** Meets expectations

**Rules.** Optional. Chosen from PA Rating master data.

## Rating {#PAQuarter3Review.RatingId}
<!-- alias: loc:Rating, dom:PAQuarter3Review_RatingId -->

**What it is.** The appraiser's rating.

**Example.** Exceeds expectations

**Rules.** Optional. Chosen from PA Rating master data.

## AppraiseeComments {#PAQuarter3Review.AppraiseeComments}
<!-- alias: loc:AppraiseeComments, dom:PAQuarter3Review_AppraiseeComments -->

**What it is.** The employee's comments.

**Rules.** Optional. Maximum 500 characters.

## AppraiserComments {#PAQuarter3Review.AppraiserComments}
<!-- alias: loc:AppraiserComments, dom:PAQuarter3Review_AppraiserComments -->

**What it is.** The appraiser's comments.

**Rules.** Optional. Maximum 500 characters.

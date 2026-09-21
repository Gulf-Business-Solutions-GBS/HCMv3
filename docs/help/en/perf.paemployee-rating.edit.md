---
helpId: perf.paemployee-rating.edit
lang: en
title: PA Employee Ratings — Create / Edit
entity: HCMv3.PerformanceManagement.PAEmployeeRating
surfaces:
  - angular/src/app/admin/performance-management/pa-employee-detail/pa-employee-ratings/create-or-edit-pa-employee-rating-modal.component.html
audience: [admin]
permissions: []
---

## Overview

One reviewer's rating of one KPI on an appraisal. The scorecard and manager evaluation reports work from these ratings -- the scorecard uses only the managers' ratings when any exist in the report, otherwise all reviewers'.

## PAEmployeeReviewer {#PAEmployeeRating.PAEmployeeReviewerId}
<!-- alias: loc:PAEmployeeReviewer, dom:PAEmployeeRating_PAEmployeeReviewerId -->

**What it is.** The reviewer giving the rating.

**Rules.** Optional. Chosen from PA Employee Reviewer master data.

## PAEmployeeDetail {#PAEmployeeRating.PAEmployeeDetailId}
<!-- alias: loc:PAEmployeeDetail, dom:PAEmployeeRating_PAEmployeeDetailId -->

**What it is.** The KPI being rated.

**Rules.** Optional. Chosen from PA Employee Detail master data.

## Marks {#PAEmployeeRating.Marks}
<!-- alias: loc:Marks, dom:PAEmployeeRating_Marks -->

**What it is.** Marks given, shown on the manager evaluation report.

**Example.** `4`

**Rules.** Optional. Number.

## Rating {#PAEmployeeRating.RatingId}
<!-- alias: loc:Rating, dom:PAEmployeeRating_RatingId -->

**What it is.** The rating given.

**Example.** Exceeds expectations

**Rules.** Optional. Chosen from PA Rating master data.

---
helpId: perf.paplan.edit
lang: en
title: PA Plans — Create / Edit
entity: HCMv3.PerformanceManagement.PAPlan
surfaces:
  - angular/src/app/admin/performance-management/pa-form-detail/pa-plans/create-or-edit-pa-plan-modal.component.html
audience: [admin]
permissions: []
---

## Overview

One KPI line of the plan on a PA Form: the KPI, its weight and target, the ratings from the appraisee and the appraiser, and their comments. Part of the older period-based appraisal form (PA Form).

Stored for the appraisal record; no score is worked out from these lines at present. Each KPI can appear once per form.

## KpiInfo {#PAPlan.KpiInfoId}
<!-- alias: loc:KpiInfo, dom:PAPlan_KpiInfoId -->

**What it is.** The KPI.

**Example.** Customer satisfaction

**Rules.** Optional. Chosen from KPI master data.

## Weight {#PAPlan.Weight}
<!-- alias: loc:Weight, dom:PAPlan_Weight -->

**What it is.** The KPI's share of the appraisal, as a percentage.

**Example.** `25`

**Rules.** Optional. Number.

## Target {#PAPlan.Target}
<!-- alias: loc:Target, dom:PAPlan_Target -->

**What it is.** The target set for the KPI.

**Example.** `90% on-time delivery`

**Rules.** Optional.

## AppraiseeRating {#PAPlan.AppraiseeRatingId}
<!-- alias: loc:AppraiseeRating, dom:PAPlan_AppraiseeRatingId -->

**What it is.** The employee's own rating.

**Example.** Meets expectations

**Rules.** Optional. Chosen from PA Rating master data.

## Rating {#PAPlan.RatingId}
<!-- alias: loc:Rating, dom:PAPlan_RatingId -->

**What it is.** The appraiser's rating.

**Example.** Exceeds expectations

**Rules.** Optional. Chosen from PA Rating master data.

## AppraiseeComments {#PAPlan.AppraiseeComments}
<!-- alias: loc:AppraiseeComments, dom:PAPlan_AppraiseeComments -->

**What it is.** The employee's comments.

**Rules.** Optional. Maximum 500 characters.

## AppraiserComments {#PAPlan.AppraiserComments}
<!-- alias: loc:AppraiserComments, dom:PAPlan_AppraiserComments -->

**What it is.** The appraiser's comments.

**Rules.** Optional. Maximum 500 characters.

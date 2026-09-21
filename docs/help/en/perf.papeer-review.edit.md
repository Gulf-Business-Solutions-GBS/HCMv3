---
helpId: perf.papeer-review.edit
lang: en
title: PA Peer Reviews — Create / Edit
entity: HCMv3.PerformanceManagement.PAPeerReview
surfaces:
  - angular/src/app/admin/performance-management/pa-form-detail/pa-peer-reviews/create-or-edit-pa-peer-review-modal.component.html
audience: [admin]
permissions: []
---

## Overview

A peer's rating of one KPI on a PA Form. Part of the older period-based appraisal form (PA Form). Stored with the form; no report reads it at present.

## Peer {#PAPeerReview.PeerId}
<!-- alias: loc:Peer, dom:PAPeerReview_PeerId -->

**What it is.** The peer giving the rating.

**Rules.** Optional. Chosen from the employee list.

## KpiInfo {#PAPeerReview.KpiInfoId}
<!-- alias: loc:KpiInfo, dom:PAPeerReview_KpiInfoId -->

**What it is.** The KPI rated.

**Rules.** Optional. Chosen from KPI master data.

## Rating {#PAPeerReview.RatingId}
<!-- alias: loc:Rating, dom:PAPeerReview_RatingId -->

**What it is.** The rating given.

**Rules.** Optional. Chosen from PA Rating master data.

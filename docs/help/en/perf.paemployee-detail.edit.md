---
helpId: perf.paemployee-detail.edit
lang: en
title: PA Employee Details — Create / Edit
entity: HCMv3.PerformanceManagement.PAEmployeeDetail
surfaces:
  - angular/src/app/admin/performance-management/pa-employee-detail/pa-employee-details/create-or-edit-pa-employee-detail-modal.component.html
audience: [admin]
permissions: []
---

## Overview

One KPI on an appraisal, with its weight and target. Reviewers rate the appraisal KPI by KPI.

The employee scorecard report scores each KPI as its weight times the average rating, relative to the highest rating code, and adds them up. Each KPI can appear once per appraisal.

## KpiInfo {#PAEmployeeDetail.KpiInfoId}
<!-- alias: loc:KpiInfo, dom:PAEmployeeDetail_KpiInfoId -->

**What it is.** The KPI.

**Example.** Customer satisfaction

**Rules.** Optional. Chosen from KPI master data.

## Weight {#PAEmployeeDetail.Weight}
<!-- alias: loc:Weight, dom:PAEmployeeDetail_Weight -->

**What it is.** The KPI's share of the appraisal, as a percentage.

**Example.** `25`

**Rules.** Optional. Number.

## Target {#PAEmployeeDetail.Target}
<!-- alias: loc:Target, dom:PAEmployeeDetail_Target -->

**What it is.** The target set for the KPI.

**Rules.** Optional.

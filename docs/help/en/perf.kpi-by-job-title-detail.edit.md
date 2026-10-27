---
helpId: perf.kpi-by-job-title-detail.edit
lang: en
title: KPI By Job Title Details — Create / Edit
entity: HCMv3.PerformanceManagement.KpiByJobTitleDetail
surfaces:
  - angular/src/app/admin/performance-management/kpi-by-job-title-details/create-or-edit-kpi-by-job-title-detail-modal.component.html
audience: [admin]
permissions: []
---

## Overview

One KPI in a job title's set. Each KPI can appear once, and the weights in a set must not add up to more than 100.

## KpiByJobTitle {#KpiByJobTitleDetail.KpiByJobTitleId}
<!-- alias: loc:KpiByJobTitle, dom:KpiByJobTitleDetail_KpiByJobTitleId -->

**What it is.** The job title's KPI set.

**Rules.** Required. Chosen from KPI By Job Title master data.

## KpiInfo {#KpiByJobTitleDetail.KpiInfoId}
<!-- alias: loc:KpiInfo, dom:KpiByJobTitleDetail_KpiInfoId -->

**What it is.** The KPI.

**Rules.** Required. Chosen from KPI master data.

## Weight {#KpiByJobTitleDetail.Weight}
<!-- alias: loc:Weight, dom:KpiByJobTitleDetail_Weight -->

**What it is.** The KPI's weight, as a percentage.

**Example.** `20`

**Rules.** Required. Number.

## Target {#KpiByJobTitleDetail.Target}
<!-- alias: loc:Target, dom:KpiByJobTitleDetail_Target -->

**What it is.** The target for the KPI.

**Rules.** Optional.

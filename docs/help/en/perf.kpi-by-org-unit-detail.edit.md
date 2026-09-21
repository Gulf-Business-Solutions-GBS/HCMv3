---
helpId: perf.kpi-by-org-unit-detail.edit
lang: en
title: KPI By Org Unit Details — Create / Edit
entity: HCMv3.PerformanceManagement.KpiByOrgUnitDetail
surfaces:
  - angular/src/app/admin/performance-management/kpi-by-org-unit-details/create-or-edit-kpi-by-org-unit-detail-modal.component.html
audience: [admin]
permissions: []
---

## Overview

One KPI in an organisation unit's set. Each KPI can appear once, and the weights in a set must not add up to more than 100.

## KpiByOrgUnit {#KpiByOrgUnitDetail.KpiByOrgUnitId}
<!-- alias: loc:KpiByOrgUnit, dom:KpiByOrgUnitDetail_KpiByOrgUnitId -->

**What it is.** The unit's KPI set.

**Rules.** Required. Chosen from KPI By Org Unit master data.

## KpiInfo {#KpiByOrgUnitDetail.KpiInfoId}
<!-- alias: loc:KpiInfo, dom:KpiByOrgUnitDetail_KpiInfoId -->

**What it is.** The KPI.

**Rules.** Required. Chosen from KPI master data.

## Weight {#KpiByOrgUnitDetail.Weight}
<!-- alias: loc:Weight, dom:KpiByOrgUnitDetail_Weight -->

**What it is.** The KPI's weight, as a percentage.

**Example.** `20`

**Rules.** Required. Number.

## Target {#KpiByOrgUnitDetail.Target}
<!-- alias: loc:Target, dom:KpiByOrgUnitDetail_Target -->

**What it is.** The target for the KPI.

**Rules.** Optional.

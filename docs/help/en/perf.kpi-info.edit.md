---
helpId: perf.kpi-info.edit
lang: en
title: KPIs — Create / Edit
entity: HCMv3.PerformanceManagement.KpiInfo
surfaces:
  - angular/src/app/admin/performance-management/kpi-infos/create-or-edit-kpi-info-modal.component.html
audience: [admin]
permissions: [Pages.PerformanceManagement.KpiInfos.Create]
---

## Overview

A key performance indicator that appraisals rate.

## KpiCategory {#KpiInfo.KpiCategoryId}
<!-- alias: loc:KpiCategory, dom:KpiInfo_KpiCategoryId -->

**What it is.** The category the KPI belongs to.

**Example.** Customer

**Rules.** Required. Chosen from KPI Category master data.

## Active {#KpiInfo.Active}
<!-- alias: loc:Active, dom:KpiInfo_Active -->

**What it is.** Whether the KPI is in use.

**Why it matters.** Not read at present.

**Rules.** Yes or no.

## Competency {#KpiInfo.CompetencyId}
<!-- alias: loc:Competency, dom:KpiInfo_CompetencyId -->

**What it is.** The competency the KPI relates to, from the competencies list.

**Example.** Customer Focus

**Why it matters.** Recorded for reference; nothing else uses it.

**Rules.** Optional. Chosen from Competency master data.

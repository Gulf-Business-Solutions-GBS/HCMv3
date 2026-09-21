---
helpId: perf.kpi-by-job-title.edit
lang: en
title: KPI By Job Title — Create / Edit
entity: HCMv3.PerformanceManagement.KpiByJobTitle
surfaces:
  - angular/src/app/admin/performance-management/kpi-by-job-titles/create-or-edit-kpi-by-job-title-modal.component.html
audience: [admin]
permissions: [Pages.PerformanceManagement.KpiByJobTitles.Create]
---

## Overview

A set of KPIs for a job title, from a given date -- the KPIs and weights that apply to that role. Each job title can have one set per start date.

Stored as a template: appraisals do not take their KPIs from it automatically at present.

## JobTitle {#KpiByJobTitle.JobTitleId}
<!-- alias: loc:JobTitle, dom:KpiByJobTitle_JobTitleId -->

**What it is.** The job title.

**Example.** Accountant

**Rules.** Required. Chosen from Job Title master data.

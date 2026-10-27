---
helpId: cd.competency-by-job-title-detail.edit
lang: en
title: Competency By Job Title Details — Create / Edit
entity: HCMv3.CompetencyDevelopment.CompetencyByJobTitleDetail
surfaces:
  - angular/src/app/admin/competency-development/competency-by-job-title-details/create-or-edit-competency-by-job-title-detail-modal.component.html
audience: [admin]
permissions: []
---

## Overview

One competency expected of a job title, and its weight. A competency can be listed once per job title, and the weights together cannot exceed 100.

## CompetencyByJobTitle {#CompetencyByJobTitleDetail.CompetencyByJobTitleId}
<!-- alias: loc:CompetencyByJobTitle, dom:CompetencyByJobTitleDetail_CompetencyByJobTitleId -->

**What it is.** The job title's competency list.

**Rules.** Required. Chosen from Competency By Job Title master data.

## Competency {#CompetencyByJobTitleDetail.CompetencyId}
<!-- alias: loc:Competency, dom:CompetencyByJobTitleDetail_CompetencyId -->

**What it is.** The competency.

**Example.** Negotiation

**Rules.** Required. Chosen from Competency master data.

## Weight {#CompetencyByJobTitleDetail.Weight}
<!-- alias: loc:Weight, dom:CompetencyByJobTitleDetail_Weight -->

**What it is.** How much the competency counts, as a percentage.

**Example.** 25

**Rules.** Required. Number.

## Target {#CompetencyByJobTitleDetail.Target}
<!-- alias: loc:Target, dom:CompetencyByJobTitleDetail_Target -->

**What it is.** What is expected.

**Rules.** Optional. Maximum 512 characters.

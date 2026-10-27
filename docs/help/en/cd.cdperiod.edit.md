---
helpId: cd.cdperiod.edit
lang: en
title: CD Periods — Create / Edit
entity: HCMv3.CompetencyDevelopment.CDPeriod
surfaces:
  - angular/src/app/admin/competency-development/cd-periods/create-or-edit-cd-period-modal.component.html
audience: [admin]
permissions: [Pages.CompetencyDevelopment.CDPeriods.Create]
---

## Overview

A review cycle, usually a year, that employees' CD forms belong to.

## Name {#CDPeriod.Name}
<!-- alias: loc:Name, dom:CDPeriod_Name -->

**What it is.** The period's name. It must be unique.

**Example.** 2026

**Rules.** Required. Maximum 256 characters.

## Closed {#CDPeriod.Closed}
<!-- alias: loc:Closed, dom:CDPeriod_Closed -->

**What it is.** Whether the period is finished.

**Why it matters.** Shown in the list only: forms in a closed period can still be added and changed.

**Rules.** Yes or no.

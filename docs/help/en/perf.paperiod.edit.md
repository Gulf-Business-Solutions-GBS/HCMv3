---
helpId: perf.paperiod.edit
lang: en
title: PA Periods — Create / Edit
entity: HCMv3.PerformanceManagement.PAPeriod
surfaces:
  - angular/src/app/admin/performance-management/pa-periods/create-or-edit-pa-period-modal.component.html
audience: [admin]
permissions: [Pages.PerformanceManagement.PAPeriods.Create]
---

## Overview

An appraisal period for the older PA Form -- for example "FY 2026". Each employee can have one PA Form per period. The performance appraisal and PA summary reports can be filtered by it.

## Name {#PAPeriod.Name}
<!-- alias: loc:Name, dom:PAPeriod_Name -->

**What it is.** The period's name. It must be unique.

**Example.** FY 2026

**Rules.** Required. Maximum 256 characters.

## Closed {#PAPeriod.Closed}
<!-- alias: loc:Closed, dom:PAPeriod_Closed -->

**What it is.** Whether the period is closed.

**Why it matters.** Not read at present.

**Rules.** Yes or no.

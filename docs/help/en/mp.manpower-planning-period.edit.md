---
helpId: mp.manpower-planning-period.edit
lang: en
title: Manpower Planning Periods — Create / Edit
entity: HCMv3.ManpowerPlanningManagement.ManpowerPlanningPeriod
surfaces:
  - angular/src/app/admin/manpower-planning-management/manpower-planning-periods/create-or-edit-manpower-planning-period-modal.component.html
audience: [admin]
permissions: [Pages.ManpowerPlanningManagement.ManpowerPlanningPeriods.Create]
---

## Overview

A planning period, usually a budget year. The end date cannot be before the start date. Positions created from an approved plan start on the period's start date.

## Name {#ManpowerPlanningPeriod.Name}
<!-- alias: loc:Name, dom:ManpowerPlanningPeriod_Name -->

**What it is.** The period's name. It is shown to the approver.

**Example.** Budget 2027

**Rules.** Required. Maximum 200 characters.

## Active {#ManpowerPlanningPeriod.Active}
<!-- alias: loc:Active, dom:ManpowerPlanningPeriod_Active -->

**What it is.** Whether the period is open for planning.

**Why it matters.** Not read at present.

**Rules.** Yes or no.

---
helpId: tm.work-location-duty-period.edit
lang: en
title: Work Location Duty Periods — Create / Edit
entity: HCMv3.TimeManagement.WorkLocationDutyPeriod
surfaces:
  - angular/src/app/admin/time-management/work-location-duty-periods/create-or-edit-work-location-duty-period-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.WorkLocationDutyPeriods.Create]
---

## Overview

The duty period used at a work location, from a given date.

Not read at present: employees' duties come from their own duty periods and roster groups, and nothing reads this list.

## WorkLocation {#WorkLocationDutyPeriod.WorkLocationId}
<!-- alias: loc:WorkLocation, dom:WorkLocationDutyPeriod_WorkLocationId -->

**What it is.** The work location.

**Example.** Shuwaikh Warehouse

**Rules.** Required. Chosen from Work Location master data.

## DutyPeriod {#WorkLocationDutyPeriod.DutyPeriodId}
<!-- alias: loc:DutyPeriod, dom:WorkLocationDutyPeriod_DutyPeriodId -->

**What it is.** The duty period used there.

**Example.** Warehouse Shifts

**Rules.** Required. Chosen from Duty Period master data.

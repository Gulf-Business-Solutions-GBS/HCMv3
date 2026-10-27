---
helpId: pa.employee-extd-work-location.edit
lang: en
title: Employee Extended Work Locations — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeExtdWorkLocation
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/extd-work-locations/create-or-edit-employee-extd-work-location-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeExtdWorkLocations.Create]
---

## Overview

An additional work location the employee may work at for a period, besides the one on their org assignment. The same location cannot be recorded for overlapping periods.

It is recorded for reference; no calculation or approval step reads it at present.

## WorkLocationId {#EmployeeExtdWorkLocation.WorkLocationId}
<!-- alias: loc:WorkLocationId, dom:EmployeeExtdWorkLocation_WorkLocationId -->

**What it is.** The additional work location.

**Example.** Warehouse — Shuwaikh

**Rules.** Required. Chosen from Work Location master data.

:::admin
Maintained under Organization Management → Work Locations.
:::

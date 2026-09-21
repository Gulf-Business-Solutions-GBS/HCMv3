---
helpId: acc.building.edit
lang: en
title: Buildings — Create / Edit
entity: HCMv3.AccommodationManagement.Building
surfaces:
  - angular/src/app/admin/accommodation-management/buildings/create-or-edit-building-modal.component.html
audience: [admin]
permissions: [Pages.AccommodationManagement.Buildings.Create]
---

## Overview

A building used for company housing. Company housing is set up as a tree: location, building, floor, flat, room, bed. An employee's stay names one place at every level.

## Location {#Building.LocationId}
<!-- alias: loc:Location, dom:Building_LocationId -->

**What it is.** The location the building is in.

**Rules.** Required. Chosen from Location master data.

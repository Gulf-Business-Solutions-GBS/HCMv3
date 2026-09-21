---
helpId: acc.floor.edit
lang: en
title: Floors — Create / Edit
entity: HCMv3.AccommodationManagement.Floor
surfaces:
  - angular/src/app/admin/accommodation-management/floors/create-or-edit-floor-modal.component.html
audience: [admin]
permissions: [Pages.AccommodationManagement.Floors.Create]
---

## Overview

A floor of a building. Company housing is set up as a tree: location, building, floor, flat, room, bed. An employee's stay names one place at every level.

## Building {#Floor.BuildingId}
<!-- alias: loc:Building, dom:Floor_BuildingId -->

**What it is.** The building the floor is in.

**Rules.** Required. Chosen from Building master data.

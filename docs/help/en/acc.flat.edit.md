---
helpId: acc.flat.edit
lang: en
title: Flats — Create / Edit
entity: HCMv3.AccommodationManagement.Flat
surfaces:
  - angular/src/app/admin/accommodation-management/flats/create-or-edit-flat-modal.component.html
audience: [admin]
permissions: [Pages.AccommodationManagement.Flats.Create]
---

## Overview

A flat on a floor. Company housing is set up as a tree: location, building, floor, flat, room, bed. An employee's stay names one place at every level.

## Floor {#Flat.FloorId}
<!-- alias: loc:Floor, dom:Flat_FloorId -->

**What it is.** The floor the flat is on.

**Rules.** Required. Chosen from Floor master data.

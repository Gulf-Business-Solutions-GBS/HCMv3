---
helpId: acc.bed.edit
lang: en
title: Beds — Create / Edit
entity: HCMv3.AccommodationManagement.Bed
surfaces:
  - angular/src/app/admin/accommodation-management/beds/create-or-edit-bed-modal.component.html
audience: [admin]
permissions: [Pages.AccommodationManagement.Beds.Create]
---

## Overview

A bed in a room -- the place one employee is given. Company housing is set up as a tree: location, building, floor, flat, room, bed. An employee's stay names one place at every level.

## Room {#Bed.RoomId}
<!-- alias: loc:Room, dom:Bed_RoomId -->

**What it is.** The room the bed is in.

**Rules.** Required. Chosen from Room master data.

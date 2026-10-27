---
helpId: acc.room.edit
lang: en
title: Rooms — Create / Edit
entity: HCMv3.AccommodationManagement.Room
surfaces:
  - angular/src/app/admin/accommodation-management/rooms/create-or-edit-room-modal.component.html
audience: [admin]
permissions: [Pages.AccommodationManagement.Rooms.Create]
---

## Overview

A room in a flat. Company housing is set up as a tree: location, building, floor, flat, room, bed. An employee's stay names one place at every level.

## Flat {#Room.FlatId}
<!-- alias: loc:Flat, dom:Room_FlatId -->

**What it is.** The flat the room is in.

**Rules.** Required. Chosen from Flat master data.

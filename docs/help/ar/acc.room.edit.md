---
helpId: acc.room.edit
lang: ar
title: الغرف — إنشاء / تحرير
entity: HCMv3.AccommodationManagement.Room
surfaces:
  - angular/src/app/admin/accommodation-management/rooms/create-or-edit-room-modal.component.html
audience: [admin]
permissions: [Pages.AccommodationManagement.Rooms.Create]
---

## Overview

غرفة في شقة. يُعدّ سكن الشركة على شكل شجرة: الموقع، المبنى، الطابق، الشقة، الغرفة، السرير. وتحدد إقامة الموظف مكاناً واحداً في كل مستوى.

## Flat {#Room.FlatId}
<!-- alias: loc:Flat, dom:Room_FlatId -->

**ما هو.** الشقة التي تقع فيها الغرفة.

**القواعد.** إلزامي. يُختار من قائمة الشقق.

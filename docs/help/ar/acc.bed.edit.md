---
helpId: acc.bed.edit
lang: ar
title: الأسرّة — إنشاء / تحرير
entity: HCMv3.AccommodationManagement.Bed
surfaces:
  - angular/src/app/admin/accommodation-management/beds/create-or-edit-bed-modal.component.html
audience: [admin]
permissions: [Pages.AccommodationManagement.Beds.Create]
---

## Overview

سرير في غرفة -- المكان الذي يُعطى لموظف واحد. يُعدّ سكن الشركة على شكل شجرة: الموقع، المبنى، الطابق، الشقة، الغرفة، السرير. وتحدد إقامة الموظف مكاناً واحداً في كل مستوى.

## Room {#Bed.RoomId}
<!-- alias: loc:Room, dom:Bed_RoomId -->

**ما هو.** الغرفة التي يقع فيها السرير.

**القواعد.** إلزامي. يُختار من قائمة الغرف.

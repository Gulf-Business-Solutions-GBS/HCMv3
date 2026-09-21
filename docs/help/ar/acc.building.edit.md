---
helpId: acc.building.edit
lang: ar
title: المباني — إنشاء / تحرير
entity: HCMv3.AccommodationManagement.Building
surfaces:
  - angular/src/app/admin/accommodation-management/buildings/create-or-edit-building-modal.component.html
audience: [admin]
permissions: [Pages.AccommodationManagement.Buildings.Create]
---

## Overview

مبنى يُستخدم لسكن الشركة. يُعدّ سكن الشركة على شكل شجرة: الموقع، المبنى، الطابق، الشقة، الغرفة، السرير. وتحدد إقامة الموظف مكاناً واحداً في كل مستوى.

## Location {#Building.LocationId}
<!-- alias: loc:Location, dom:Building_LocationId -->

**ما هو.** الموقع الذي يقع فيه المبنى.

**القواعد.** إلزامي. يُختار من قائمة المواقع.

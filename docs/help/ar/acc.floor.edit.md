---
helpId: acc.floor.edit
lang: ar
title: الطوابق — إنشاء / تحرير
entity: HCMv3.AccommodationManagement.Floor
surfaces:
  - angular/src/app/admin/accommodation-management/floors/create-or-edit-floor-modal.component.html
audience: [admin]
permissions: [Pages.AccommodationManagement.Floors.Create]
---

## Overview

طابق في مبنى. يُعدّ سكن الشركة على شكل شجرة: الموقع، المبنى، الطابق، الشقة، الغرفة، السرير. وتحدد إقامة الموظف مكاناً واحداً في كل مستوى.

## Building {#Floor.BuildingId}
<!-- alias: loc:Building, dom:Floor_BuildingId -->

**ما هو.** المبنى الذي يقع فيه الطابق.

**القواعد.** إلزامي. يُختار من قائمة المباني.

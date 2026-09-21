---
helpId: acc.flat.edit
lang: ar
title: الشقق — إنشاء / تحرير
entity: HCMv3.AccommodationManagement.Flat
surfaces:
  - angular/src/app/admin/accommodation-management/flats/create-or-edit-flat-modal.component.html
audience: [admin]
permissions: [Pages.AccommodationManagement.Flats.Create]
---

## Overview

شقة في طابق. يُعدّ سكن الشركة على شكل شجرة: الموقع، المبنى، الطابق، الشقة، الغرفة، السرير. وتحدد إقامة الموظف مكاناً واحداً في كل مستوى.

## Floor {#Flat.FloorId}
<!-- alias: loc:Floor, dom:Flat_FloorId -->

**ما هو.** الطابق الذي تقع فيه الشقة.

**القواعد.** إلزامي. يُختار من قائمة الطوابق.

---
helpId: acc.building-contract.edit
lang: ar
title: عقود المباني — إنشاء / تحرير
entity: HCMv3.AccommodationManagement.BuildingContract
surfaces:
  - angular/src/app/admin/accommodation-management/building-contracts/create-or-edit-building-contract-modal.component.html
audience: [admin]
permissions: [Pages.AccommodationManagement.BuildingContracts.Create]
---

## Overview

فترة إيجار أو عقد لمبنى. لا يمكن أن يكون تاريخ الانتهاء قبل تاريخ البدء. يُحفظ كسجل، ولا يوجد ما ينبّه عند انتهاء العقد.

## Building {#BuildingContract.BuildingId}
<!-- alias: loc:Building, dom:BuildingContract_BuildingId -->

**ما هو.** المبنى الذي يخصه العقد.

**القواعد.** إلزامي. يُختار من قائمة المباني.

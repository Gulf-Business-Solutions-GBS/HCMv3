---
helpId: gp.gate-pass-vehicle.edit
lang: ar
title: مركبات تصاريح الدخول والخروج — إنشاء / تحرير
entity: HCMv3.GatePassManagement.GatePassVehicle
surfaces:
  - angular/src/app/admin/gate-pass-management/gate-pass-detail/vehicles/create-or-edit-gate-pass-vehicle-modal.component.html
audience: [admin]
permissions: [Pages.GatePassManagement.GatePassVehicles.Create]
---

## Overview

مركبة يشملها تصريح البوابة. ولا يُضاف رقم اللوحة إلا مرة واحدة لكل تصريح.

## VehicleType {#GatePassVehicle.VehicleTypeId}
<!-- alias: loc:VehicleType, dom:GatePassVehicle_VehicleTypeId -->

**ما هو.** نوع المركبة.

**مثال.** بيك أب

**القواعد.** إلزامي. يُختار من قائمة أنواع المركبات.

## VehiclePlateNumber {#GatePassVehicle.VehiclePlateNumber}
<!-- alias: loc:VehiclePlateNumber, dom:GatePassVehicle_VehiclePlateNumber -->

**ما هو.** رقم اللوحة.

**مثال.** 12/34567

**القواعد.** إلزامي. الحد الأقصى 64 حرفاً.

## VehicleBrand {#GatePassVehicle.VehicleBrandId}
<!-- alias: loc:VehicleBrand, dom:GatePassVehicle_VehicleBrandId -->

**ما هو.** الصنع.

**مثال.** تويوتا

**القواعد.** إلزامي. يُختار من قائمة ماركات المركبات.

## VehicleModel {#GatePassVehicle.VehicleModelId}
<!-- alias: loc:VehicleModel, dom:GatePassVehicle_VehicleModelId -->

**ما هو.** الطراز.

**مثال.** هايلكس

**القواعد.** إلزامي. يُختار من قائمة طرازات المركبات.

## VehicleDocExpiryDate {#GatePassVehicle.VehicleDocExpiryDate}
<!-- alias: loc:VehicleDocExpiryDate, dom:GatePassVehicle_VehicleDocExpiryDate -->

**ما هو.** متى تنتهي رخصة المركبة.

**لماذا يهم.** للتسجيل فقط: لا يوجد ما ينبّه عند حلوله.

**القواعد.** اختياري. تاريخ.

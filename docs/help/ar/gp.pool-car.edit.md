---
helpId: gp.pool-car.edit
lang: ar
title: سيارات المجمع — إنشاء / تحرير
entity: HCMv3.GatePassManagement.PoolCar
surfaces:
  - angular/src/app/admin/gate-pass-management/pool-cars/create-or-edit-pool-car-modal.component.html
audience: [admin]
permissions: [Pages.GatePassManagement.PoolCars.Create]
---

## Overview

سيارة للشركة يمكن إعطاؤها للموظفين لفترة.

## VehicleType {#PoolCar.VehicleTypeId}
<!-- alias: loc:VehicleType, dom:PoolCar_VehicleTypeId -->

**ما هو.** نوع المركبة.

**مثال.** بيك أب

**القواعد.** إلزامي. يُختار من قائمة أنواع المركبات.

## VehiclePlateNumber {#PoolCar.VehiclePlateNumber}
<!-- alias: loc:VehiclePlateNumber, dom:PoolCar_VehiclePlateNumber -->

**ما هو.** رقم اللوحة.

**مثال.** 12/34567

**القواعد.** إلزامي. الحد الأقصى 64 حرفاً.

## VehicleBrand {#PoolCar.VehicleBrandId}
<!-- alias: loc:VehicleBrand, dom:PoolCar_VehicleBrandId -->

**ما هو.** الصنع.

**مثال.** تويوتا

**القواعد.** إلزامي. يُختار من قائمة ماركات المركبات.

## VehicleModel {#PoolCar.VehicleModelId}
<!-- alias: loc:VehicleModel, dom:PoolCar_VehicleModelId -->

**ما هو.** الطراز.

**مثال.** هايلكس

**القواعد.** إلزامي. يُختار من قائمة طرازات المركبات.

## VehicleDocExpiryDate {#PoolCar.VehicleDocExpiryDate}
<!-- alias: loc:VehicleDocExpiryDate, dom:PoolCar_VehicleDocExpiryDate -->

**ما هو.** متى تنتهي رخصة المركبة.

**لماذا يهم.** للتسجيل فقط: لا يوجد ما ينبّه عند حلوله.

**القواعد.** إلزامي. تاريخ.

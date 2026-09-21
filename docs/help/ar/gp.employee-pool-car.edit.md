---
helpId: gp.employee-pool-car.edit
lang: ar
title: سيارات المجمع للموظفين — إنشاء / تحرير
entity: HCMv3.GatePassManagement.EmployeePoolCar
surfaces:
  - angular/src/app/admin/gate-pass-management/employee-pool-cars/create-or-edit-employee-pool-car-modal.component.html
audience: [admin]
permissions: [Pages.GatePassManagement.EmployeePoolCars.Create]
---

## Overview

سيارة من سيارات الشركة تُعطى لموظف يقودها، ومتى. ولا يجوز أن تكون النهاية قبل البداية. ولا يوجد ما يمنع إعطاء السيارة نفسها لسائقَين في الوقت نفسه.

## DriverEmployee {#EmployeePoolCar.DriverEmployeeId}
<!-- alias: loc:DriverEmployee, dom:EmployeePoolCar_DriverEmployeeId -->

**ما هو.** الموظف الذي يقود.

**القواعد.** إلزامي. يُختار من قائمة.

## PoolCar {#EmployeePoolCar.PoolCarId}
<!-- alias: loc:PoolCar, dom:EmployeePoolCar_PoolCarId -->

**ما هو.** السيارة.

**القواعد.** إلزامي. يُختار من قائمة سيارات المجمع.

## StartTime {#EmployeePoolCar.StartTime}
<!-- alias: loc:StartTime, dom:EmployeePoolCar_StartTime -->

**ما هو.** متى أُخذت السيارة.

**القواعد.** إلزامي. تاريخ.

## EndTime {#EmployeePoolCar.EndTime}
<!-- alias: loc:EndTime, dom:EmployeePoolCar_EndTime -->

**ما هو.** متى يجب إعادتها.

**القواعد.** إلزامي. تاريخ.

## IsReturned {#EmployeePoolCar.IsReturned}
<!-- alias: loc:IsReturned, dom:EmployeePoolCar_IsReturned -->

**ما هو.** ما إذا أُعيدت السيارة.

**القواعد.** نعم أو لا.

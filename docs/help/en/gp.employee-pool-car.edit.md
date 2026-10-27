---
helpId: gp.employee-pool-car.edit
lang: en
title: Employee Pool Cars — Create / Edit
entity: HCMv3.GatePassManagement.EmployeePoolCar
surfaces:
  - angular/src/app/admin/gate-pass-management/employee-pool-cars/create-or-edit-employee-pool-car-modal.component.html
audience: [admin]
permissions: [Pages.GatePassManagement.EmployeePoolCars.Create]
---

## Overview

A pool car given to an employee who drives it, and when. The end cannot be before the start. Nothing stops the same car being given to two drivers at once.

## DriverEmployee {#EmployeePoolCar.DriverEmployeeId}
<!-- alias: loc:DriverEmployee, dom:EmployeePoolCar_DriverEmployeeId -->

**What it is.** The employee driving.

**Rules.** Required. Chosen from a list.

## PoolCar {#EmployeePoolCar.PoolCarId}
<!-- alias: loc:PoolCar, dom:EmployeePoolCar_PoolCarId -->

**What it is.** The car.

**Rules.** Required. Chosen from Pool Car master data.

## StartTime {#EmployeePoolCar.StartTime}
<!-- alias: loc:StartTime, dom:EmployeePoolCar_StartTime -->

**What it is.** When the car was taken.

**Rules.** Required. Date value.

## EndTime {#EmployeePoolCar.EndTime}
<!-- alias: loc:EndTime, dom:EmployeePoolCar_EndTime -->

**What it is.** When it is due back.

**Rules.** Required. Date value.

## IsReturned {#EmployeePoolCar.IsReturned}
<!-- alias: loc:IsReturned, dom:EmployeePoolCar_IsReturned -->

**What it is.** Whether the car has been returned.

**Rules.** Yes or no.

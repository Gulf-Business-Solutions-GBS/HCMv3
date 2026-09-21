---
helpId: gp.pool-car.edit
lang: en
title: Pool Cars — Create / Edit
entity: HCMv3.GatePassManagement.PoolCar
surfaces:
  - angular/src/app/admin/gate-pass-management/pool-cars/create-or-edit-pool-car-modal.component.html
audience: [admin]
permissions: [Pages.GatePassManagement.PoolCars.Create]
---

## Overview

A company car that employees can be given for a time.

## VehicleType {#PoolCar.VehicleTypeId}
<!-- alias: loc:VehicleType, dom:PoolCar_VehicleTypeId -->

**What it is.** The kind of vehicle.

**Example.** Pickup

**Rules.** Required. Chosen from Vehicle Type master data.

## VehiclePlateNumber {#PoolCar.VehiclePlateNumber}
<!-- alias: loc:VehiclePlateNumber, dom:PoolCar_VehiclePlateNumber -->

**What it is.** The plate number.

**Example.** 12/34567

**Rules.** Required. Maximum 64 characters.

## VehicleBrand {#PoolCar.VehicleBrandId}
<!-- alias: loc:VehicleBrand, dom:PoolCar_VehicleBrandId -->

**What it is.** The make.

**Example.** Toyota

**Rules.** Required. Chosen from Vehicle Brand master data.

## VehicleModel {#PoolCar.VehicleModelId}
<!-- alias: loc:VehicleModel, dom:PoolCar_VehicleModelId -->

**What it is.** The model.

**Example.** Hilux

**Rules.** Required. Chosen from Vehicle Model master data.

## VehicleDocExpiryDate {#PoolCar.VehicleDocExpiryDate}
<!-- alias: loc:VehicleDocExpiryDate, dom:PoolCar_VehicleDocExpiryDate -->

**What it is.** When the vehicle's registration expires.

**Why it matters.** A record only: nothing warns when it passes.

**Rules.** Required. Date value.

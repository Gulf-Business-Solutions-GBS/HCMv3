---
helpId: gp.gate-pass-vehicle.edit
lang: en
title: Gate Pass Vehicles — Create / Edit
entity: HCMv3.GatePassManagement.GatePassVehicle
surfaces:
  - angular/src/app/admin/gate-pass-management/gate-pass-detail/vehicles/create-or-edit-gate-pass-vehicle-modal.component.html
audience: [admin]
permissions: [Pages.GatePassManagement.GatePassVehicles.Create]
---

## Overview

A vehicle covered by a gate pass. A plate number can be added once per pass.

## VehicleType {#GatePassVehicle.VehicleTypeId}
<!-- alias: loc:VehicleType, dom:GatePassVehicle_VehicleTypeId -->

**What it is.** The kind of vehicle.

**Example.** Pickup

**Rules.** Required. Chosen from Vehicle Type master data.

## VehiclePlateNumber {#GatePassVehicle.VehiclePlateNumber}
<!-- alias: loc:VehiclePlateNumber, dom:GatePassVehicle_VehiclePlateNumber -->

**What it is.** The plate number.

**Example.** 12/34567

**Rules.** Required. Maximum 64 characters.

## VehicleBrand {#GatePassVehicle.VehicleBrandId}
<!-- alias: loc:VehicleBrand, dom:GatePassVehicle_VehicleBrandId -->

**What it is.** The make.

**Example.** Toyota

**Rules.** Required. Chosen from Vehicle Brand master data.

## VehicleModel {#GatePassVehicle.VehicleModelId}
<!-- alias: loc:VehicleModel, dom:GatePassVehicle_VehicleModelId -->

**What it is.** The model.

**Example.** Hilux

**Rules.** Required. Chosen from Vehicle Model master data.

## VehicleDocExpiryDate {#GatePassVehicle.VehicleDocExpiryDate}
<!-- alias: loc:VehicleDocExpiryDate, dom:GatePassVehicle_VehicleDocExpiryDate -->

**What it is.** When the vehicle's registration expires.

**Why it matters.** A record only: nothing warns when it passes.

**Rules.** Optional. Date value.

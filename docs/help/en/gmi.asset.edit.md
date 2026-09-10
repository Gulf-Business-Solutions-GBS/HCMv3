---
helpId: gmi.asset.edit
lang: en
title: Assets — Create / Edit
entity: HCMv3.GeneralMasterInfo.Asset
surfaces:
  - angular/src/app/admin/general-master-info/assets/create-or-edit-asset-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.Assets.Create]
---

## Overview

Company assets that can be issued to employees -- vehicles and equipment. Assets issued to an employee are recorded on the employee's record, and employees can request assets through self-service, which offers only active assets.

The descriptive details below are printed on the employee asset report.

## Brand {#Asset.Brand}
<!-- alias: loc:Brand, dom:Asset_Brand -->

**What it is.** The asset's make.

**Example.** `Toyota`

**Rules.** Optional. Maximum 256 characters.

## Model {#Asset.Model}
<!-- alias: loc:Model, dom:Asset_Model -->

**What it is.** The asset's model.

**Example.** `Land Cruiser`

**Rules.** Optional. Maximum 256 characters.

## Color {#Asset.Color}
<!-- alias: loc:Color, dom:Asset_Color -->

**What it is.** The asset's colour.

**Example.** `White`

**Rules.** Optional. Maximum 64 characters.

## VIN_Number {#Asset.VIN_Number}
<!-- alias: loc:VIN_Number, dom:Asset_VinNumber -->

**What it is.** The vehicle identification number, for vehicles.

**Example.** `JTMHV05J804012345`

**Rules.** Optional. Maximum 64 characters.

## Serial {#Asset.Serial}
<!-- alias: loc:Serial, dom:Asset_Serial -->

**What it is.** The manufacturer's serial number, for equipment.

**Example.** `SN-4471902`

**Rules.** Optional. Maximum 64 characters.

## PlateNumber {#Asset.PlateNumber}
<!-- alias: loc:PlateNumber, dom:Asset_PlateNumber -->

**What it is.** The registration plate, for vehicles.

**Example.** `12/34567`

**Rules.** Optional. Maximum 64 characters.

## Barcode {#Asset.Barcode}
<!-- alias: loc:Barcode, dom:Asset_Barcode -->

**What it is.** The asset tag or barcode the organisation has fixed to the asset.

**Example.** `AST-000312`

**Rules.** Optional. Maximum 64 characters.

## Year {#Asset.Year}
<!-- alias: loc:Year, dom:Asset_Year -->

**What it is.** The year associated with the asset, typically a vehicle's model year.

**Example.** `2022`

**Rules.** Optional. Whole number.

## ExpiryDate {#Asset.ExpiryDate}
<!-- alias: loc:ExpiryDate, dom:Asset_ExpiryDate -->

**What it is.** The date something about the asset expires, such as a vehicle's registration or an equipment warranty.

**Rules.** Optional. Date value.

## Value {#Asset.Value}
<!-- alias: loc:Value, dom:Asset_Value -->

**What it is.** The asset's value.

**Example.** `8500`

**Rules.** Optional. Number.

## AssetType {#Asset.AssetTypeId}
<!-- alias: loc:AssetType, dom:Asset_AssetTypeId -->

**What it is.** The kind of asset, from the asset types list.

**Rules.** Optional. Chosen from Asset Type master data.

:::admin
Asset types are maintained under General Master Info → Asset Types.
:::

## Status {#Asset.Status}
<!-- alias: loc:Status, dom:Asset_Status -->

**What it is.** Whether the asset is in use.

**Why it matters.** Only active assets are offered when employees request an asset through self-service.

**Rules.** Required. One of: Active, Inactive.

## AssetAllocationStatus {#Asset.AssetAllocationStatus}
<!-- alias: loc:AssetAllocationStatus, dom:Asset_AssetAllocationStatus -->

**What it is.** Whether the asset is currently issued to someone.

**Why it matters.** Set by hand -- issuing the asset to an employee does not change it. It is shown on the asset report.

**Rules.** Required. One of: Occupied, Unoccupied.

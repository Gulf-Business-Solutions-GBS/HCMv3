---
helpId: gp.gate-pass-material.edit
lang: en
title: Gate Pass Materials — Create / Edit
entity: HCMv3.GatePassManagement.GatePassMaterial
surfaces:
  - angular/src/app/admin/gate-pass-management/gate-pass-detail/materials/create-or-edit-gate-pass-material-modal.component.html
audience: [admin]
permissions: [Pages.GatePassManagement.GatePassMaterials.Create]
---

## Overview

An item taken through the gate on a pass, and how much has come back.

## ItemCode {#GatePassMaterial.ItemCode}
<!-- alias: loc:ItemCode, dom:GatePassMaterial_ItemCode -->

**What it is.** The item's code.

**Rules.** Required. Maximum 128 characters.

## ItemName {#GatePassMaterial.ItemName}
<!-- alias: loc:ItemName, dom:GatePassMaterial_ItemName -->

**What it is.** The item's name.

**Example.** Laptop

**Rules.** Required. Maximum 256 characters.

## Quantity {#GatePassMaterial.Quantity}
<!-- alias: loc:Quantity, dom:GatePassMaterial_Quantity -->

**What it is.** How many went out.

**Example.** 2

**Rules.** Required. Whole number.

## ExpectedReturnDate {#GatePassMaterial.ExpectedReturnDate}
<!-- alias: loc:ExpectedReturnDate, dom:GatePassMaterial_ExpectedReturnDate -->

**What it is.** When the items should come back.

**Why it matters.** A record only: nothing warns when it passes.

**Rules.** Required. Date value.

## ReturnedQuantity {#GatePassMaterial.ReturnedQuantity}
<!-- alias: loc:ReturnedQuantity, dom:GatePassMaterial_ReturnedQuantity -->

**What it is.** How many have come back.

**Example.** 1

**Rules.** Required. Whole number.

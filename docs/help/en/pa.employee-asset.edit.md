---
helpId: pa.employee-asset.edit
lang: en
title: Employee Assets — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeAsset
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/assets/create-or-edit-employee-asset-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeAssets.Create]
---

## Overview

A company asset issued to the employee for a period -- a laptop, phone or car.

The same asset cannot be assigned for overlapping periods, whichever employee holds it. Employees can request assets through self-service.

## AssetId {#EmployeeAsset.AssetId}
<!-- alias: loc:AssetId, dom:EmployeeAsset_AssetId -->

**What it is.** The asset issued.

**Example.** Laptop — Dell 5540 (SN 7GH21)

**Why it matters.** Cannot be assigned to anyone else for an overlapping period.

**Rules.** Required. Chosen from Asset master data.

:::admin
Maintained under General Master Info → Assets.
:::

## AssetDeductionMode {#EmployeeAsset.AssetDeductionMode}
<!-- alias: loc:AssetDeductionMode, dom:EmployeeAsset_AssetDeductionMode -->

**What it is.** Whether the employee pays for the asset's invoices, and how much.

**Example.** Above Allowance

**Why it matters.** Used by the asset invoice report to work out the employee's share. Nothing is deducted in payroll from it.

**Rules.** Required. One of: None, Full, Above Allowance.

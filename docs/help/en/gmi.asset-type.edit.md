---
helpId: gmi.asset-type.edit
lang: en
title: Asset Types — Create / Edit
entity: HCMv3.GeneralMasterInfo.AssetType
surfaces:
  - angular/src/app/admin/general-master-info/asset-types/create-or-edit-asset-type-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.AssetTypes.Create]
---

## Overview

The kinds of company asset -- vehicles, laptops, phones and so on. Each asset names its type, and the type decides whether employees can ask for assets of that kind in self-service.

Besides the code and English and Arabic names, a type has a wage type and the Include In Workflow switch. The code is assigned automatically when left blank, and codes and names must be unique.

## WageType {#AssetType.WageTypeId}
<!-- alias: loc:WageType, dom:AssetType_WageTypeId -->

**What it is.** The wage type meant for payments for assets of this type.

**Example.** Car Allowance

**Why it matters.** Recorded only; payroll and the asset reports do not read it at present.

**Rules.** Optional. Chosen from Wage Type master data.

## IncludeInWorkflow {#AssetType.IncludeInWorkflow}
<!-- alias: loc:IncludeInWorkflow, dom:AssetType_IncludeInWorkflow -->

**What it is.** Whether employees may request assets of this type through self-service.

**Example.** Ticked for Laptop and Mobile Phone; unticked for Vehicle, which only HR issues.

**Why it matters.** Self-service offers only active assets whose type has this ticked; an asset with no type is never offered. A new type starts unticked, so tick it for every type employees should be able to ask for. HR can still issue assets of any type on the employee's record.

**Rules.** Yes or no.

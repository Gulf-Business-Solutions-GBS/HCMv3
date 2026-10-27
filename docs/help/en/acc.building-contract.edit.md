---
helpId: acc.building-contract.edit
lang: en
title: Building Contracts — Create / Edit
entity: HCMv3.AccommodationManagement.BuildingContract
surfaces:
  - angular/src/app/admin/accommodation-management/building-contracts/create-or-edit-building-contract-modal.component.html
audience: [admin]
permissions: [Pages.AccommodationManagement.BuildingContracts.Create]
---

## Overview

A lease or contract period for a building. The end date cannot be before the start date. Kept as a record; nothing warns when a contract ends.

## Building {#BuildingContract.BuildingId}
<!-- alias: loc:Building, dom:BuildingContract_BuildingId -->

**What it is.** The building the contract is for.

**Rules.** Required. Chosen from Building master data.

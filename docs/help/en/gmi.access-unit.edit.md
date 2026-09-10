---
helpId: gmi.access-unit.edit
lang: en
title: Access Units — Create / Edit
entity: HCMv3.GeneralMasterInfo.AccessUnit
surfaces:
  - angular/src/app/admin/general-master-info/access-units/create-or-edit-access-unit-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.AccessUnits.Create]
---

## Overview

An access unit is one attendance or access-control device. Each punch imported from the devices records the access unit it came from, matched by the device's network address.

When punches arrive from an address that no access unit has, a new unit is created automatically, named after the device -- or after its address, if the name is already taken by a unit at a different address. So the list also grows by itself as devices are added.

## IPAddress {#AccessUnit.IPAddress}
<!-- alias: loc:IPAddress, dom:AccessUnit_IPAddress -->

**What it is.** The network address of the device, as the attendance import sees it.

**Example.** `192.168.1.50`

**Why it matters.** It is how imported punches are matched to this unit. If the device's address changes, change it here too -- otherwise the next import creates a second unit for the new address.

**Rules.** Optional.

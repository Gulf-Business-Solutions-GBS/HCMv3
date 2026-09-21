---
helpId: trv.travel-per-diem.edit
lang: en
title: Travel Per Diems — Create / Edit
entity: HCMv3.TravelManagement.TravelPerDiem
surfaces:
  - angular/src/app/admin/travel-management/travel-per-diems/create-or-edit-travel-per-diem-modal.component.html
audience: [admin]
permissions: [Pages.TravelManagement.TravelPerDiems.Create]
---

## Overview

The daily allowance for a route and an employee group and sub group. Only one rate can exist for each combination. It is used only to show an expected per diem on a trip leg.

## FromDestination {#TravelPerDiem.FromDestinationId}
<!-- alias: loc:FromDestination, dom:TravelPerDiem_FromDestinationId -->

**What it is.** Where the route starts.

**Example.** Kuwait

**Rules.** Required. Chosen from Travel Destination master data.

## ToDestination {#TravelPerDiem.ToDestinationId}
<!-- alias: loc:ToDestination, dom:TravelPerDiem_ToDestinationId -->

**What it is.** Where the route ends.

**Example.** London

**Rules.** Required. Chosen from Travel Destination master data.

## EmployeeGroup {#TravelPerDiem.EmployeeGroupId}
<!-- alias: loc:EmployeeGroup, dom:TravelPerDiem_EmployeeGroupId -->

**What it is.** The employee group.

**Rules.** Required. Chosen from a list.

## EmployeeSubGroup {#TravelPerDiem.EmployeeSubGroupId}
<!-- alias: loc:EmployeeSubGroup, dom:TravelPerDiem_EmployeeSubGroupId -->

**What it is.** The employee sub group.

**Rules.** Required. Chosen from a list.

## Amount {#TravelPerDiem.Amount}
<!-- alias: loc:Amount, dom:TravelPerDiem_Amount -->

**What it is.** The allowance per night.

**Example.** 45.000

**Rules.** Required. Number.

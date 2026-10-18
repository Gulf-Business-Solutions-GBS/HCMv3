---
helpId: trv.travel-type.edit
lang: en
title: Travel Types — Create / Edit
entity: HCMv3.TravelManagement.TravelType
surfaces:
  - angular/src/app/admin/travel-management/travel-types/create-or-edit-travel-type-modal.component.html
audience: [admin]
permissions: [Pages.TravelManagement.TravelTypes.Create]
---

## Overview

A kind of trip, such as a business meeting or training.

## WageType {#TravelType.WageTypeId}
<!-- alias: loc:WageType, dom:TravelType_WageTypeId -->

**What it is.** The wagetype travel would be paid through.

**Why it matters.** Not read at present: payroll has no travel calculation, so nothing is paid through it.

**Rules.** Required. Chosen from Wage Type master data.

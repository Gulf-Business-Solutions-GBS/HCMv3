---
helpId: gmi.city.edit
lang: en
title: Cities — Create / Edit
entity: HCMv3.GeneralMasterInfo.City
surfaces:
  - angular/src/app/admin/general-master-info/cities/create-or-edit-city-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.Cities.Create]
---

## Overview

The cities used across HCMv3 -- most visibly as an employee's place of birth.

## State {#City.StateId}
<!-- alias: loc:State, dom:City_StateId -->

**What it is.** The state or region the city is in, from the states list.

**Why it matters.** Recorded for reference; nothing else uses it.

**Rules.** Optional. Chosen from State master data.

---
helpId: hs.first-aid-box.edit
lang: en
title: First Aid Box — Create / Edit
entity: HCMv3.HealthAndSafetyManagement.FirstAidBox
surfaces:
  - angular/src/app/admin/health-and-safety-management/first-aid-boxes/create-or-edit-first-aid-box-modal.component.html
audience: [admin]
permissions: []
---

## Overview

A first-aid box, where it is and who looks after it. Its medicines are listed as details. Leave the code blank to have the next number given.

## Location {#FirstAidBox.LocationId}
<!-- alias: loc:Location, dom:FirstAidBox_LocationId -->

**What it is.** The work location.

**Example.** Main warehouse

**Rules.** Required. Chosen from Work Location master data.

## AllocationDate {#FirstAidBox.AllocationDate}
<!-- alias: loc:AllocationDate, dom:FirstAidBox_AllocationDate -->

**What it is.** When the box was put in place.

**Rules.** Required. Date value.

## ResponsiblePerson {#FirstAidBox.ResponsiblePersonId}
<!-- alias: loc:ResponsiblePerson, dom:FirstAidBox_ResponsiblePersonId -->

**What it is.** Who looks after the box.

**Rules.** Required. Chosen from the employee list.

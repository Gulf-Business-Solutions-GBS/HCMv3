---
helpId: gmi.clearance-request-field.edit
lang: en
title: Clearance Request Fields — Create / Edit
entity: HCMv3.GeneralMasterInfo.ClearanceRequestField
surfaces:
  - angular/src/app/admin/general-master-info/clearance-request-fields/create-or-edit-clearance-request-field-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.ClearanceRequestFields.Create]
---

## Overview

The lines of the exit clearance checklist -- "laptop returned", "access badge handed in" -- that departments confirm before an employee leaves.

Details on a line in the list opens that line's own page: its fields at the top, with Edit, and below them the departments (workflow roles) it is assigned to, which can be added, changed and removed there.

## ClearanceRequestType {#ClearanceRequestField.ClearanceRequestTypeId}
<!-- alias: loc:ClearanceRequestType, dom:ClearanceRequestField_ClearanceRequestTypeId -->

**What it is.** The clearance type this line belongs to.

**Why it matters.** A line with a type appears only on clearances of that type. A line with no type appears on every clearance.

**Rules.** Optional. Chosen from Clearance Request Type master data.

## IsActive {#ClearanceRequestField.IsActive}
<!-- alias: loc:IsActive, dom:ClearanceRequestField_IsActive -->

**What it is.** Whether the line is in use.

**Why it matters.** Only active lines are put on clearance checklists.

**Rules.** Yes or no.

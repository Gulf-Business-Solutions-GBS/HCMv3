---
helpId: org.organization-unit-type.edit
lang: en
title: Organization Unit Types — Create / Edit
entity: HCMv3.OrganizationManagement.OrganizationUnitType
surfaces:
  - angular/src/app/admin/organization-management/organization-unit-types/create-or-edit-organization-unit-type-modal.component.html
audience: [admin]
permissions: [Pages.OrganizationManagement.OrganizationUnitTypes.Create]
---

## Overview

A level of the organisation tree, such as company, division or department. The code must be one of the fixed levels Level0 to Level9, where Level0 is the top.

## AllowLeaveConflict {#OrganizationUnitType.AllowLeaveConflict}
<!-- alias: loc:AllowLeaveConflict, dom:OrganizationUnitType_AllowLeaveConflict -->

**What it is.** Whether leave may overlap within units of this level.

**Why it matters.** Only one level can have it ticked. Not read at present.

**Rules.** Yes or no.

## AssignPayrollArea {#OrganizationUnitType.AssignPayrollArea}
<!-- alias: loc:AssignPayrollArea, dom:OrganizationUnitType_AssignPayrollArea -->

**What it is.** Whether units of this level carry a payroll area.

**Why it matters.** Only one level other than Level0 can have it ticked. Go-Live Setup reads it when it creates the company unit, giving the unit a payroll area only when the level has this ticked.

**Rules.** Yes or no.

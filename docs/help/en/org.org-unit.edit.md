---
helpId: org.org-unit.edit
lang: en
title: Organization Units — Create / Edit
entity: HCMv3.OrganizationManagement.OrgUnitProfile
surfaces:
  - angular/src/app/admin/organization-management/org-units/create-or-edit-org-unit-modal.component.html
audience: [admin]
permissions: [Pages.OrganizationManagement.OrgUnits]
---

## Overview

A unit of the organisation tree -- company, division, department and so on -- with its English and Arabic names and its parent unit. The tree can be at most 10 levels deep. Leave the code blank to have the next number given. Employees are placed in a unit on their organisation assignment; reports find an employee's unit at each level by walking up from that unit.

## OrganizationUnitType {#OrgUnitProfile.OrganizationUnitTypeId}
<!-- alias: loc:OrganizationUnitType, dom:OrgUnit_OrganizationUnitTypeId -->

**What it is.** The level of the unit in the tree.

**Example.** Department

**Rules.** Required. Chosen from Organization Unit Type master data.

## Address {#OrgUnitProfile.Address}
<!-- alias: loc:Address, dom:OrgUnit_Address -->

**What it is.** The unit's address.

**Why it matters.** Not read at present.

**Rules.** Optional. Maximum 4000 characters.

## OrgGeoLocation {#OrgUnitProfile.OrgGeoLocation}
<!-- alias: loc:OrgGeoLocation, dom:OrgUnit_OrgGeoLocation -->

**What it is.** The unit's map location.

**Why it matters.** Not read at present.

**Rules.** Optional. Maximum 500 characters.

## OrgUnitLogo {#OrgUnitProfile.LogoPath}
<!-- alias: loc:OrgUnitLogo, dom:OrgUnit_Logo -->

**What it is.** The unit's logo, for printing on letters, payslips and bank reports. PNG, JPEG or GIF, up to 1 MB; Remove clears it.

**Why it matters.** Kept as a file in the tenant's file store, not in the database. No printed report shows it yet; reports pick it up as each one is updated to read it.

**Rules.** Optional.

## PayrollAreaId {#OrgUnitProfile.PayrollAreaId}
<!-- alias: loc:PayrollAreaId, dom:OrgUnit_PayrollAreaId -->

**What it is.** The payroll area of employees in this unit.

**Example.** Monthly - Kuwait

**Why it matters.** Used only when an employee's organisation assignment has no payroll area of its own; it is not passed down to units below. Separately, some payroll amounts, such as insurance recoveries, are rounded to the decimal places of this payroll area's currency for every employee in the unit, even one whose assignment names another payroll area; with no payroll area here they are not rounded.

**Rules.** Optional. Chosen from Payroll Area master data.

---
helpId: sec.sys-user-org-unit-data.edit
lang: en
title: User Org Unit Data — Create / Edit
entity: HCMv3.SystemSecurity.SysUserOrgUnitData
surfaces:
  - angular/src/app/admin/system-security/sys-user-org-unit-datas/create-or-edit-sys-user-org-unit-data-modal.component.html
audience: [admin]
permissions: []
---

## Overview

Row-level security scope: which part of the organisation the users in a role may see. A user sees employees in the organisation units given to any of their roles, and everything below those units. A row at the top organisation level lets the role see everyone.

Applied only when *EnableRowLevelSecurity* is ticked in Security Settings.

## Employee {#SysUserOrgUnitData.EmployeeId}
<!-- alias: loc:Employee, dom:SysUserOrgUnitData_EmployeeId -->

**What it is.** An employee the scope is for.

**Why it matters.** Not read at present.

**Rules.** Optional. Chosen from the employee list.

## Role {#SysUserOrgUnitData.RoleId}
<!-- alias: loc:Role, dom:SysUserOrgUnitData_RoleId -->

**What it is.** The role the scope applies to.

**Example.** HR Officer - Kuwait

**Rules.** Required. Chosen from Role master data.

## OrgLevel {#SysUserOrgUnitData.OrgLevelId}
<!-- alias: loc:OrgLevel, dom:SysUserOrgUnitData_OrgLevelId -->

**What it is.** The organisation level of the unit.

**Why it matters.** The top level (level 0) grants the role every employee.

**Rules.** Required. Chosen from Organization Unit Type master data.

## OrgUnit {#SysUserOrgUnitData.OrganizationUnitId}
<!-- alias: loc:OrgUnit, dom:SysUserOrgUnitData_OrganizationUnitId -->

**What it is.** The organisation unit the role may see.

**Example.** Kuwait Branch

**Why it matters.** The role sees employees in this unit and every unit below it.

**Rules.** Required. Chosen from Organization Unit master data.

## ShowUnlocked {#SysUserOrgUnitData.ShowUnlocked}
<!-- alias: loc:ShowUnlocked, dom:SysUserOrgUnitData_ShowUnlocked -->

**What it is.** Whether unlocked records are shown.

**Why it matters.** Not read at present.

**Rules.** Yes or no.

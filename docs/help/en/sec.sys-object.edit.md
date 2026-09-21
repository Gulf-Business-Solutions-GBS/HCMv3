---
helpId: sec.sys-object.edit
lang: en
title: Sys Objects — Create / Edit
entity: HCMv3.SystemSecurity.SysObject
surfaces:
  - angular/src/app/admin/system-security/sys-objects/create-or-edit-sys-object-modal.component.html
audience: [admin]
permissions: [Pages.SystemSecurity.SysObjects.Create]
---

## Overview

The system's list of record types, and how sensitive each is to payroll and time. Marking a type *payroll sensitive* is what brings it under the payroll lock and retro tracking: while a payroll area is locked, its employees' records of that type cannot be changed, and a back-dated change sets the date payroll recalculates from.

## Name {#SysObject.Name}
<!-- alias: loc:Name, dom:SysObject_Name -->

**What it is.** The record type's name.

**Example.** EmployeeBasicPay

**Rules.** Required. Maximum 256 characters.

## FullName {#SysObject.FullName}
<!-- alias: loc:FullName, dom:SysObject_FullName -->

**What it is.** The record type's full class name.

**Example.** `HCMv3.PersonnelAdministration.EmployeeBasicPay`

**Why it matters.** This is how the system matches the row to records -- it must be exact.

**Rules.** Optional. Maximum 500 characters.

## IsPayrollSensitive {#SysObject.IsPayrollSensitive}
<!-- alias: loc:IsPayrollSensitive, dom:SysObject_IsPayrollSensitive -->

**What it is.** Whether changes to this type affect payroll.

**Why it matters.** Ticked, changes are refused while the employee's payroll area is locked, and a back-dated change is logged and makes payroll recalculate from that date.

**Rules.** Yes or no.

## RespectPermission {#SysObject.RespectPermission}
<!-- alias: loc:RespectPermission, dom:SysObject_RespectPermission -->

**What it is.** Whether permissions are checked for this type.

**Why it matters.** Not read at present.

**Rules.** Yes or no.

## MustRetroRun {#SysObject.MustRetroRun}
<!-- alias: loc:MustRetroRun, dom:SysObject_MustRetroRun -->

**What it is.** Whether back-dated changes of this type are always allowed.

**Why it matters.** Ticked, the payroll setting *Disable Retro Calculation* does not block this type.

**Rules.** Yes or no.

## IsTimeSensitive {#SysObject.IsTimeSensitive}
<!-- alias: loc:IsTimeSensitive, dom:SysObject_IsTimeSensitive -->

**What it is.** Whether changes to this type affect time evaluation.

**Why it matters.** Ticked, a back-dated change makes the time engine re-evaluate the employee from that date.

**Rules.** Yes or no.

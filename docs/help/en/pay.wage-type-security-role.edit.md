---
helpId: pay.wage-type-security-role.edit
lang: en
title: Wagetype Security Roles — Create / Edit
entity: HCMv3.PayrollManagement.WageTypeSecurityRole
surfaces:
  - angular/src/app/admin/payroll-management/wage-type-security-roles/create-or-edit-wage-type-security-role-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.WageTypeSecurityRoles.Create]
---

## Overview

The roles allowed to see a secured wagetype. Not enforced at present: nothing restricts access to a wagetype, whatever is listed here.

## WageTypeId {#WageTypeSecurityRole.WageTypeId}
<!-- alias: loc:WageTypeId, dom:WageTypeSecurityRole_WageTypeId -->

**What it is.** The secured wagetype.

**Example.** Executive Bonus

**Rules.** Required. Chosen from Wagetype master data.

## RoleId {#WageTypeSecurityRole.RoleId}
<!-- alias: loc:RoleId, dom:WageTypeSecurityRole_RoleId -->

**What it is.** A role allowed to see it.

**Example.** Payroll Manager

**Why it matters.** Each role can be listed once per wagetype.

**Rules.** Required. Chosen from Role master data.

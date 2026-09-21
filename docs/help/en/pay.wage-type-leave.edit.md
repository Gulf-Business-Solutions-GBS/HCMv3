---
helpId: pay.wage-type-leave.edit
lang: en
title: Wagetype Leaves — Create / Edit
entity: HCMv3.PayrollManagement.WageTypeLeave
surfaces:
  - angular/src/app/admin/payroll-management/wage-type-leaves/create-or-edit-wage-type-leave-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.WageTypeLeaves.Create]
---

## Overview

Which wagetypes make up the pay for a type of leave. Leave pay, the value of accrued leave and the annual leave provision all add up the monthly amounts of the wagetypes listed here for that absence type.

## WageTypeId {#WageTypeLeave.WageTypeId}
<!-- alias: loc:WageTypeId, dom:WageTypeLeave_WageTypeId -->

**What it is.** A wagetype included in the pay for this leave.

**Example.** Housing Allowance

**Rules.** Required. Chosen from Wagetype master data.

## AbsenceTypeId {#WageTypeLeave.AbsenceTypeId}
<!-- alias: loc:AbsenceTypeId, dom:WageTypeLeave_AbsenceTypeId -->

**What it is.** The type of leave.

**Example.** Annual Leave

**Why it matters.** Each wagetype can be listed only once for an absence type.

**Rules.** Required. Chosen from Absence Type master data.

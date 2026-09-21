---
helpId: pay.employee-sub-group-absence-type.edit
lang: en
title: Employee Sub Group Absence Types — Create / Edit
entity: HCMv3.PayrollManagement.EmployeeSubGroupAbsenceType
surfaces:
  - angular/src/app/admin/payroll-management/employee-sub-group-detail/absence-types/create-or-edit-employee-sub-group-absence-type-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.EmployeeSubGroupAbsenceTypes.Create]
---

## Overview

Absence types listed against an employee sub group. Not read at present: entitlements come from the sub group's leave rules, and nothing reads this list.

## AbsenceType {#EmployeeSubGroupAbsenceType.AbsenceTypeId}
<!-- alias: loc:AbsenceType, dom:EmployeeSubGroupAbsenceType_AbsenceTypeId -->

**What it is.** An absence type listed for the sub group.

**Example.** Hajj Leave

**Why it matters.** Each type can be listed once per sub group.

**Rules.** Optional. Chosen from Absence Type master data.

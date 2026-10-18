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

Leave types listed against a grade. Not read at present: entitlements come from the grade's leave rules, and nothing reads this list.

## AbsenceType {#EmployeeSubGroupAbsenceType.AbsenceTypeId}
<!-- alias: loc:AbsenceType, dom:EmployeeSubGroupAbsenceType_AbsenceTypeId -->

**What it is.** A leave type listed for the grade.

**Example.** Hajj Leave

**Why it matters.** Each type can be listed once per grade.

**Rules.** Optional. Chosen from Leave Type master data.

---
helpId: pay.absence-type-exception.edit
lang: en
title: Absence Type Exceptions — Create / Edit
entity: HCMv3.PayrollManagement.AbsenceTypeException
surfaces:
  - angular/src/app/admin/payroll-management/absence-type-exceptions/create-or-edit-absence-type-exception-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.AbsenceTypeExceptions.Create]
---

## Overview

Pairs of leave types that should not be combined. Not checked at present: the pairs are saved, but nothing stops the two types being taken together.

## AbsenceTypes {#AbsenceTypeException.AbsenceTypeId}
<!-- alias: loc:AbsenceTypes, dom:AbsenceTypeException_AbsenceTypeId -->

**What it is.** The leave type.

**Example.** Annual Leave

**Rules.** Required. Chosen from Leave Type master data.

## ExceptAbsenceType {#AbsenceTypeException.ExceptAbsenceTypeId}
<!-- alias: loc:ExceptAbsenceType, dom:AbsenceTypeException_ExceptAbsenceTypeId -->

**What it is.** The leave type it should not be combined with. A type cannot except itself.

**Example.** Sick Leave

**Rules.** Required. Chosen from Leave Type master data.

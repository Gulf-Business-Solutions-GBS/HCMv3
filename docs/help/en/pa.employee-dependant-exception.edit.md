---
helpId: pa.employee-dependant-exception.edit
lang: en
title: Employee Dependant Exceptions — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeDependantException
surfaces:
  - angular/src/app/admin/personnel-administration/employee-dependant-exceptions/create-or-edit-employee-dependant-exception-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeDependantExceptions.Create]
---

## Overview

Overrides one dependant's ticket or health reimbursement entitlement for a period. The period must fall within the dependant's own dates.

For health reimbursement, the amount replaces that dependant's share. For tickets, the time engine uses it when accruing the dependant's ticket.

## EmployeeDependantId {#EmployeeDependantException.EmployeeDependantId}
<!-- alias: loc:EmployeeDependantId, dom:EmployeeDependantException_EmployeeDependantId -->

**What it is.** The dependant the exception applies to.

**Example.** Sara Al-Mutairi (Daughter)

**Rules.** Required. Chosen from a list.

## ExceptionBenefitType {#EmployeeDependantException.ExceptionBenefitType}
<!-- alias: loc:ExceptionBenefitType, dom:EmployeeDependantException_ExceptionBenefitType -->

**What it is.** Which benefit is overridden.

**Example.** Ticket

**Rules.** Required. One of: Ticket, Health Reimbursement.

## Amount {#EmployeeDependantException.Amount}
<!-- alias: loc:Amount, dom:EmployeeDependantException_Amount -->

**What it is.** The entitlement amount that applies instead.

**Example.** 150.000

**Rules.** Optional. Number.

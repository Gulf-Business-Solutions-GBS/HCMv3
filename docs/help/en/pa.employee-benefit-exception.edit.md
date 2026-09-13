---
helpId: pa.employee-benefit-exception.edit
lang: en
title: Employee Benefit Exceptions — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeBenefitException
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/benefit-exceptions/create-or-edit-employee-benefit-exception-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeBenefitExceptions.Create]
---

## Overview

Overrides the employee's own ticket or health reimbursement entitlement for a period.

For health reimbursement, the amount replaces the employee's share from their sub group's entitlement. For tickets, the time engine uses the amount when accruing the employee's own ticket; an amount of zero means not entitled.

## ExceptionBenefitType {#EmployeeBenefitException.ExceptionBenefitType}
<!-- alias: loc:ExceptionBenefitType, dom:EmployeeBenefitException_ExceptionBenefitType -->

**What it is.** Which benefit is overridden.

**Example.** Health Reimbursement

**Rules.** Required. One of: Ticket, Health Reimbursement.

## Amount {#EmployeeBenefitException.Amount}
<!-- alias: loc:Amount, dom:EmployeeBenefitException_Amount -->

**What it is.** The entitlement amount that applies instead.

**Example.** 300.000

**Why it matters.** For tickets, zero means the employee is not entitled during the period.

**Rules.** Optional. Number.

---
helpId: pa.employee-fund-exemption.edit
lang: en
title: Employee Fund Exemptions — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeFundExemption
surfaces:
  - angular/src/app/admin/personnel-administration/employee-fund-exemptions/create-or-edit-employee-fund-exemption-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeFundExemptions.Create]
---

## Overview

A period during which payroll does not deduct the fund contribution.

Payroll skips the deduction for any payroll whose end date falls inside an exemption period.

## EmployeeFundId {#EmployeeFundExemption.EmployeeFundId}
<!-- alias: loc:EmployeeFundId, dom:EmployeeFundExemption_EmployeeFundId -->

**What it is.** The fund account the exemption applies to.

**Rules.** Required. Chosen from a list.

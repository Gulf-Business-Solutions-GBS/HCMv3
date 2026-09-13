---
helpId: pa.employee-loan-exemption.edit
lang: en
title: Employee Loan Exemptions — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeLoanExemption
surfaces:
  - angular/src/app/admin/personnel-administration/employee-loan-exemptions/create-or-edit-employee-loan-exemption-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeLoanExemptions.Create]
---

## Overview

Skips one month's instalment on one of the employee's loans. Payroll does not recover the loan in a month that has an exemption.

The exemption date must fall within the loan's recovery period, and a loan can have only one exemption per month. Exemptions can be requested through self-service and routed for approval.

## EmployeeLoanId {#EmployeeLoanExemption.EmployeeLoanId}
<!-- alias: loc:EmployeeLoanId, dom:EmployeeLoanExemption_EmployeeLoanId -->

**What it is.** The loan whose instalment is skipped.

**Rules.** Required. Chosen from a list.

## ExemptionDate {#EmployeeLoanExemption.ExemptionDate}
<!-- alias: loc:ExemptionDate, dom:EmployeeLoanExemption_ExemptionDate -->

**What it is.** Any date in the month whose instalment is skipped.

**Example.** 15 Dec 2026

**Why it matters.** Must be between the loan's recovery start date and end date.

**Rules.** Required. Date value.

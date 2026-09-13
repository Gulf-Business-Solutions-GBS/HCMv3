---
helpId: pa.employee-health-reimburse.edit
lang: en
title: Employee Health Reimburses — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeHealthReimburse
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/health-reimburses/create-or-edit-employee-health-reimburse-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeHealthReimburses.Create]
---

## Overview

A health reimbursement claim for the employee. Payroll pays claims whose payment date falls in the month it processes.

Every save checks the claim against the employee's entitlement for the date, whether it is entered here or requested through self-service: a claim is refused if the employee has no entitlement, if the amount is zero, or if another claim already exists for the same date. When the claim type is routed for approval, a claim entered here also waits for approval.

## PaymentDate {#EmployeeHealthReimburse.PaymentDate}
<!-- alias: loc:PaymentDate, dom:EmployeeHealthReimburse_PaymentDate -->

**What it is.** The date the expense was paid.

**Example.** 5 Sep 2026

**Why it matters.** Only one claim per date. Payroll pays the claim in the month of this date.

**Rules.** Required. Date value.

## Amount {#EmployeeHealthReimburse.Amount}
<!-- alias: loc:Amount, dom:EmployeeHealthReimburse_Amount -->

**What it is.** The amount claimed.

**Example.** 45.500

**Why it matters.** Cannot be zero, and is checked against the employee's remaining entitlement.

**Rules.** Required. Number.

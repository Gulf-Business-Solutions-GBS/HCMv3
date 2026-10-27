---
helpId: pa.employee-loan-installment.edit
lang: en
title: Employee Loan Installments — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeLoanInstallment
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/loans/create-or-edit-employee-loan-installment-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeLoans.Edit]
---

## Overview

One row of a loan's installment schedule: the amount payroll recovers each month from a given month onward. Open it from **Installments, Recoveries & Exemptions** in the loan's row menu on the employee's Loans section; the loan is already filled in.

Each row runs until the day before the next row's month, and the last row runs on with no end, so the list reads as a history of the installment: to change the installment from a month, add a row for that month rather than editing the one before it. Payroll uses the row in force in the payroll month. A loan with no rows is recovered at its Fixed Amount, as loans recorded before installments existed are.

A loan gets its first row automatically when it comes into force with a Fixed Amount: the Fixed Amount from the month recovery starts. When an employee rejoins with a new code and brings a loan balance over, the merged loan's rows are written automatically too, with the remark "Carried forward on employee rejoin".

When **Auto Update Loan Recovery End Date** is on in Payroll Settings, adding, changing or deleting a row recalculates the loan's recovery end date: month by month from the recovery start, skipping exempted months, until the due amount is recovered. A change also tells the next payroll run to recalculate the employee from that month.

## StartDate {#EmployeeLoanInstallment.StartDate}
<!-- alias: loc:EffectiveDate, dom:EmployeeLoanInstallment_StartDate -->

**What it is.** The month from which this amount is recovered.

**Example.** Jan 2027

**Why it matters.** The amount applies from the first day of this month until the next row's month starts. It cannot be before the month of the loan's recovery start date, and a loan can have only one row per month.

**Rules.** Required. A month.

## InstallmentAmount {#EmployeeLoanInstallment.Amount}
<!-- alias: loc:InstallmentAmount, dom:EmployeeLoanInstallment_Amount -->

**What it is.** The amount payroll deducts each month while this row is in force.

**Example.** 150.000

**Why it matters.** Payroll never deducts more than the balance still due, so the last deduction can be smaller.

**Rules.** Required. Number.

## Remark {#EmployeeLoanInstallment.Remark}
<!-- alias: loc:Remark, dom:EmployeeLoanInstallment_Remark -->

**What it is.** A note on why the installment changed.

**Example.** Reduced at the employee's request from January

**Why it matters.** Shown in the loan's installment list; not read by payroll.

**Rules.** Optional.

---
helpId: pa.loan-installment.edit
lang: en
title: Loan Installments — Create / Edit
entity: HCMv3.PersonnelAdministration.LoanInstallment
surfaces:
  - angular/src/app/admin/personnel-administration/loan-installments/create-or-edit-loan-installment-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.LoanInstallments.Create]
---

## Overview

A planned instalment in a loan's schedule.

It is recorded for reference; payroll recovers loans from the loan's instalment amount and does not read this schedule at present.

## EmployeeLoanId {#LoanInstallment.EmployeeLoanId}
<!-- alias: loc:EmployeeLoanId, dom:LoanInstallment_EmployeeLoanId -->

**What it is.** The loan this instalment belongs to.

**Rules.** Required. Chosen from a list.

## InstallmentDate {#LoanInstallment.InstallmentDate}
<!-- alias: loc:InstallmentDate, dom:LoanInstallment_InstallmentDate -->

**What it is.** The date the instalment is due.

**Example.** 31 Oct 2026

**Rules.** Required. Date value.

## InstallmentAmount {#LoanInstallment.InstallmentAmount}
<!-- alias: loc:InstallmentAmount, dom:LoanInstallment_InstallmentAmount -->

**What it is.** The instalment amount.

**Example.** 100.000

**Rules.** Optional. Number.

## InstallmentStatus {#LoanInstallment.InstallmentStatus}
<!-- alias: loc:InstallmentStatus, dom:LoanInstallment_InstallmentStatus -->

**What it is.** Whether the instalment has been paid.

**Example.** Unpaid

**Rules.** Required. One of: Unpaid, Paid.

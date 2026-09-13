---
helpId: pa.employee-loan-recovery.edit
lang: en
title: Employee Loan Recoveries — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeLoanRecovery
surfaces:
  - angular/src/app/admin/personnel-administration/employee-loan-recoveries/create-or-edit-employee-loan-recovery-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeLoanRecoveries.Create]
---

## Overview

A repayment against one of the employee's loans. Payroll records its own instalments here as Recovery Through Payroll.

A recovery cannot be dated before the loan's recovery start date, and recoveries cannot add up to more than the loan's due amount. Saving a recovery on this form does not update the loan's balance due; payroll updates the balance when it recovers.

## EmployeeLoanId {#EmployeeLoanRecovery.EmployeeLoanId}
<!-- alias: loc:EmployeeLoanId, dom:EmployeeLoanRecovery_EmployeeLoanId -->

**What it is.** The loan being repaid.

**Rules.** Required. Chosen from a list.

## RecoveryDate {#EmployeeLoanRecovery.RecoveryDate}
<!-- alias: loc:RecoveryDate, dom:EmployeeLoanRecovery_RecoveryDate -->

**What it is.** The date of the repayment.

**Example.** 31 Oct 2026

**Why it matters.** Cannot be before the loan's recovery start date.

**Rules.** Required. Date value.

## RecoveryAmount {#EmployeeLoanRecovery.RecoveryAmount}
<!-- alias: loc:RecoveryAmount, dom:EmployeeLoanRecovery_RecoveryAmount -->

**What it is.** The amount repaid.

**Example.** 100.000

**Why it matters.** All recoveries on the loan together cannot exceed its due amount.

**Rules.** Optional. Number.

## LoanRecoveryMethod {#EmployeeLoanRecovery.LoanRecoveryMethod}
<!-- alias: loc:LoanRecoveryMethod, dom:EmployeeLoanRecovery_LoanRecoveryMethod -->

**What it is.** How the repayment was made.

**Example.** Recovery External

**Why it matters.** Payroll recognises its own rows as Recovery Through Payroll and replaces them when it recalculates the month.

**Rules.** Required. One of: Recovery Through Payroll, Recovery External, Recovery Through End Of Service, Recovery Exemption, Additional Recovery Through Payroll.

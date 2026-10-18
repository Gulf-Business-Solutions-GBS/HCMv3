---
helpId: pa.employee-loan.edit
lang: en
title: Employee Loans — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeLoan
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/loans/create-or-edit-employee-loan-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeLoans.Create]
---

## Overview

A loan to the employee and how it is recovered. Payroll recovers it from the recovery start date according to the loan type; each instalment it takes is recorded as a loan recovery and reduces the balance due.

The actual amount cannot be less than the amount due, and neither the recovery start date nor the end date can be before the start date.

## LoanTypeId {#EmployeeLoan.LoanTypeId}
<!-- alias: loc:LoanTypeId, dom:EmployeeLoan_LoanTypeId -->

**What it is.** The kind of loan.

**Example.** Personal Loan

**Why it matters.** Its deduction type and recovery option decide how payroll recovers the loan.

**Rules.** Required. Chosen from Loan Type master data.

:::admin
Maintained under General Master Info → Loan Types. A loan type set to Variable deduction makes payroll stop with an error for the employee.
:::

## RecoveryStartDate {#EmployeeLoan.RecoveryStartDate}
<!-- alias: loc:RecoveryStartDate, dom:EmployeeLoan_RecoveryStartDate -->

**What it is.** The date payroll starts recovering the loan.

**Example.** 1 Oct 2026

**Why it matters.** Payroll recovers from the first payroll ending on or after this date. Exemptions must fall between this date and the loan's end date.

**Rules.** Required. Date value.

## ActualAmount {#EmployeeLoan.ActualAmount}
<!-- alias: loc:ActualAmount, dom:EmployeeLoan_ActualAmount -->

**What it is.** The amount the employee receives.

**Example.** 1200.000

**Why it matters.** Cannot be less than the amount due.

**Rules.** Optional. Number.

## DueAmount {#EmployeeLoan.DueAmount}
<!-- alias: loc:DueAmount, dom:EmployeeLoan_DueAmount -->

**What it is.** The amount to be recovered.

**Example.** 1200.000

**Why it matters.** The balance due starts at this amount when the loan is created. When the loan is paid through payroll, this is the amount paid out.

**Rules.** Optional. Number.

## FixedAmount {#EmployeeLoan.FixedAmount}
<!-- alias: loc:FixedAmount, dom:EmployeeLoan_FixedAmount -->

**What it is.** The instalment payroll deducts each month.

**Example.** 100.000

**Why it matters.** Payroll never deducts more than the balance still due, so the last instalment can be smaller.

**Rules.** Optional. Number.

## RecoverFracInLastTrans {#EmployeeLoan.RecoverFracInLastTrans}
<!-- alias: loc:RecoverFracInLastTrans, dom:EmployeeLoan_RecoverFracInLastTrans -->

**What it is.** Whether a small remainder is added to the last instalment.

**Why it matters.** When no later recovery is scheduled and the balance left after an instalment is smaller than the instalment, payroll adds it so the loan clears.

**Rules.** Yes or no.

## IncludeInLoanPay {#EmployeeLoan.IncludeInLoanPay}
<!-- alias: loc:IncludeLoanPaymentInPayroll, dom:EmployeeLoan_IncludeInLoanPay -->

**What it is.** Whether the loan is paid to the employee through payroll.

**Why it matters.** When ticked, payroll pays the due amount in the payroll month in which the loan starts.

**Rules.** Yes or no.

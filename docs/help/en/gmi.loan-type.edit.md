---
helpId: gmi.loan-type.edit
lang: en
title: Loan Types — Create / Edit
entity: HCMv3.GeneralMasterInfo.LoanType
surfaces:
  - angular/src/app/admin/general-master-info/loan-types/create-or-edit-loan-type-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.LoanTypes.Create]
---

## Overview

The kinds of loan employees can take. Every employee loan names a loan type, and payroll recovers instalments according to the type's deduction type and recovery option.

The eligibility settings are stored but not enforced at present: a loan is not refused for going beyond them.

## LoanDeductionType {#LoanType.LoanDeductionType}
<!-- alias: loc:LoanDeductionType, dom:LoanType_LoanDeductionType -->

**What it is.** How each instalment is worked out.

**Why it matters.** With Fixed, payroll deducts the instalment amount set on the employee's loan, and never more than the balance still owed.

**Rules.** Required. One of: Fixed, Variable.

:::admin
Do not use Variable yet. The payroll calculation does not support it, and stops with an error for any employee with a loan of a Variable type rather than deducting nothing.
:::

## RecoveryOption {#LoanType.RecoveryOption}
<!-- alias: loc:RecoveryOption, dom:LoanType_RecoveryOption -->

**What it is.** How the loan is repaid.

**Why it matters.** Payroll recovers loans whose type is Through Payroll or All. With Through External, payroll does not deduct it and repayment is handled outside HCMv3.

**Rules.** Required. One of: All, Through Payroll, Through External.

## EligiblePeriod {#LoanType.EligiblePeriod}
<!-- alias: loc:EligiblePeriod, dom:LoanType_EligiblePeriod -->

**What it is.** The period of service an employee needs before becoming eligible for this loan.

**Why it matters.** Not enforced at present.

**Rules.** Required. Whole number.

## EligibleCountPerYear {#LoanType.EligibleCountPerYear}
<!-- alias: loc:EligibleCountPerYear, dom:LoanType_EligibleCountPerYear -->

**What it is.** How many loans of this type an employee may take in a year.

**Why it matters.** Not enforced at present.

**Rules.** Required. Whole number.

## MaxInstallments {#LoanType.MaxInstallments}
<!-- alias: loc:MaxInstallments, dom:LoanType_MaxInstallments -->

**What it is.** The most instalments a loan of this type may be repaid over.

**Why it matters.** Not enforced at present.

**Rules.** Required. Whole number.

## EligibleAmount {#LoanType.EligibleAmount}
<!-- alias: loc:EligibleAmount, dom:LoanType_EligibleAmount -->

**What it is.** What the eligible amount is based on: a default amount, or the employee's basic salary.

**Why it matters.** Not enforced at present.

**Rules.** Required. One of: Default, Basic Salary.

## EligibleAmountNumber {#LoanType.EligibleAmountNumber}
<!-- alias: loc:EligibleAmountNumber, dom:LoanType_EligibleAmountNumber -->

**What it is.** The figure that goes with the basis above.

**Why it matters.** Not enforced at present.

**Rules.** Required. Number.

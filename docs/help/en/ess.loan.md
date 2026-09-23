---
helpId: ess.loan
lang: en
title: My Loans
surfaces:
  - angular/src/app/main/ess/my-loan/my-loan.component.html
audience: [end-user]
permissions: [Ess.MyLoans]
---

## Overview

Ask for a loan or an advance your company offers, and see the ones you already have. You say how much, over what period, and when repayment should start.

An approved loan is paid to you and then recovered from your pay automatically, instalment by instalment, until it is cleared. If you leave before it is, the balance is settled against your final payment.

## Loan type {#ess.loan.loan-type}
<!-- alias: loc:LoanType -->

**What it is.** Which of your company's loan schemes you are asking for — a salary advance, a housing loan, an emergency loan, and so on.

**Example.** Salary Advance.

**Why it matters.** The type carries the rules: the most you may borrow, how long you have to repay it, whether interest applies and whether you may hold more than one at a time.

## Loan amount {#ess.loan.loan-amount}
<!-- alias: loc:LoanAmount -->

**What it is.** How much you are asking for, in your payroll currency. It must be more than zero.

**Example.** 1,500.000

**Why it matters.** This is the figure the instalments are worked back from, and the one your approvers judge against the scheme's limit and against what you are already repaying.

## Instalment {#ess.loan.instalment}
<!-- alias: loc:Instalment -->

**What it is.** How much comes out of each month's pay until the loan is cleared.

**Example.** 125.000 a month, clearing 1,500.000 over a year.

**Why it matters.** This is what you actually feel. A high instalment clears the loan quickly and takes a large bite each month; a low one does the opposite and runs past dates you may care about, such as the end of your contract.

## Start date {#ess.loan.start-date}
<!-- alias: loc:StartDate -->

**What it is.** When you want the money.

**Example.** 1 August 2026.

**Why it matters.** Payroll pays an approved loan in the run that covers this date, so a date inside a period that has already been processed pushes the payment to the next one.

## Recovery start date {#ess.loan.recovery-start-date}
<!-- alias: loc:RecoveryStartDate -->

**What it is.** When the instalments begin coming out of your pay. It does not have to be the month you are paid the loan.

**Example.** Paid on 1 August, first instalment in September.

**Why it matters.** It is how you get a month's breathing space between receiving a loan and starting to repay it — but it also moves the end of the loan back by the same amount.

## End date {#ess.loan.end-date}
<!-- alias: loc:EndDate -->

**What it is.** When the last instalment is due.

**Example.** 31 July 2027.

**Why it matters.** The amount, the instalment and this date have to agree — together they say how the loan is repaid, and a date that does not leave room for the instalments to clear the balance is the usual reason a request is sent back.

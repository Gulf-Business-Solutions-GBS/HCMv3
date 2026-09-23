---
helpId: ess.loan-exemption
lang: en
title: My Loan Exemptions
surfaces:
  - angular/src/app/main/ess/my-loan-exemption/my-loan-exemption.component.html
audience: [end-user]
permissions: [Ess.MyLoanExemptions]
---

## Overview

Ask to skip one month's loan instalment — a month where the deduction would hurt, and you would rather pay it later.

An exemption does not reduce what you owe. The month is skipped and the loan runs one month longer, so the balance is the same and the end date moves. Only loans that are still being repaid can be exempted, which is why the list can be empty.

## Loan {#ess.loan-exemption.loan}
<!-- alias: loc:Loan -->

**What it is.** Which of your loans to skip a month on. The list holds only loans still under recovery, with their type, balance, instalment and end date shown so you can tell them apart.

**Example.** Salary Advance — 750.000 outstanding, 125.000 a month, ending July 2027.

**Why it matters.** If you are repaying more than one loan, exempting the wrong one leaves the deduction you were trying to avoid still coming out. An empty list means nothing of yours is currently being recovered.

## Exemption month {#ess.loan-exemption.exemption-month}
<!-- alias: loc:ExemptionMonth -->

**What it is.** The month whose instalment you want to skip.

**Example.** August 2026, when school fees are due.

**Why it matters.** It has to be a month that has not been paid yet — once payroll has run, the deduction has happened and an exemption cannot reach back for it. Ask before the payroll cut-off for that month, not after.

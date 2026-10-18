---
helpId: ess.health-reimburse
lang: en
title: My Health Reimbursements
surfaces:
  - angular/src/app/main/ess/my-health-reimburse/my-health-reimburse.component.html
audience: [end-user]
permissions: [Ess.MyHealthReimburses]
---

## Overview

Claim back medical costs your company covers — a consultation, medicine, a dental visit — against the health allowance your grade gives you.

The panel at the top shows what you have to spend and what is left of it. If it says you have no entitlement, your grade has not been given one, and that is a question for HR rather than something to fix here.

## Payment date {#ess.health-reimburse.payment-date}
<!-- alias: loc:PaymentDate -->

**What it is.** The day you paid for the treatment.

**Example.** 6 April 2026.

**Why it matters.** It decides which entitlement period the claim comes out of — a bill paid in December and claimed in January still belongs to the year you paid it, which matters when a period is nearly used up.

## Amount {#ess.health-reimburse.amount}
<!-- alias: loc:Amount -->

**What it is.** What you paid, in your payroll currency.

**Example.** 45.000

**Why it matters.** Claim the amount on the receipt. Keep the receipt — a claim without one is the first thing an approver asks about, and where the claim is for more than your claimable balance it will be cut to what is left rather than refused outright.

## Your balance {#ess.health-reimburse.balance}
<!-- alias: loc:HealthReimburseBalance -->

**What it is.** Your health allowance for the current period, broken down: the **allowance** itself, anything **carried forward** from last period, the **total** of the two, what you have **used**, what is **pending** approval, what is **remaining**, and what is **claimable** right now.

**Example.** Allowance 500.000 + carried forward 120.000 = 620.000 total; used 200.000, pending 45.000, remaining 375.000.

**Why it matters.** **Claimable is the figure to go by, not remaining.** Remaining ignores claims still waiting for approval; claimable has already set them aside. Claiming against "remaining" is how people end up over their entitlement and have a claim cut back.

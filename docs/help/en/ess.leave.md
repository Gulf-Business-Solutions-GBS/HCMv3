---
helpId: ess.leave
lang: en
title: My Leave
surfaces:
  - angular/src/app/main/ess/my-leave/my-leave.component.html
audience: [end-user]
permissions: [Ess.MyLeave]
---

## Overview

Book leave, see what you have left, change leave already approved, and tell HR when you are back.

Your balances sit at the top, one card per leave type. Below them are the forms — applying, changing an approved leave, reporting your return — and then everything you have booked.

Leave you have asked for but that is not yet approved is already held against your balance. That is deliberate: it stops you booking the same days twice while the first request is with your manager.

## Leave type {#ess.leave.absence-type}
<!-- alias: loc:AbsenceType -->

**What it is.** Which kind of leave you are taking — annual, sick, unpaid, compassionate, maternity — from the types your company gives you.

**Example.** Annual Leave.

**Why it matters.** Each type has its own balance, its own rules about notice and documents, and its own effect on pay. Sick leave in particular is paid on a sliding scale rather than in full, which is what the deduction breakdown below explains.

## Start date {#ess.leave.start-date}
<!-- alias: loc:StartDate -->

**What it is.** Your first day off.

**Example.** 3 August 2026.

**Why it matters.** It is the first day counted, so a start date on a weekend or a public holiday usually means you meant the next working day. Some leave types also have a notice rule — a request made too close to the date is refused.

## End date {#ess.leave.end-date}
<!-- alias: loc:EndDate -->

**What it is.** Your last day off. You return the day after it.

**Example.** 14 August 2026, coming back on the 15th.

**Why it matters.** It must be on or after the start date, and the days between the two are what comes off your balance — how weekends and holidays count is your company's rule, and the **Days** figure shows the result.

## Reason for the change {#ess.leave.reason-for-the-change}
<!-- alias: loc:ReasonForTheChange -->

**What it is.** Why the approved leave needs changing. Required.

**Example.** "Client visit brought forward — coming back a week early."

**Why it matters.** You are asking somebody to revisit a decision they already made, and they will want to know why. It is required for that reason, and it stays on the record with the amendment.

## Rejoining date {#ess.leave.rejoining-date}
<!-- alias: loc:RejoiningDate -->

**What it is.** The day you actually came back to work.

**Example.** 15 August 2026 as planned; or the 11th, four days early.

**Why it matters.** It is what your attendance is counted from, and what decides whether days go back to your balance or extra ones come off. A date later than planned is a late return, and the days between are dealt with under your company's rule for that.

## Return reason {#ess.leave.return-reason}
<!-- alias: loc:ReturnReason -->

**What it is.** Why you came back on a different day from the one approved. Needed only when the return is early or late.

**Example.** "Flight cancelled, first seat available was Tuesday."

**Why it matters.** A return that is not on the approved day has to be explained to whoever approves it, and a late one may be treated as absence without it. Saying what happened is what keeps the record straight.

## Paid leave dates {#ess.leave.paid-leave-dates}
<!-- alias: loc:PaidLeaveDates -->

**What it is.** On a late return, which of the extra days are to be taken as paid leave — covered by a balance you still have.

**Example.** Returned four days late, two of them taken as annual leave.

**Why it matters.** It is how an overrun is settled honestly: days covered by a balance come off it, and the rest are dealt with as unpaid. Only days you actually have a balance for can go here.

## Unpaid leave dates {#ess.leave.unpaid-leave-dates}
<!-- alias: loc:UnpaidLeaveDates -->

**What it is.** On a late return, which of the extra days are unpaid.

**Example.** The other two of the four days.

**Why it matters.** These days are deducted from your pay. Splitting an overrun honestly between paid and unpaid here is what stops the whole of it being treated as unauthorised absence, which is the worse outcome.

## Payment type {#ess.leave.leave-payment-type-label}
<!-- alias: loc:LeavePaymentTypeLabel -->

**What it is.** Whether you want the leave paid as usual, or your leave salary paid in advance before you go.

**Example.** Paid in advance, for a month away.

**Why it matters.** Advance payment brings money forward, it does not add any: the advance is settled against the pay you would have had while away. It also needs more notice, because it has to reach a payroll run before you leave.

## Payment date {#ess.leave.payment-date-label}
<!-- alias: loc:PaymentDateLabel -->

**What it is.** When you want the advance paid, if you asked for one.

**Example.** 28 July 2026, the payroll before you fly.

**Why it matters.** It has to be a date a payroll run can still reach. Asking for an advance two days before you go, after the month's payroll has been processed, means it arrives after you have gone.

## Bookable balance {#ess.leave.bookable-balance}
<!-- alias: loc:BookableBalance -->

**What it is.** How many days you can still book, per leave type. **Remaining** is shown underneath it, and the two are different: remaining counts only leave already approved, while bookable also holds back what you have requested and not yet had approved.

**Example.** Remaining 18, bookable 13 — because you have a five-day request waiting.

**Why it matters.** **Book against bookable, not remaining.** The gap between them is leave you have already asked for; treating it as free is how a second request is rejected for a balance you thought you had.

## Sick leave deduction breakdown {#ess.leave.sick-breakdown}
<!-- alias: loc:SickLeaveDeductionBreakdown -->

**What it is.** For sick leave, how the days you asked for are paid: which are at full pay, which at part pay and which unpaid, worked out from your company's rule and the sick leave you have already taken this year.

**Example.** Days 1–15 at full pay, 16–25 at three quarters, 26–35 at half.

**Why it matters.** Sick leave is rarely paid in full throughout, and the steps depend on how much you have already had. This is shown before you submit precisely so that the deduction is not a surprise on the payslip.

## Days {#ess.leave.days}
<!-- alias: loc:Days -->

**What it is.** How many days the leave actually costs you, from your dates and your company's calendar.

**Example.** 3 to 14 August shows as 10 days, not 12, where the weekends do not count.

**Why it matters.** This, not the span of dates, is what comes off your balance. If it is not the number you expected, the difference is weekends or public holidays inside your dates — which is worth knowing before you book the flights.

## Report my return {#ess.leave.report-return}
<!-- alias: loc:ReportMyReturn -->

**What it is.** Opens the form for telling HR you are back, once your leave has started.

**Example.** Back on Sunday as planned; or back four days early.

**Why it matters.** Until your return is recorded, the system still has you away — which affects attendance, payroll and anybody looking for you. Reporting a return early gives the unused days back to your balance.

## Request cancellation {#ess.leave.request-cancellation}
<!-- alias: loc:RequestCancellation -->

**What it is.** Asks for approved leave to be cancelled outright, before it starts.

**Example.** Approved leave next month that you no longer want.

**Why it matters.** It is a request, not a cancellation: the leave stands until somebody approves it, and the days come back to your balance then, not now. Do not book anything over those days before you see it approved.

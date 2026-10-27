---
helpId: ess.overtime
lang: en
title: My Overtime
surfaces:
  - angular/src/app/main/ess/my-overtime/my-overtime.component.html
audience: [end-user]
permissions: [Ess.MyOvertime]
---

## Overview

Claim hours you worked beyond your normal duty. You say which day, which kind of overtime and how many hours; your manager approves the hours, and payroll pays what was approved.

This is a claim, not a record of attendance. The hours your punches show are what the Time Engine works out on its own — what you ask for here is what you want paid, and the two are compared when the claim is approved.

## Overtime type {#ess.overtime.overtime-type}
<!-- alias: loc:OvertimeType -->

**What it is.** Which kind of overtime this was — ordinary weekday hours, a rest day, a public holiday, night work — from the types your company has set up.

**Example.** Public Holiday Overtime.

**Why it matters.** The type carries the rate. The same hour is worth different money on a Tuesday evening and on Eid, so the type is what the payment is actually built on.

## Date {#ess.overtime.effective-date}
<!-- alias: loc:EffectiveDate -->

**What it is.** The day you worked the hours.

**Example.** 3 June 2026.

**Why it matters.** It decides which payroll period pays the claim, and it is what an approver checks the hours against — a claim for a day you were on leave is one they will ask about.

## Hours {#ess.overtime.hours}
<!-- alias: loc:Hours -->

**What it is.** How many hours you are claiming for that day. It must be more than zero, and it may be a fraction.

**Example.** 2.5

**Why it matters.** This is what you asked for, and it is kept even after approval, so the record shows both the claim and what was allowed.

## Approved hours {#ess.overtime.approved-hours}
<!-- alias: loc:ApprovedHours -->

**What it is.** How many hours were actually approved. Blank until somebody approves the claim, and it can be fewer than you asked for.

**Example.** You claimed 2.5 and 2 were approved.

**Why it matters.** **This is the figure you are paid.** If it differs from what you claimed, the reason is in the workflow history — the approver's comment is where it will be, and asking them is better than claiming the difference again.

---
helpId: ess.passport-withdrawal
lang: en
title: My Passport Withdrawals
surfaces:
  - angular/src/app/main/ess/my-passport-withdrawal/my-passport-withdrawal.component.html
audience: [end-user]
permissions: [Ess.MyPassportWithdrawals]
---

## Overview

Ask for a document your company holds for you — normally your passport — to be taken out for a while, and record it when you bring it back.

Two halves. You ask to withdraw, saying when you need it and when you will return it; and when you do return it you record that here, which is what puts the document back in your company's custody and closes the request. A document you never record as returned stays out against your name.

## Document {#ess.passport-withdrawal.document}
<!-- alias: loc:Document -->

**What it is.** Which held document you want out. The list holds only documents your company is holding for you and that can be withdrawn.

**Example.** Passport A12345678.

**Why it matters.** An empty list means either nothing of yours is held, or the one you want is already out — which is the other half of this screen, not a reason to raise a second request.

## Withdraw date {#ess.passport-withdrawal.withdraw-date}
<!-- alias: loc:WithdrawDate -->

**What it is.** When you need the document from.

**Example.** 18 September 2026, the day before you travel.

**Why it matters.** It is what whoever holds the documents works to, and they need notice. Asking for a passport on the morning you fly is how people miss flights.

## Expected return date {#ess.passport-withdrawal.return-date}
<!-- alias: loc:ReturnDate -->

**What it is.** When you expect to bring it back.

**Example.** 28 September 2026.

**Why it matters.** It is a promise, not a limit — nothing happens automatically on that date. What it does is tell your company when to expect the document back, and it is what a chase is based on when it does not appear.

## Reason {#ess.passport-withdrawal.withdrawal-reason}
<!-- alias: loc:WithdrawalReason -->

**What it is.** Why you need it, in your own words.

**Example.** Travel to Egypt for family leave; visa application at the embassy.

**Why it matters.** It is what your approver reads. A passport is the document a company is most careful with, so a clear reason is what makes a request straightforward rather than a conversation.

## Guarantor {#ess.passport-withdrawal.guarantor}
<!-- alias: loc:Guarantor -->

**What it is.** A colleague who vouches for the document coming back. Required by some companies, offered by others.

**Example.** Your line manager, or a senior colleague in your department.

**Why it matters.** Your guarantor is told when the request is approved, so pick somebody who knows you are asking. It is a real undertaking on their part, not a formality.

## Actual return date {#ess.passport-withdrawal.actual-return-date}
<!-- alias: loc:ActualReturnDate -->

**What it is.** The day you actually handed the document back. Recorded when you return it, not when you ask.

**Example.** 27 September 2026 — a day early.

**Why it matters.** This is what closes the request and puts the document back in custody. Until it is recorded the document is out against your name, whatever the expected date said, and it will show on your clearance at the end of your employment.

## Return my document {#ess.passport-withdrawal.return}
<!-- alias: loc:ReturnMyDocument -->

**What it is.** Opens the short form for recording the document coming back. It appears on a request whose document is still out.

**Example.** You are back from leave and handing your passport in at the HR desk.

**Why it matters.** Handing the passport over in person is not what closes the request — recording it is. Do both, on the same day, or the record and the safe disagree.

## Still out {#ess.passport-withdrawal.still-out}
<!-- alias: loc:StillOut -->

**What it is.** A mark on a request whose document has not been recorded as returned.

**Example.** Withdrawn three weeks ago, expected back a fortnight ago, still out.

**Why it matters.** It is the line that matters to your company's auditors, and the one that appears on your clearance. If it says "still out" and the document is in your desk drawer, the record is wrong and only recording the return will fix it.

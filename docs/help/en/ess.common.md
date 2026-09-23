---
helpId: ess.common
lang: en
title: Self-service — shared topics
audience: [end-user]
permissions: []
---

## Overview

Not a screen. This file holds the topics that every self-service request screen shows — the request list, the
status badge, the withdraw button and the rest — written once here and borrowed by each screen, so the same
thing is never explained two different ways. See DESIGN.md §5.1 and §5.4.

## Making a request {#ess.common.new-request}

**What it is.** The button at the top right of the screen that opens the form. Filling it in and pressing **Save** submits the request; nothing is sent until you do.

**Example.** **Request Leave**, **Request Ticket**, **Request Loan** — the wording follows the screen.

**Why it matters.** Once submitted the request goes to whoever approves that kind of request in your company, and you cannot edit it in place. To change something, withdraw it and make a new one, or use **Amend** where the screen offers it.

## My requests {#ess.common.my-requests}

**What it is.** Everything you have asked for on this screen, newest first, with where each one has got to.

**Example.** Three ticket requests: one approved last month, one waiting, one you withdrew.

**Why it matters.** This is the record. A request that is not in this list was never submitted — if you filled the form in and did not press **Save**, nothing was sent.

## Status {#ess.common.status}

**What it is.** Where a request has got to. **Draft** is not submitted, **Pending** is waiting for somebody, **Approved** has been agreed and acted on, **Rejected** was turned down, and **Withdrawn** is one you took back yourself.

**Example.** Pending — sitting with your line manager.

**Why it matters.** Only an approved request changes anything: your balance, your pay or your record. A pending one has done nothing yet, whatever the form said when you filled it in.

## Workflow history {#ess.common.workflow-history}

**What it is.** Who a request has reached, what each person did with it and when, including any comment they left.

**Example.** Submitted 09:12 → Line manager approved 11:40 ("cover arranged") → with HR.

**Why it matters.** It answers "who is it with?" without asking anybody. If a request has sat with one person for days, this tells you which person, which is who to chase.

## Withdraw {#ess.common.withdraw}

**What it is.** Takes a request back. Offered only while it can still be taken back — once it is approved and acted on, the button is gone.

**Example.** You asked for leave next week, your plans changed, you withdraw it before your manager gets to it.

**Why it matters.** A withdrawn request is not deleted: it stays in the list marked **Withdrawn**, so the record shows what happened. Withdrawing frees anything the request was holding, such as a leave balance reserved against it.

## You have no employee record {#ess.common.not-linked}

**What it is.** A warning that the account you signed in with is not attached to an employee, so there is nobody for a request to be about.

**Example.** A newly created user, or an administrator account that is deliberately not an employee.

**Why it matters.** Nothing on the self-service screens works until HR links the account to your employee record, and only HR can do it. Administrator accounts are often left unlinked on purpose.

## Comments {#ess.common.comments}

**What it is.** Anything you want the people approving this to read. Free text, and optional.

**Example.** "Flying on the 14th, back on the 28th — cover agreed with Ahmed."

**Why it matters.** It is the only place you can explain a request that needs explaining, and it stays with the request for good: approvers see it, and so does anyone who looks the request up afterwards. A request that surprises somebody is a request that comes back to you.

## Amend {#ess.common.amend}

**What it is.** Changes a request that has already been approved, by raising a second request linked to the first. Offered only on the screens where a change is a real thing to ask for.

**Example.** Your approved leave was ten days and you now need eight.

**Why it matters.** An amendment goes through approval like any other request, so nothing changes until it is approved too. The original stays in the list — the pair is the record of what was asked for and what it became.

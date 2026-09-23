---
helpId: ess.inbox
lang: en
title: My Inbox
surfaces:
  - angular/src/app/main/ess/inbox/ess-inbox.component.html
audience: [end-user]
permissions: [Ess.Inbox]
---

## Overview

Everything waiting on you: requests from your team, and anything a colleague has delegated to you while they are away.

Pick a request on the left and its detail opens on the right, with what is being asked for and how it got to you. Nothing leaves your inbox until you act on it, and nothing you do here can be undone from here — a decision goes straight to the requester.

If your inbox is empty, nothing is waiting on you. It is not a sign that something is broken.

## Step {#ess.inbox.step}
<!-- alias: loc:Step -->

**What it is.** Where you are in the chain of approvals — "Step 2 / 3" means two more people after you, and you are the second.

**Example.** Step 1 / 3 — you are the first of three.

**Why it matters.** It changes what your approval means. Approving at step 1 of 3 passes the request on; approving at the last step is what actually makes it happen. It only appears on requests with more than one step.

## Comment {#ess.inbox.comment}
<!-- alias: loc:Comment -->

**What it is.** What you want to say about your decision. Optional on an approval; expected on a rejection or a send-back.

**Example.** "Approved — cover arranged with Fatima."; "Sending back: please attach the medical report."

**Why it matters.** It is what the requester sees, and it is kept in the workflow history for good. A rejection without a comment is the commonest reason somebody raises the same request again unchanged — say what is wrong and you will not see it twice.

## Approve {#ess.inbox.approve}
<!-- alias: loc:Approve -->

**What it is.** Agrees to the request and moves it on: to the next approver, or into effect if you are the last.

**Example.** Approving a leave request that your team can cover.

**Why it matters.** Approving at the final step is what changes the record — a balance comes down, a payment is scheduled, an asset moves. Check the step before you press it, and take the extra look when it says you are last.

## Reject {#ess.inbox.reject}
<!-- alias: loc:Reject -->

**What it is.** Turns the request down. It ends there: the requester has to raise a new one.

**Example.** Leave over a month-end close, which cannot be covered.

**Why it matters.** Rejection is final for that request, so where you only want something corrected, **Send back** is the kinder and faster option. Rejecting also does not tell the requester why on its own — the comment does.

## Send back {#ess.inbox.send-back}
<!-- alias: loc:SendBack -->

**What it is.** Returns the request to the requester to change and resubmit, instead of refusing it.

**Example.** A ticket claim with the wrong date; a document request missing its scan.

**Why it matters.** It keeps the request alive with its history, so nobody starts again from nothing. It is offered only where the step's configuration allows it — when the button is not there, the choice is approve or reject.

---
helpId: ess.personal-data
lang: en
title: My Personal Data
surfaces:
  - angular/src/app/main/ess/my-personal-data/my-personal-data.component.html
audience: [end-user]
permissions: [Ess.MyProfile]
---

## Overview

Ask for your own details to be corrected — a marital status, a phone number, an address, a name spelling.

You cannot edit your record directly; you propose changes and HR approves them. That is deliberate: what is on your record drives your pay, your allowances and what your company files with the government, so a change is something somebody agrees to rather than something you can simply do.

## Pick the fields to change {#ess.personal-data.pick-fields}
<!-- alias: loc:PickTheFieldsYouWantToChangeHint -->

**What it is.** One request can carry several changes. Tick the fields you want to change, and only those appear for you to fill in.

**Example.** Ticking Marital Status and Mobile Number, leaving the other twenty fields alone.

**Why it matters.** Untouched fields are not part of the request at all, so nothing you did not tick can be changed by accident. It also keeps the request readable for whoever approves it — three changes they can see are easier to agree to than a whole record to compare.

## Current {#ess.personal-data.current}
<!-- alias: loc:Current -->

**What it is.** What your record says today, shown beside each field you have ticked.

**Example.** Marital Status: Single.

**Why it matters.** It is what the change is measured against, and it catches the common case where the record is already right and the request is not needed. Read it before you type the new value.

## New value {#ess.personal-data.new-value}
<!-- alias: loc:NewValue -->

**What it is.** What it should say instead.

**Example.** Marital Status: Married.

**Why it matters.** This is what is written to your record once the request is approved — so write it as it should appear, not as a note about it. For a name or a document detail, copy it from the official document rather than typing it from memory.

## Changes {#ess.personal-data.changes}
<!-- alias: loc:Changes -->

**What it is.** On a submitted request, the list of what it is asking to change: each field, from what, to what.

**Example.** Marital Status: Single → Married; Mobile: 9XXXXXXX → 6XXXXXXX.

**Why it matters.** It is the whole request in one line, and it is what your approver sees. It is also how you check that what you submitted is what you meant, without opening the form again.

## Attachments {#ess.personal-data.attachments}
<!-- alias: loc:Attachments -->

**What it is.** Documents backing the change, attached to the request.

**Example.** A marriage certificate for a change of marital status; a tenancy contract for a new address.

**Why it matters.** Most changes to an official detail need evidence, and a request without it waits while somebody asks you for it. Attaching the document when you raise the request is usually the difference between approval the same day and approval next week.

## You already have a pending change {#ess.personal-data.pending-change}
<!-- alias: loc:YouAlreadyHaveAPendingPersonalDataChange -->

**What it is.** A notice that a request of yours is still waiting, so you cannot raise another.

**Example.** You asked to change your address on Sunday and it has not been approved yet.

**Why it matters.** Two requests changing the same record would race each other, and whichever was approved second would quietly undo the first. Wait for the one you have, or withdraw it and raise one request with everything in it.

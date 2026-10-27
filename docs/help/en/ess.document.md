---
helpId: ess.document
lang: en
title: My Documents
surfaces:
  - angular/src/app/main/ess/my-document/my-document.component.html
audience: [end-user]
permissions: [Ess.MyDocuments]
---

## Overview

Put a personal document on your HR file — a passport, a civil ID, a driving licence, a qualification — with its number, its dates and a scan.

Recording a document is what lets the system warn you before it expires, and what lets HR find it without asking you for a copy. The same document cannot be recorded twice: if it is already on your file, add the new one only when the number has actually changed, such as after a renewal.

## Document type {#ess.document.document-type}
<!-- alias: loc:DocumentType -->

**What it is.** What kind of document this is, from the types your company records.

**Example.** Passport; Civil ID; Driving Licence.

**Why it matters.** The type decides what is expected of the record and what is done with it — a passport and a civil ID are the two most companies act on, because their expiry drives residency and work permits.

## Document number {#ess.document.document-number}
<!-- alias: loc:DocumentNumber -->

**What it is.** The number printed on the document.

**Example.** A12345678

**Why it matters.** **Copy it exactly.** It is how the record is matched to the document, and it is what goes on government submissions — a mistyped number there comes back as a rejected application weeks later. The same type and number cannot be recorded twice for you, which is the check that catches a duplicate entry rather than a renewal.

## Issue date {#ess.document.issue-date}
<!-- alias: loc:IssueDate -->

**What it is.** When the document was issued.

**Example.** 3 March 2024.

**Why it matters.** It is what tells a renewal apart from the document it replaced when both are on file, and some authorities ask for it alongside the number.

## Expiry date {#ess.document.expiry-date}
<!-- alias: loc:ExpiryDate -->

**What it is.** When the document runs out.

**Example.** 2 March 2034.

**Why it matters.** This is the date the system watches. Reminders go out ahead of it, and an expired passport or civil ID stops residency renewals and travel — so it is the one field here worth checking twice. Whether it must be filled in depends on the document type: for a type that does not expire you may leave it blank. For some types the dates also may not overlap a document of the same type already on your file; if the request is refused for that reason, ask HR.

## Attachments {#ess.document.attachments}
<!-- alias: loc:Attachments -->

**What it is.** The scan or photograph of the document itself, attached to the record.

**Example.** A clear photo of the passport's photograph page.

**Why it matters.** Without it HR has your word for the number and nothing to check it against, and they will ask you for a copy the first time somebody needs one. A legible scan now saves that. Attachments are kept in your company's file store, not in the database, and only people who may see your record may open them. To see them again later, open **Details** on the request, or choose Attachments from the row's Actions.

## New Request {#ess.document.new-request}
<!-- alias: loc:NewRequest -->

**What it is.** The one button for everything you can ask for on this screen. Choose the request type — New Document, Withdraw Document or Print Letter — and press **Create**; the form for that request opens, and the document type is chosen on the form.

**Example.** Choose New Document to add a renewed passport; Print Letter to ask for a salary certificate; Withdraw Document to take your passport out.

**Why it matters.** A request type your company has not switched on for you is not offered, so the list may be shorter than a colleague's in another company. Each request is sent for approval on its own and appears in its own list below.

## Status {#ess.document.document-status}
<!-- alias: loc:Status -->

**What it is.** In the Documents list, where the document stands today: Active, Inactive or Expired. In the Document Withdrawals and Letters lists, where the request has got to: Draft, Open, Approved, Rejected or Withdrawn.

**Example.** A passport renewed last month is Active; the passport it replaced shows Inactive.

**Why it matters.** Nobody sets a document's status. It becomes Inactive by itself as soon as a newer one of the same type is on your file, and Expired once its expiry date has passed. Only the Active one can be withdrawn. A document you have asked for that is not on your file yet shows where the request is instead — Draft, Open or Rejected — until it is approved.

## Withdraw Document {#ess.document.withdraw-document}
<!-- alias: loc:WithdrawDocument -->

**What it is.** Asks to take this document out of company custody. The withdrawal form opens with the document already chosen.

**Example.** Withdrawing your passport for a visa appointment.

**Why it matters.** Available only for document types your company lets employees withdraw, and only for the Active document. If it is already out, asking again extends the date it is due back.

## Renew Document {#ess.document.renew-document}
<!-- alias: loc:RenewDocument -->

**What it is.** Records a newer document of the same type. The form opens with the type already chosen.

**Example.** Adding your new passport after a renewal, with its new number and dates.

**Why it matters.** The old record is never overwritten. Once the new one is approved, the old one shows Inactive and stays on your file as history.

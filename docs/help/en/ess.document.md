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

**Why it matters.** This is the date the system watches. Reminders go out ahead of it, and an expired passport or civil ID stops residency renewals and travel — so it is the one field here worth checking twice.

## Attachments {#ess.document.attachments}
<!-- alias: loc:Attachments -->

**What it is.** The scan or photograph of the document itself, attached to the record.

**Example.** A clear photo of the passport's photograph page.

**Why it matters.** Without it HR has your word for the number and nothing to check it against, and they will ask you for a copy the first time somebody needs one. A legible scan now saves that. Attachments are kept in your company's file store, not in the database, and only people who may see your record may open them.

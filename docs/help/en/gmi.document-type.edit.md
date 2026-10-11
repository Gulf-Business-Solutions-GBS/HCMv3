---
helpId: gmi.document-type.edit
lang: en
title: Document Types — Create / Edit
entity: HCMv3.GeneralMasterInfo.DocumentType
surfaces:
  - angular/src/app/admin/general-master-info/document-types/create-or-edit-document-type-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.DocumentTypes.Create]
---

## Overview

The kinds of document HCMv3 keeps -- for employees and for the organisation. Employee documents, organisation documents and self-service letter requests each name a document type.

The reminder settings on this form are stored, but no process reads them at present, so they do not yet send reminders before a document expires.

## NotificationPeriod {#DocumentType.NotificationPeriod}
<!-- alias: loc:NotificationPeriod, dom:DocumentType_NotificationPeriod -->

**What it is.** How long before a document's expiry reminders were meant to start.

**Why it matters.** Not read at present.

**Rules.** Required. Whole number.

## NotificationConfig {#DocumentType.NotificationConfigId}
<!-- alias: loc:NotificationConfig, dom:DocumentType_NotificationConfigId -->

**What it is.** The notification settings meant for this document type's reminders.

**Why it matters.** Not read at present.

**Rules.** Optional. Chosen from Notification Config master data.

## NotificationFrequency {#DocumentType.NotificationFrequency}
<!-- alias: loc:NotificationFrequency, dom:DocumentType_NotificationFrequency -->

**What it is.** How often a reminder was meant to repeat.

**Why it matters.** Not read at present.

**Rules.** Required. One of: Once, Daily, Weekly, Monthly.

## NotificationCountLimit {#DocumentType.NotificationCountLimit}
<!-- alias: loc:NotificationCountLimit, dom:DocumentType_NotificationCountLimit -->

**What it is.** The most reminders meant to be sent for one document.

**Why it matters.** Not read at present.

**Rules.** Required. Whole number.

## DocumentBelongsTo {#DocumentType.DocumentBelongsTo}
<!-- alias: loc:DocumentBelongsTo, dom:DocumentType_DocumentBelongsTo -->

**What it is.** Whether documents of this type belong to employees or to the organisation.

**Why it matters.** Recorded for information; nothing filters document types by it at present.

**Rules.** Required. One of: All, Employee, Organization.

## IncludeInWorkflow {#DocumentType.IncludeInWorkflow}
<!-- alias: loc:IncludeInWorkflow, dom:DocumentType_IncludeInWorkflow -->

**What it is.** Whether employees are offered this type when they raise a request in self service.

**Example.** Ticked for Passport and Salary Certificate; unticked for a type only HR records.

**Why it matters.** Untick it and the type disappears from the New Request list on the employee's Documents screen, and a request for it is refused. HR can still record documents of the type.

## IsLetterType {#DocumentType.IsLetterType}
<!-- alias: loc:IsLetterType, dom:DocumentType_IsLetterType -->

**What it is.** Whether this type is a letter the company issues to the employee, such as a salary certificate, and not a document the employee holds.

**Example.** Ticked for Salary Certificate and Experience Certificate; unticked for Passport and Civil ID.

**Why it matters.** A ticked type is offered under "Request a letter" and asks who the letter is addressed to. An unticked type is offered under "Record a document" and asks for a number and dates. A type cannot be both.

## IsWithdrawable {#DocumentType.IsWithdrawable}
<!-- alias: loc:IsWithdrawable, dom:DocumentType_IsWithdrawable -->

**What it is.** Whether employees may ask to take a document of this type out of company custody and hand it back.

**Example.** Ticked for Passport.

**Why it matters.** Only ticked types offer Withdraw Document to the employee. More than one type may be ticked. If no type at all is ticked, the system falls back to the single type named by the older passport setting (code 2 unless changed).

## IsHiddenFromEmployee {#DocumentType.IsHiddenFromEmployee}
<!-- alias: loc:IsHiddenFromEmployee, dom:DocumentType_IsHiddenFromEmployee -->

**What it is.** Keeps documents of this type out of the employee's own Documents screen.

**Example.** Ticked for a document HR holds about the employee and not for them.

**Why it matters.** The employee neither sees these documents nor can record one. HR's own screens are not affected.

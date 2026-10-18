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
<!-- alias: loc:ReminderDays, dom:DocumentType_NotificationPeriod -->

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

**Rules.** Yes or no.

## IsLetterType {#DocumentType.IsLetterType}
<!-- alias: loc:IsLetterType, dom:DocumentType_IsLetterType -->

**What it is.** Whether this type is a letter the company issues to the employee, such as a salary certificate, and not a document the employee holds.

**Example.** Ticked for Salary Certificate and Experience Certificate; unticked for Passport and Civil ID.

**Why it matters.** A ticked type is offered when the employee chooses Print Letter, and asks who the letter is addressed to. An unticked type is offered when the employee chooses New Document, and asks for a number and dates. A type cannot be both.

**Rules.** Yes or no.

## PrintReport {#DocumentType.PrintReportId}
<!-- alias: loc:PrintReport, dom:DocumentType_PrintReportId -->

**What it is.** The report a letter of this type is produced from: automatically when a request is approved, and again whenever HR presses Print Letter. Shown only when Issued As Letter is ticked.

**Example.** Salary Certificate English for the Salary Certificate type; Experience Certificate for an Employment Certificate type.

**Why it matters.** Without a choice here, the report is found from the type's English name with the spaces removed — "Salary Certificate" prints the first report whose name starts with SalaryCertificate — and a type whose name matches no report cannot be printed at all. Choosing the report removes the guesswork and lets a type print a report with a different name, such as the Arabic certificate.

**Rules.** Optional. Chosen from Print Report master data.

## IsWithdrawable {#DocumentType.IsWithdrawable}
<!-- alias: loc:IsWithdrawable, dom:DocumentType_IsWithdrawable -->

**What it is.** Whether employees may ask to take a document of this type out of company custody and hand it back.

**Example.** Ticked for Passport.

**Why it matters.** Only ticked types offer Withdraw Document to the employee. More than one type may be ticked. If no type at all is ticked, the system falls back to the single type named by the older passport setting (code 2 unless changed).

**Rules.** Yes or no.

## IsHiddenFromEmployee {#DocumentType.IsHiddenFromEmployee}
<!-- alias: loc:IsHiddenFromEmployee, dom:DocumentType_IsHiddenFromEmployee -->

**What it is.** Keeps documents of this type out of the employee's own Documents screen.

**Example.** Ticked for a document HR holds about the employee and not for them.

**Why it matters.** The employee neither sees these documents nor can record one. HR's own screens are not affected.

**Rules.** Yes or no.

## ShowToAll {#DocumentType.ShowToAll}
<!-- alias: loc:ShowToAll, dom:DocumentType_ShowToAll -->

**What it is.** Makes organisation documents of this type company policies everyone may read.

**Example.** Ticked for Employee Handbook and Code of Conduct; unticked for Commercial Licence.

**Why it matters.** Organisation documents of a ticked type are listed, read-only, on the Org Policy screen for everyone who has access to it, and their attached files can be opened there. Untick it and they disappear from that screen; they stay on Organization Documents for HR.

**Rules.** Yes or no.

## IsExpiryDateRequired {#DocumentType.IsExpiryDateRequired}
<!-- alias: loc:IsExpiryDateRequired, dom:DocumentType_IsExpiryDateRequired -->

**What it is.** Whether a document of this type must carry an expiry date.

**Example.** Ticked for Passport and Civil ID; unticked for a qualification certificate, which does not expire.

**Why it matters.** When ticked, a document saved without an expiry date is refused, on HR's screen and on the employee's own request. When unticked, the expiry date may be left blank and the document is treated as never expiring. It also changes how Allow Date Overlap compares two documents.

**Rules.** Yes or no.

## AllowDateOverlap {#DocumentType.AllowDateOverlap}
<!-- alias: loc:AllowDateOverlap, dom:DocumentType_AllowDateOverlap -->

**What it is.** Whether an employee may hold two documents of this type whose dates overlap.

**Example.** Unticked for Passport: a second passport dated inside the first one's validity is refused.

**Why it matters.** When unticked, a new document of the type is refused if its issue-to-expiry period overlaps one already on the employee's file. For a type with no required expiry date, only two documents issued on the same day clash. **A renewal entered before the old document runs out counts as an overlap**, so tick this for types that are renewed early. Document types that existed before this switch was added were ticked automatically, so nothing that was accepted before is refused now.

**Rules.** Yes or no.

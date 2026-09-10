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

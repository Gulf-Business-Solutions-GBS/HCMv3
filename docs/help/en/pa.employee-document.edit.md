---
helpId: pa.employee-document.edit
lang: en
title: Employee Documents — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeDocument
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/documents/create-or-edit-employee-document-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeDocuments.Create]
---

## Overview

An official document held for the employee -- a passport, civil ID, residency or licence -- with its number and validity.

Whether the document is in or out follows its movements: while a withdrawal has not been returned, the document shows as out. Documents can also be added through self-service, and passports withdrawn there.

## DocumentType {#EmployeeDocument.DocumentTypeId}
<!-- alias: loc:DocumentType, dom:EmployeeDocument_DocumentTypeId -->

**What it is.** The kind of document.

**Example.** Passport

**Rules.** Required. Chosen from Document Type master data.

:::admin
Maintained under General Master Info → Document Types. Their reminder settings are not acted on at present, so no expiry reminders are sent.
:::

## DocumentNum {#EmployeeDocument.DocumentNum}
<!-- alias: loc:DocumentNum, dom:EmployeeDocument_DocumentNum -->

**What it is.** The document number.

**Example.** P01234567

**Why it matters.** Required when the document is submitted through self-service.

**Rules.** Optional. Maximum 128 characters.

## IssueDate {#EmployeeDocument.IssueDate}
<!-- alias: loc:IssueDate, dom:EmployeeDocument_IssueDate -->

**What it is.** The date the document was issued.

**Example.** 3 Mar 2022

**Rules.** Required. Date value.

## ExpiryDate {#EmployeeDocument.ExpiryDate}
<!-- alias: loc:ExpiryDate, dom:EmployeeDocument_ExpiryDate -->

**What it is.** The date the document expires.

**Example.** 2 Mar 2032

**Why it matters.** Self-service shows the employee their documents that have not yet expired.

**Rules.** Optional. Date value.

## DocumentPlace {#EmployeeDocument.DocPlaceId}
<!-- alias: loc:DocumentPlace, dom:EmployeeDocument_DocPlaceId -->

**What it is.** Where the original document is kept, from the document places list.

**Example.** HR Office Safe

**Why it matters.** Recorded for reference; nothing else uses it.

**Rules.** Optional. Chosen from Document Place master data.

## Description {#EmployeeDocument.Description}
<!-- alias: loc:Description, dom:EmployeeDocument_Description -->

**What it is.** A description of the document.

**Example.** Kuwait residency, Article 18

**Why it matters.** Shown on the document and passport reports.

**Rules.** Optional. Maximum 2000 characters.

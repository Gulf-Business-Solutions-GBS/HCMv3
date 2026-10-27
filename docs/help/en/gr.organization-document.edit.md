---
helpId: gr.organization-document.edit
lang: en
title: Organization Documents — Create / Edit
entity: HCMv3.GovtRelationsManagement.OrganizationDocument
surfaces:
  - angular/src/app/admin/govt-relations-management/organization-documents/create-or-edit-organization-document-modal.component.html
audience: [admin]
permissions: [Pages.GovtRelationsManagement.OrganizationDocuments.Create]
---

## Overview

A document or licence held by the organisation, such as a commercial licence. The expiry date cannot be before the issue date. The Organization Documents report lists documents valid in a date range and the days left before each expires.

## OrganizationUnit {#OrganizationDocument.OrganizationUnitId}
<!-- alias: loc:OrganizationUnit, dom:OrganizationDocument_OrganizationUnitId -->

**What it is.** The part of the organisation the document belongs to.

**Rules.** Required. Chosen from Organization Unit master data.

## DocumentType {#OrganizationDocument.DocumentTypeId}
<!-- alias: loc:DocumentType, dom:OrganizationDocument_DocumentTypeId -->

**What it is.** The kind of document.

**Example.** Commercial licence

**Rules.** Required. Chosen from Document Type master data.

## DocumentNum {#OrganizationDocument.DocumentNum}
<!-- alias: loc:DocumentNum, dom:OrganizationDocument_DocumentNum -->

**What it is.** The document number.

**Rules.** Optional. Maximum 256 characters.

## Sponsor {#OrganizationDocument.SponsorId}
<!-- alias: loc:Sponsor, dom:OrganizationDocument_SponsorId -->

**What it is.** The sponsor the document is for, if any.

**Rules.** Optional. Chosen from Sponsor master data.

## IssueDate {#OrganizationDocument.IssueDate}
<!-- alias: loc:IssueDate, dom:OrganizationDocument_IssueDate -->

**What it is.** When it was issued.

**Rules.** Required. Date value.

## ExpiryDate {#OrganizationDocument.ExpiryDate}
<!-- alias: loc:ExpiryDate, dom:OrganizationDocument_ExpiryDate -->

**What it is.** When it expires.

**Rules.** Required. Date value.

## DocumentPlace {#OrganizationDocument.DocumentPlace}
<!-- alias: loc:DocumentPlace, dom:OrganizationDocument_DocumentPlace -->

**What it is.** Where the original is kept.

**Rules.** Optional. Maximum 256 characters.

## NotificationSent {#OrganizationDocument.NotificationSent}
<!-- alias: loc:NotificationSent, dom:OrganizationDocument_NotificationSent -->

**What it is.** Whether an expiry notice has been sent.

**Why it matters.** Not read at present.

**Rules.** Yes or no.

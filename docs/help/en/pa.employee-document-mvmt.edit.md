---
helpId: pa.employee-document-mvmt.edit
lang: en
title: Document Movements — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeDocumentMvmt
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/documents/employee-document-movements-modal.component.html
audience: [admin]
permissions: []
---

## Overview

A withdrawal and return of one of the employee's documents -- for example a passport handed back for travel.

While a movement has no actual return date, the document shows as out. Employees can request a passport withdrawal through self-service.

## HandedOverOn {#EmployeeDocumentMvmt.ActualWithdrawDate}
<!-- alias: loc:HandedOverOn, dom:Mvmt_HandOverDate -->

**What it is.** The day the document was actually handed over to the employee.

**Why it matters.** Recording it marks the document as out with the employee. It cannot be later than today.

**Rules.** Required. Date value.

## ActualReturnDate {#EmployeeDocumentMvmt.ActualReturnDate}
<!-- alias: loc:ActualReturnDate, dom:Mvmt_ActualReturnDate -->

**What it is.** The date the document actually came back.

**Example.** 18 Jul 2026

**Why it matters.** Leave it empty while the document is still out.

**Rules.** Optional. Date value.

## ReturnRemarks {#EmployeeDocumentMvmt.ReturnRemarks}
<!-- alias: loc:ReturnRemarks, dom:Mvmt_ReturnRemarks -->

**What it is.** A note made when the document comes back.

**Example.** Passport returned in good condition; new visa stamp on page 12.

**Why it matters.** For information only. When the return comes through a self-service request, approvers see it on the request.

**Rules.** Optional.

## WithdrawDate {#EmployeeDocumentMvmt.WithdrawDate}
<!-- alias: loc:WithdrawDate, dom:Mvmt_WithdrawDate -->

**What it is.** The date the document was taken out.

**Example.** 1 Jul 2026

**Rules.** Required. Date value.

## ReturnDate {#EmployeeDocumentMvmt.ReturnDate}
<!-- alias: loc:ReturnDate, dom:Mvmt_ReturnDate -->

**What it is.** The date the document is due back.

**Example.** 20 Jul 2026

**Rules.** Required. Date value.

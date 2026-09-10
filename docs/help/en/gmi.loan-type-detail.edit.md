---
helpId: gmi.loan-type-detail.edit
lang: en
title: Loan Type Details — Create / Edit
entity: HCMv3.GeneralMasterInfo.LoanTypeDetail
surfaces:
  - angular/src/app/admin/general-master-info/loan-type-details/create-or-edit-loan-type-detail-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.LoanTypeDetails.Create]
---

## Overview

Percentages attached to a loan type. Not used at present: they belong to the variable loan recovery, which the payroll calculation does not support yet.

## LoanType {#LoanTypeDetail.LoanTypeId}
<!-- alias: loc:LoanType, dom:LoanTypeDetail_LoanTypeId -->

**What it is.** The loan type the percentage belongs to.

**Rules.** Optional. Chosen from Loan Type master data.

## Percent {#LoanTypeDetail.Percent}
<!-- alias: loc:Percent, dom:LoanTypeDetail_Percent -->

**What it is.** The percentage.

**Rules.** Required. Number.

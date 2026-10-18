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

## WageType {#LoanTypeDetail.WageTypeId}
<!-- alias: loc:WageType, dom:LoanTypeDetail_WageTypeId -->

**What it is.** A pay item that the percentage on this row is taken of, for loans whose deduction type is Variable.

**Example.** 1001 - Basic Salary, with Percent 25

**Why it matters.** For a Variable loan, the monthly instalment is the sum, over all rows, of this wage type's amount on the employee's payslip times the row's percent, never more than the balance still owed. A wage type the employee is not paid that month adds nothing. Each wage type can be listed only once per loan type, and the percent must be more than 0 and no more than 100.

**Rules.** Required. Chosen from Wage Type master data.

## Percent {#LoanTypeDetail.Percent}
<!-- alias: loc:Percent, dom:LoanTypeDetail_Percent -->

**What it is.** The percentage.

**Rules.** Required. Number.

---
helpId: pa.employee-fund-detail.edit
lang: en
title: Employee Fund Details — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeFundDetail
surfaces:
  - angular/src/app/admin/personnel-administration/employee-fund-details/create-or-edit-employee-fund-detail-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeFundDetails.Create]
---

## Overview

A transaction on the employee's fund account: an opening balance, a deduction, a payment out, or an external deduction.

Payroll pays out Payment lines dated in the month it processes. It also rebuilds the month's Deduction and Opening lines each time it runs: lines of those two kinds dated in that month are deleted and recalculated.

## EmployeeFundId {#EmployeeFundDetail.EmployeeFundId}
<!-- alias: loc:EmployeeFundId, dom:EmployeeFundDetail_EmployeeFundId -->

**What it is.** The fund account the transaction belongs to.

**Rules.** Required. Chosen from a list.

## TransactionDate {#EmployeeFundDetail.TransactionDate}
<!-- alias: loc:TransactionDate, dom:EmployeeFundDetail_TransactionDate -->

**What it is.** The date of the transaction.

**Example.** 30 Sep 2026

**Why it matters.** Payroll pays Payment lines dated in its month.

**Rules.** Required. Date value.

## Amount {#EmployeeFundDetail.Amount}
<!-- alias: loc:Amount, dom:EmployeeFundDetail_Amount -->

**What it is.** The amount of the transaction.

**Example.** 75.000

**Why it matters.** Opening and Deduction lines add to the fund; Payment lines are paid out of it.

**Rules.** Optional. Number.

## FundPayType {#EmployeeFundDetail.FundPayType}
<!-- alias: loc:FundPayType, dom:EmployeeFundDetail_FundPayType -->

**What it is.** The kind of transaction.

**Example.** Payment

**Rules.** Required. One of: Deduction, Payment, Opening, External Deduction.

:::admin
Deduction and Opening lines dated in a month that payroll processes are deleted and recalculated by payroll. Enter manual contributions for those months as External Deduction.
:::

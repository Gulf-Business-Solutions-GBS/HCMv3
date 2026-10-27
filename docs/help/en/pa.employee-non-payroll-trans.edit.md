---
helpId: pa.employee-non-payroll-trans.edit
lang: en
title: Non-Payroll Transactions — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeNonPayrollTrans
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/non-payroll-trans/create-or-edit-employee-non-payroll-tran-modal.component.html
audience: [admin]
permissions: []
---

## Overview

A payment made to the employee outside payroll -- for example a bonus paid by cheque.

Payroll does not pay it. The average bonus calculation does count non-payroll amounts of the bonus wagetype within the period it averages, and the bonus and non-payroll reports show them.

## WageTypeId {#EmployeeNonPayrollTrans.WageTypeId}
<!-- alias: loc:WageTypeId, dom:EmployeeNonPayrollTran_WageTypeId -->

**What it is.** The pay element the payment is recorded against.

**Example.** Annual Bonus

**Rules.** Required. Chosen from Wage Type master data.

:::admin
Maintained under Payroll Management → Wagetypes.
:::

## PaymentDate {#EmployeeNonPayrollTrans.PaymentDate}
<!-- alias: loc:PaymentDate, dom:EmployeeNonPayrollTran_PaymentDate -->

**What it is.** The date the payment was made.

**Example.** 15 Mar 2026

**Rules.** Required. Date value.

## Amount {#EmployeeNonPayrollTrans.Amount}
<!-- alias: loc:Amount, dom:EmployeeNonPayrollTran_Amount -->

**What it is.** The amount paid.

**Example.** 1000.000

**Rules.** Required. Number.

## CurrencyId {#EmployeeNonPayrollTrans.CurrencyId}
<!-- alias: loc:CurrencyId, dom:EmployeeNonPayrollTran_CurrencyId -->

**What it is.** The currency of the payment.

**Example.** KWD

**Rules.** Optional. Chosen from Currency master data.

## PayableBy {#EmployeeNonPayrollTrans.PayableBy}
<!-- alias: loc:PayableBy, dom:EmployeeNonPayrollTran_PayableBy -->

**What it is.** Who pays it, as free text.

**Example.** Head Office Finance

**Why it matters.** Shown on the non-payroll report.

**Rules.** Optional. Maximum 256 characters.

## PaymentMethod {#EmployeeNonPayrollTrans.PaymentMethod}
<!-- alias: loc:PaymentMethod, dom:EmployeeNonPayrollTran_PaymentMethod -->

**What it is.** How it was paid, as free text.

**Example.** Cheque

**Why it matters.** Shown on the non-payroll report.

**Rules.** Optional. Maximum 256 characters.

## IsLocked {#EmployeeNonPayrollTrans.IsLocked}
<!-- alias: loc:IsLocked, dom:EmployeeNonPayrollTran_IsLocked -->

**What it is.** Marks the record as locked.

**Why it matters.** Shown on the non-payroll report; it does not stop the record being edited.

**Rules.** Yes or no.

## NotificationSent {#EmployeeNonPayrollTrans.NotificationSent}
<!-- alias: loc:NotificationSent, dom:EmployeeNonPayrollTran_NotificationSent -->

**What it is.** Marks that the employee has been notified.

**Why it matters.** Recorded only; nothing sends or reads it at present.

**Rules.** Yes or no.

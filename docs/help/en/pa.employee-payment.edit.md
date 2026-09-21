---
helpId: pa.employee-payment.edit
lang: en
title: Employee Payments — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeePayment
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/payments/create-or-edit-employee-payment-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeePayments.Create]
---

## Overview

A one-off payment or deduction for the employee on a date -- for example a bonus, commission or a single allowance. Payroll pays payments whose date falls in the period it processes.

Payroll also creates payment records itself for advance payments and net-pay carry-overs, and approved payment batches create them too.

## WageTypeId {#EmployeePayment.WageTypeId}
<!-- alias: loc:WageTypeId, dom:EmployeePayment_WageTypeId -->

**What it is.** The pay element being paid.

**Example.** Performance Bonus

**Rules.** Required. Chosen from Wagetype master data.

:::admin
Maintained under Payroll Management → Wagetypes.
:::

## PaymentDate {#EmployeePayment.PaymentDate}
<!-- alias: loc:PaymentDate, dom:EmployeePayment_PaymentDate -->

**What it is.** The date the payment is due.

**Example.** 30 Sep 2026

**Why it matters.** Payroll includes the payment in the run whose period covers this date.

**Rules.** Required. Date value.

## Amount {#EmployeePayment.Amount}
<!-- alias: loc:Amount, dom:EmployeePayment_Amount -->

**What it is.** The amount, or the number of days or hours, depending on the unit type.

**Example.** 250.000

**Rules.** Required. Number.

## CurrencyId {#EmployeePayment.CurrencyId}
<!-- alias: loc:CurrencyId, dom:EmployeePayment_CurrencyId -->

**What it is.** The currency of the payment.

**Example.** KWD

**Rules.** Optional. Chosen from Currency master data.

## UnitType {#EmployeePayment.UnitType}
<!-- alias: loc:UnitType, dom:EmployeePayment_UnitType -->

**What it is.** Whether the amount is money, days or hours.

**Example.** Amount

**Why it matters.** An Amount is paid as entered. Days and hours are converted to days and paid at the employee's rate for wagetypes included in partial pay; hours use the default working hours per day.

**Rules.** Required. One of: Amount, Day, Hour.

## CostCenterId {#EmployeePayment.CostCenterId}
<!-- alias: loc:CostCenterId, dom:EmployeePayment_CostCenterId -->

**What it is.** A cost centre recorded with the payment.

**Example.** CC-100 Finance

**Why it matters.** Recorded only; cost postings do not read it at present.

**Rules.** Optional. Chosen from Cost Center master data.

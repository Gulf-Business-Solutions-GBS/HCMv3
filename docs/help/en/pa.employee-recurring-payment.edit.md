---
helpId: pa.employee-recurring-payment.edit
lang: en
title: Employee Recurring Payments — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeRecurringPayment
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/recurring-payments/create-or-edit-employee-recurring-payment-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeRecurringPayments.Create]
---

## Overview

A fixed amount paid to, or deducted from, the employee under one wage type for a period -- a car allowance, a phone allowance or a fixed monthly deduction. Opened from the Recurring Payments section of the employee record.

Payroll reads Monthly and Yearly payments. A Monthly payment adds its full amount for every month its period touches, even a month it covers only in part. A Yearly payment adds its amount once a year, in the month of its start date. Daily, Hourly and End Of Service payments are saved but not paid at present.

The amount must be more than zero, and two payments of the same wage type for the same employee cannot overlap. With Enable Effective Date In Recurring Payment switched on in payroll settings, a new open-ended payment (end date 31/12/9999) closes the current open-ended one the day before it starts, instead of being refused as an overlap.

## WageTypeId {#EmployeeRecurringPayment.WageTypeId}
<!-- alias: loc:WageTypeId, dom:EmployeeRecurringPayment_WageTypeId -->

**What it is.** The wage type the amount is paid or deducted under.

**Example.** Car Allowance

**Why it matters.** Payroll adds the amount to that wage type's line on the payslip, so the wage type's billing type decides whether it is an earning or a deduction.

**Rules.** Required. Chosen from Wage Type master data.

:::admin
Maintained under Payroll Management → Wage Types.
:::

## PayInterval {#EmployeeRecurringPayment.PayInterval}
<!-- alias: loc:PayInterval, dom:EmployeeRecurringPayment_PayInterval -->

**What it is.** How often the amount is paid.

**Example.** Monthly

**Why it matters.** Monthly pays every month of the period; Yearly pays once a year, in the start date's month. Payroll does not pay Daily, Hourly or End Of Service payments at present.

**Rules.** Required. One of: Monthly, Daily, Hourly, End Of Service, Yearly.

## Amount {#EmployeeRecurringPayment.Amount}
<!-- alias: loc:Amount, dom:EmployeeRecurringPayment_Amount -->

**What it is.** The amount paid each time, in the payroll currency.

**Example.** 75.000

**Why it matters.** Paid in full for each month that qualifies; it is not reduced for a part month.

**Rules.** Required. Number.

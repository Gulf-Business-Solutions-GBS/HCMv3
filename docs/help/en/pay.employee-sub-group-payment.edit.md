---
helpId: pay.employee-sub-group-payment.edit
lang: en
title: Employee Sub Group Payments — Create / Edit
entity: HCMv3.PayrollManagement.EmployeeSubGroupPayment
surfaces:
  - angular/src/app/admin/payroll-management/employee-sub-group-detail/payments/create-or-edit-employee-sub-group-payment-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.EmployeeSubGroupPayments.Create]
---

## Overview

A standing payment for everyone in the grade, paid through a wagetype.

Only two pay intervals are used. *Yearly* rows are paid once a year, in their **Pay Month**. *End Of Service* rows are paid in the employee's end-of-service run. Rows with any other interval are not paid.

## WageType {#EmployeeSubGroupPayment.WageTypeId}
<!-- alias: loc:WageType, dom:EmployeeSubGroupPayment_WageTypeId -->

**What it is.** The wagetype the payment is made under.

**Example.** Annual Bonus

**Rules.** Optional. Chosen from a list.

## Amount {#EmployeeSubGroupPayment.Amount}
<!-- alias: loc:Amount, dom:EmployeeSubGroupPayment_Amount -->

**What it is.** The amount paid.

**Example.** `300`

**Rules.** Optional. Number.

## PayInterval {#EmployeeSubGroupPayment.PayInterval}
<!-- alias: loc:PayInterval, dom:EmployeeSubGroupPayment_PayInterval -->

**What it is.** When the payment is made.

**Example.** Yearly

**Why it matters.** Only *Yearly* and *End Of Service* are paid.

**Rules.** Required. One of: Monthly, Daily, Hourly, End Of Service, Yearly.

## PayMonth {#EmployeeSubGroupPayment.PayMonth}
<!-- alias: loc:PayMonth, dom:EmployeeSubGroupPayment_PayMonth -->

**What it is.** The month a *Yearly* payment is made.

**Example.** December

**Rules.** Required. One of: None, January, February, March, April, May, June, July, August, September, October, November, December.

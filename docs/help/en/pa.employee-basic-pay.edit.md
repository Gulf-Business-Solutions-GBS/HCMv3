---
helpId: pa.employee-basic-pay.edit
lang: en
title: Employee Basic Pays — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeBasicPay
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/basic-pays/create-or-edit-employee-basic-pay-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeBasicPays.Create]
---

## Overview

The header of the employee's pay for a period. Its detail lines hold the wagetypes and amounts; this record says how often the pay is expressed.

Payroll uses the basic pay whose period covers the pay date. Saving a new one does not close the previous one, and only a duplicate start date is refused, so set the previous record's end date yourself.

## PayType {#EmployeeBasicPay.PayType}
<!-- alias: loc:PayType, dom:EmployeeBasicPay_PayType -->

**What it is.** The type of pay.

**Example.** Regular

**Why it matters.** Kept for reference; no calculation reads it at present.

**Rules.** Required. One of: None, Regular.

## PayInterval {#EmployeeBasicPay.PayInterval}
<!-- alias: loc:PayInterval, dom:EmployeeBasicPay_PayInterval -->

**What it is.** The period the detail amounts are expressed for.

**Example.** Monthly

**Why it matters.** Payroll works out each detail line from it: Monthly amounts are prorated by the wagetype's billing method, Daily and Hourly amounts are multiplied by the days or hours recorded for the employee under Employee Remunerations in that month.

**Rules.** Required. One of: Monthly, Daily, Hourly, End Of Service, Yearly.

:::admin
With End Of Service or Yearly, the basic pay calculation produces no amount for the detail lines.
:::

## EmployeeActionId {#EmployeeBasicPay.EmployeeActionId}
<!-- alias: loc:EmployeeActionId, dom:EmployeeBasicPay_EmployeeActionId -->

**What it is.** The personnel action this pay change belongs to, if any.

**Example.** Promotion — 1 Jan 2026

**Why it matters.** Recorded as a link only; payroll does not read it.

**Rules.** Optional. Chosen from a list.

---
helpId: pa.employee-basic-pay-detail.edit
lang: en
title: Employee Basic Pay Details — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeBasicPayDetail
surfaces:
  - angular/src/app/admin/personnel-administration/employee-basic-pay-details/create-or-edit-employee-basic-pay-detail-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeBasicPayDetails.Create]
---

## Overview

One wagetype and amount within a basic pay record -- for example basic salary, housing allowance or transport allowance. Payroll calculates each wagetype from the detail line of the basic pay in force on the pay date.

A basic pay record can hold each wagetype only once.

## EmployeeBasicPayId {#EmployeeBasicPayDetail.EmployeeBasicPayId}
<!-- alias: loc:EmployeeBasicPayId, dom:EmployeeBasicPayDetail_EmployeeBasicPayId -->

**What it is.** The basic pay record this line belongs to.

**Rules.** Required. Chosen from a list.

## WageTypeId {#EmployeeBasicPayDetail.WageTypeId}
<!-- alias: loc:WageTypeId, dom:EmployeeBasicPayDetail_WageTypeId -->

**What it is.** The pay element this line pays.

**Example.** Basic Salary

**Why it matters.** Each wagetype can appear once per basic pay record.

**Rules.** Required. Chosen from Wagetype master data.

:::admin
Maintained under Payroll Management → Wagetypes.
:::

## Amount {#EmployeeBasicPayDetail.Amount}
<!-- alias: loc:Amount, dom:EmployeeBasicPayDetail_Amount -->

**What it is.** The rate for this wagetype, per the basic pay's pay interval.

**Example.** 850.000

**Why it matters.** Payroll uses it as the monthly, daily or hourly rate for this wagetype, and it is carried into the cost and journal postings.

**Rules.** Optional. Number.

## CurrencyId {#EmployeeBasicPayDetail.CurrencyId}
<!-- alias: loc:CurrencyId, dom:EmployeeBasicPayDetail_CurrencyId -->

**What it is.** The currency of the amount.

**Example.** KWD

**Why it matters.** The journal voucher export reads it to post the employee's pay in this currency.

**Rules.** Required. Chosen from Currency master data.

## DependantUnit {#EmployeeBasicPayDetail.DependantUnit}
<!-- alias: loc:DependantUnit, dom:EmployeeBasicPayDetail_DependantUnit -->

**What it is.** Whether this line depends on another wagetype, and how.

**Example.** Percentage

**Why it matters.** Used only when a dependent wagetype is chosen.

**Rules.** Required. One of: None, Percentage.

## DependantNumber {#EmployeeBasicPayDetail.DependantNumber}
<!-- alias: loc:DependantNumber, dom:EmployeeBasicPayDetail_DependantNumber -->

**What it is.** The percentage of the dependent wagetype's amount.

**Example.** 25

**Rules.** Optional. Number.

## DependantWageTypeId {#EmployeeBasicPayDetail.DependantWageTypeId}
<!-- alias: loc:DependantWageTypeId, dom:EmployeeBasicPayDetail_DependantWageTypeId -->

**What it is.** Another wagetype this line is calculated from.

**Example.** Basic Salary

**Rules.** Optional. Chosen from Wagetype master data.

:::admin
When this is set, payroll calculates the percentage of that wagetype's amount but stores the result in the payroll line's number, not its amount -- behaviour carried over from the legacy system. The line does not currently pay the calculated percentage.
:::

---
helpId: pay.gratuity-accrual-summary.edit
lang: en
title: Gratuity Accrual Summaries — Create / Edit
entity: HCMv3.PayrollManagement.GratuityAccrualSummary
surfaces:
  - angular/src/app/admin/payroll-management/gratuity-accrual-summaries/create-or-edit-gratuity-accrual-summary-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.GratuityAccrualSummaries.Create]
---

## Overview

A monthly snapshot of an employee's end-of-service indemnity: days accrued, balance and amounts.

Not written at present: no process fills these summaries, and nothing reads them. Balances are worked out when they are needed, from the time register and the employee's records.

## EmployeeId {#GratuityAccrualSummary.EmployeeId}
<!-- alias: loc:EmployeeId, dom:GratuityAccrualSummary_EmployeeId -->

**What it is.** The employee.

**Rules.** Required. Chosen from the employee list.

## Year {#GratuityAccrualSummary.Year}
<!-- alias: loc:Year, dom:GratuityAccrualSummary_Year -->

**What it is.** The year of the snapshot.

**Example.** `2026`

**Rules.** Required. Whole number.

## Month {#GratuityAccrualSummary.Month}
<!-- alias: loc:Month, dom:GratuityAccrualSummary_Month -->

**What it is.** The month of the snapshot.

**Example.** `4`

**Rules.** Required. Whole number.

## GratuityDaysAccrued {#GratuityAccrualSummary.GratuityDaysAccrued}
<!-- alias: loc:GratuityDaysAccrued, dom:GratuityAccrualSummary_GratuityDaysAccrued -->

**What it is.** Indemnity days accrued in the month.

**Example.** `1.25`

**Rules.** Required. Number.

## GratuityDaysBalance {#GratuityAccrualSummary.GratuityDaysBalance}
<!-- alias: loc:GratuityDaysBalance, dom:GratuityAccrualSummary_GratuityDaysBalance -->

**What it is.** Indemnity days accrued to date.

**Example.** `37.5`

**Rules.** Required. Number.

## GratuityAmount {#GratuityAccrualSummary.GratuityAmount}
<!-- alias: loc:GratuityAmount, dom:GratuityAccrualSummary_GratuityAmount -->

**What it is.** The value of the indemnity to date.

**Example.** `1000.000`

**Rules.** Required. Number.

## GratuityAmountPaid {#GratuityAccrualSummary.GratuityAmountPaid}
<!-- alias: loc:GratuityAmountPaid, dom:GratuityAccrualSummary_GratuityAmountPaid -->

**What it is.** Indemnity already paid.

**Example.** `0.000`

**Rules.** Required. Number.

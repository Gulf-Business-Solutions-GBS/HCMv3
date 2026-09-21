---
helpId: pay.leave-accrual-summary.edit
lang: en
title: Leave Accrual Summaries — Create / Edit
entity: HCMv3.PayrollManagement.LeaveAccrualSummary
surfaces:
  - angular/src/app/admin/payroll-management/leave-accrual-summaries/create-or-edit-leave-accrual-summary-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.LeaveAccrualSummaries.Create]
---

## Overview

A monthly snapshot of an employee's leave balance for one leave type.

Not written at present: no process fills these summaries, and nothing reads them. Balances are worked out when they are needed, from the time register and the employee's records.

## EmployeeId {#LeaveAccrualSummary.EmployeeId}
<!-- alias: loc:EmployeeId, dom:LeaveAccrualSummary_EmployeeId -->

**What it is.** The employee.

**Rules.** Required. Chosen from the employee list.

## AbsenceTypeId {#LeaveAccrualSummary.AbsenceTypeId}
<!-- alias: loc:AbsenceTypeId, dom:LeaveAccrualSummary_AbsenceTypeId -->

**What it is.** The leave type.

**Example.** Annual Leave

**Rules.** Required. Chosen from Absence Type master data.

## Year {#LeaveAccrualSummary.Year}
<!-- alias: loc:Year, dom:LeaveAccrualSummary_Year -->

**What it is.** The year of the snapshot.

**Example.** `2026`

**Rules.** Required. Whole number.

## Month {#LeaveAccrualSummary.Month}
<!-- alias: loc:Month, dom:LeaveAccrualSummary_Month -->

**What it is.** The month of the snapshot.

**Example.** `4`

**Rules.** Required. Whole number.

## AccrualDays {#LeaveAccrualSummary.AccrualDays}
<!-- alias: loc:AccrualDays, dom:LeaveAccrualSummary_AccrualDays -->

**What it is.** Leave days accrued in the month.

**Example.** `2.5`

**Rules.** Required. Number.

## UsedDays {#LeaveAccrualSummary.UsedDays}
<!-- alias: loc:UsedDays, dom:LeaveAccrualSummary_UsedDays -->

**What it is.** Leave days taken in the month.

**Example.** `3`

**Rules.** Required. Number.

## CumulativeAccruedDays {#LeaveAccrualSummary.CumulativeAccruedDays}
<!-- alias: loc:CumulativeAccruedDays, dom:LeaveAccrualSummary_CumulativeAccruedDays -->

**What it is.** Leave days accrued to date.

**Example.** `22.5`

**Rules.** Required. Number.

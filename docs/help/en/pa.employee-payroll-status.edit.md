---
helpId: pa.employee-payroll-status.edit
lang: en
title: Employee Payroll Statuses — Create / Edit
entity: HCMv3.PayrollManagement.EmployeePayrollStatus
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/payroll-status/create-or-edit-employee-payroll-status-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.EmployeePayrollStatuses.Create]
---

## Overview

How far the employee has been processed by payroll and the time engine, and the dates that control recalculation. An employee can have only one.

The time engine creates this record the first time it processes the employee, and payroll stops with "Payroll Status not found" for an employee who has none. Both engines update the dates as they run; change them only to force or limit a recalculation.

## EarliestRetroAccountingDate {#EmployeePayrollStatus.EarliestRetroAccountingDate}
<!-- alias: loc:EarliestRetroAccountingDate, dom:EmployeePayrollStatus_earliestRetroAccountingDate -->

**What it is.** The earliest date recalculation may go back to.

**Example.** 1 Jan 2026

**Why it matters.** If a change reaches back before this date, the time engine and regular payroll stop for the employee with "EarliestRetroAccountingDate set to …", unless the date is the joining date.

**Rules.** Optional. Date value.

## AccountedTo {#EmployeePayrollStatus.AccountedTo}
<!-- alias: loc:AccountedTo, dom:EmployeePayrollStatus_accountedTo -->

**What it is.** The last pay date payroll has processed for the employee.

**Example.** 31 Aug 2026

**Why it matters.** Payroll recalculates from the month after this date up to the current pay date, and moves it forward after each run.

**Rules.** Optional. Date value.

## RunPayrollUpto {#EmployeePayrollStatus.RunPayrollUpto}
<!-- alias: loc:RunPayrollUpto, dom:EmployeePayrollStatus_runPayrollUpto -->

**What it is.** A date payroll should run up to.

**Why it matters.** Recorded only; neither engine reads it at present.

**Rules.** Optional. Date value.

## EarliestMasterDataChange {#EmployeePayrollStatus.EarliestMasterDataChange}
<!-- alias: loc:EarliestMasterDataChange, dom:EmployeePayrollStatus_earliestMasterDataChange -->

**What it is.** The earliest date affected by a change to the employee's records since payroll last ran.

**Example.** 1 Jun 2026

**Why it matters.** Payroll recalculates every month from this date; the time engine starts from it when autorun is off. The time engine sets it when a day changes, and payroll clears it after a successful run.

**Rules.** Optional. Date value.

## DoNoAccountAfter {#EmployeePayrollStatus.DoNoAccountAfter}
<!-- alias: loc:DoNoAccountAfter, dom:EmployeePayrollStatus_doNoAccountAfter -->

**What it is.** The last period payroll may calculate for the employee.

**Example.** 31 Dec 2026

**Why it matters.** Payroll and the project cost engine skip any period after this date.

**Rules.** Optional. Date value.

## EarliestRecalculationDate {#EmployeePayrollStatus.EarliestRecalculationDate}
<!-- alias: loc:EarliestRecalculationDate, dom:EmployeePayrollStatus_earliestRecalculationDate -->

**What it is.** An earliest recalculation date.

**Why it matters.** Recorded only; neither engine reads it at present.

**Rules.** Optional. Date value.

## PersonalCalendarFrom {#EmployeePayrollStatus.PersonalCalendarFrom}
<!-- alias: loc:PersonalCalendarFrom, dom:EmployeePayrollStatus_personalCalendarFrom -->

**What it is.** The date the employee's personal calendar starts.

**Why it matters.** Recorded only; neither engine reads it at present.

**Rules.** Optional. Date value.

## NextTimeEvaluationFrom {#EmployeePayrollStatus.NextTimeEvaluationFrom}
<!-- alias: loc:NextTimeEvaluationFrom, dom:EmployeePayrollStatus_nextTimeEvaluationFrom -->

**What it is.** The date the time engine will next process the employee from.

**Example.** 1 Sep 2026

**Why it matters.** The time engine moves it forward as it runs. An off-cycle payroll other than end of service stops with "Time Engine not run" if this date is before the payment date.

**Rules.** Optional. Date value.

## PersonLocked {#EmployeePayrollStatus.PersonLocked}
<!-- alias: loc:PersonLocked, dom:EmployeePayrollStatus_PersonLocked -->

**What it is.** Marks the employee as locked for payroll.

**Why it matters.** Stored only; payroll does not currently skip a locked employee.

**Rules.** Yes or no.

## PayrollCorrection {#EmployeePayrollStatus.PayrollCorrection}
<!-- alias: loc:PayrollCorrection, dom:EmployeePayrollStatus_PayrollCorrection -->

**What it is.** Marks the employee as needing a payroll correction.

**Why it matters.** Stored only; no engine reads it at present.

**Rules.** Yes or no.

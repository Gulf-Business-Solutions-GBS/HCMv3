---
helpId: pay.salary-register.edit
lang: en
title: Salary Registers — Create / Edit
entity: HCMv3.PayrollManagement.SalaryRegister
surfaces:
  - angular/src/app/admin/payroll-management/salary-registers/create-or-edit-salary-register-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.SalaryRegisters.Create]
---

## Overview

An employee's payslip for one month and run, as payroll calculated it. The payroll engine writes these; its lines are the salary register details.

A **locked** payslip cannot be changed or deleted. With the payroll setting *Lock Salary Register On Payroll Exit* on, closing a period locks that month's regular payslips, and releasing it for payroll locks its off-cycle ones.

## EmployeeId {#SalaryRegister.EmployeeId}
<!-- alias: loc:EmployeeId, dom:SalaryRegister_EmployeeId -->

**What it is.** The employee the payslip is for.

**Rules.** Required. Chosen from the employee list.

## ForPeriod {#SalaryRegister.ForPeriod}
<!-- alias: loc:ForPeriod, dom:SalaryRegister_ForPeriod -->

**What it is.** The month the pay is for.

**Example.** `2026-03-31`

**Why it matters.** For a retro recalculation this is an earlier month than **In Period**.

**Rules.** Required. Date value.

## InPeriod {#SalaryRegister.InPeriod}
<!-- alias: loc:InPeriod, dom:SalaryRegister_InPeriod -->

**What it is.** The month the pay is paid in -- the run's pay date.

**Example.** `2026-04-30`

**Rules.** Required. Date value.

## PayrollRunType {#SalaryRegister.PayrollRunType}
<!-- alias: loc:PayrollRunType, dom:SalaryRegister_PayrollRunType -->

**What it is.** Whether the payslip came from a regular or an off-cycle run.

**Example.** Regular

**Rules.** Required. One of: Regular, Off Cycle.

## PayrollResultType {#SalaryRegister.PayrollResultType}
<!-- alias: loc:PayrollResultType, dom:SalaryRegister_PayrollResultType -->

**What it is.** Which result this is for its month.

**Example.** Current

**Why it matters.** A regular run writes *Current*. When the same month is recalculated in a later run, the older payslips for that month become *Last* (the most recent before) and *Archive* (the rest).

**Rules.** Required. One of: Current, Last, Archive.

## PayrollReasonTypeId {#SalaryRegister.PayrollReasonTypeId}
<!-- alias: loc:PayrollReasonTypeId, dom:SalaryRegister_PayrollReasonTypeId -->

**What it is.** The off-cycle reason, for an off-cycle payslip.

**Example.** End Of Service

**Why it matters.** Blank on regular payslips.

**Rules.** Required. Chosen from Payroll Reason Type master data.

## OffCyclePaymentDate {#SalaryRegister.OffCyclePaymentDate}
<!-- alias: loc:OffCyclePaymentDate, dom:SalaryRegister_OffCyclePaymentDate -->

**What it is.** The payment date of an off-cycle run.

**Example.** `2026-05-05`

**Rules.** Optional. Date value.

## Locked {#SalaryRegister.Locked}
<!-- alias: loc:Locked, dom:SalaryRegister_Locked -->

**What it is.** Whether the payslip is locked.

**Why it matters.** A locked payslip and its lines cannot be changed or deleted.

**Rules.** Yes or no.

---
helpId: pay.simulation-salary-register.edit
lang: en
title: Simulation Salary Registers — Create / Edit
entity: HCMv3.PayrollManagement.SimulationSalaryRegister
surfaces:
  - angular/src/app/admin/payroll-management/simulation-salary-registers/create-or-edit-simulation-salary-register-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.SimulationSalaryRegisters.Create]
---

## Overview

A payslip from a simulation -- a trial run that shows what payroll would pay without paying it. Simulation payslips are kept apart from real ones and are never locked.

## EmployeeId {#SimulationSalaryRegister.EmployeeId}
<!-- alias: loc:EmployeeId, dom:SimulationSalaryRegister_EmployeeId -->

**What it is.** The employee the payslip is for.

**Rules.** Required. Chosen from the employee list.

## ForPeriod {#SimulationSalaryRegister.ForPeriod}
<!-- alias: loc:ForPeriod, dom:SimulationSalaryRegister_ForPeriod -->

**What it is.** The month the pay is for.

**Example.** `2026-03-31`

**Why it matters.** For a retro recalculation this is an earlier month than **In Period**.

**Rules.** Required. Date value.

## InPeriod {#SimulationSalaryRegister.InPeriod}
<!-- alias: loc:InPeriod, dom:SimulationSalaryRegister_InPeriod -->

**What it is.** The month the pay is paid in -- the run's pay date.

**Example.** `2026-04-30`

**Rules.** Required. Date value.

## PayrollRunType {#SimulationSalaryRegister.PayrollRunType}
<!-- alias: loc:PayrollRunType, dom:SimulationSalaryRegister_PayrollRunType -->

**What it is.** Whether the payslip came from a regular or an off-cycle run.

**Example.** Regular

**Rules.** Required. One of: Regular, Off Cycle.

## PayrollResultType {#SimulationSalaryRegister.PayrollResultType}
<!-- alias: loc:PayrollResultType, dom:SimulationSalaryRegister_PayrollResultType -->

**What it is.** Which result this is for its month.

**Example.** Current

**Why it matters.** A regular run writes *Current*. When the same month is recalculated in a later run, the older payslips for that month become *Last* (the most recent before) and *Archive* (the rest).

**Rules.** Required. One of: Current, Last, Archive.

## PayrollReasonTypeId {#SimulationSalaryRegister.PayrollReasonTypeId}
<!-- alias: loc:PayrollReasonTypeId, dom:SimulationSalaryRegister_PayrollReasonTypeId -->

**What it is.** The off-cycle reason, for an off-cycle payslip.

**Example.** End Of Service

**Why it matters.** Blank on regular payslips.

**Rules.** Required. Chosen from Payroll Reason Type master data.

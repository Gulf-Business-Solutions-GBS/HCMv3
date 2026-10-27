---
helpId: tm.simulation-time-register.edit
lang: en
title: Simulation Time Registers — Create / Edit
entity: HCMv3.TimeManagement.SimulationTimeRegister
surfaces:
  - angular/src/app/admin/time-management/simulation-time-registers/create-or-edit-simulation-time-register-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.SimulationTimeRegisters.Create]
---

## Overview

A day from a trial time engine run.

Not written at present: no process fills these registers and nothing reads them. Payroll and the reports work from the daily time register.

## Employee {#SimulationTimeRegister.EmployeeId}
<!-- alias: loc:Employee, dom:SimulationTimeRegister_EmployeeId -->

**What it is.** The employee.

**Rules.** Required. Chosen from the employee list.

## DayDate {#SimulationTimeRegister.DayDate}
<!-- alias: loc:DayDate, dom:SimulationTimeRegister_DayDate -->

**What it is.** The day.

**Example.** `2026-04-14`

**Rules.** Required. Date value.

## DutyPeriod {#SimulationTimeRegister.DutyPeriodId}
<!-- alias: loc:DutyPeriod, dom:SimulationTimeRegister_DutyPeriodId -->

**What it is.** The duty period that applied.

**Rules.** Optional. Chosen from Duty Period master data.

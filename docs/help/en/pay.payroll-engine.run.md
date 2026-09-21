---
helpId: pay.payroll-engine.run
lang: en
title: Payroll Engine — Run
entity: HCMv3.PayrollManagement.Dto.RunPayrollEngineInput
surfaces:
  - angular/src/app/admin/payroll-management/payroll-engine/payroll-engine.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.PayrollEngine]
---

## Overview

Runs payroll for a payroll area, or for named employees, and shows the history of runs.

A run is refused unless the payroll area has a control record and there is a payroll calendar row for the month and year of the pay date. A **regular** run also needs the period released for payroll, with its month end on the pay date. An **off-cycle** run needs the period closed and at least one employee code.

The run works in the background; its progress and result appear in the run history below.

## PayrollAreaId {#RunPayrollEngineInput.PayrollAreaName}
<!-- alias: loc:PayrollAreaId, dom:PayrollEngine_PayrollArea -->

**What it is.** The payroll area to run.

**Example.** Kuwait Monthly

**Why it matters.** With no payroll area, nobody is processed.

**Rules.** Required. Maximum 256 characters.

## PayrollRunType {#RunPayrollEngineInput.PayrollRunType}
<!-- alias: loc:PayrollRunType, dom:PayrollEngine_RunType -->

**What it is.** A regular monthly run, or an off-cycle run for particular employees.

**Example.** Regular

**Why it matters.** Regular needs the period released for payroll; off-cycle needs it closed.

**Rules.** Required. One of: Regular, Off Cycle.

## EmployeeCode {#RunPayrollEngineInput.EmployeeCodes}
<!-- alias: loc:EmployeeCode, dom:PayrollEngine_EmployeeCodes -->

**What it is.** The codes of the employees to run, separated by commas. Blank runs everyone active in the payroll area.

**Example.** `1001, 1002`

**Why it matters.** Required for an off-cycle run.

**Rules.** Optional. Maximum 4000 characters.

:::admin
Unless the time setting *Enable Time Engine Autorun* is on, named employees are run even if they have left or are in another payroll area; each is still checked against their own area's control record.
:::

## PayInDate {#RunPayrollEngineInput.PayInDate}
<!-- alias: loc:PayInDate, dom:PayrollEngine_PayInDate -->

**What it is.** The date the pay is for -- the last day of the payroll month.

**Example.** `2026-04-30`

**Why it matters.** For a regular run it must be the month end of the period on the control record.

**Rules.** Required. Date value.

## OffCycleReason {#RunPayrollEngineInput.OffCycleReason}
<!-- alias: loc:OffCycleReason, dom:PayrollEngine_OffCycleReason -->

**What it is.** Why the off-cycle run is made: end of service, advance leave or leave encashment.

**Example.** Advance Leave

**Why it matters.** *Advance Leave* picks up paid leave set to *Pay In Advance* with a payment date in the month.

**Rules.** Required. Maximum 256 characters.

## OffCyclePaymentDate {#RunPayrollEngineInput.OffCyclePaymentDate}
<!-- alias: loc:OffCyclePaymentDate, dom:PayrollEngine_OffCyclePaymentDate -->

**What it is.** The date the off-cycle payment is made.

**Example.** `2026-05-05`

**Why it matters.** Required for an off-cycle run, and must fall after the closed period. The run is then booked to the month end of this date -- 5 May books to 31 May.

**Rules.** Optional. Date value.

:::admin
When the time setting *Enable Time Engine Autorun* is on, this date is ignored and the run is booked to the month after the closed period.
:::

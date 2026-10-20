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

Runs payroll for a payroll area, or for chosen employees of it, and shows each run's progress and history.

**Choose the payroll area first.** The screen then reads the area's payroll control record and fills in the rest: the **Payroll Status** strip shows the period (month, year, start and end dates) and what it is open for, the run type is set to match, and the pay-in date is filled and locked. A period that is **locked** is open for the **Regular** run (shown in green); a period that is **closed** (exit payroll done) is open for an **Off Cycle** run, booked into the following month (shown in yellow). A period that is neither reads **Data Correction**. With no control record the strip says "Payroll control record is empty" and nothing can be run.

**Run Payroll Engine** checks the entries, then asks you to confirm, naming the area, the period, the run type and how many employees will be run ("all N eligible employees of the area" when no codes are given). The run then works in the background: its progress, counters and per-employee results appear below, and the run is added to **Payroll Engine Run History**. Only one run can be in progress per payroll area at a time; different areas can run together.

- **Show Retro (n)** opens the retro dates of the area's employees; n is how many have a retro date before the current period. See the Payroll Retro Dates help.
- **Export to excel** downloads the per-employee results of the run shown below, with the same search and "only failed" filter you applied there.
- **Refresh** reloads the history.

A run is refused unless there is a payroll calendar row for the month and year of the pay-in date. A **regular** run needs the period locked and not closed. An **off-cycle** run needs the period closed and at least one employee code.

## PayrollAreaId {#RunPayrollEngineInput.PayrollAreaName}
<!-- alias: loc:PayrollAreaId, dom:PayrollEngine_PayrollArea -->

**What it is.** The payroll area to run.

**Example.** Kuwait Monthly

**Why it matters.** Choosing an area fills the period, run type and pay-in date from its control record, clears the employee codes and switches **Simulation** off. With no payroll area, nothing can be run.

**Rules.** Required. Maximum 256 characters.

## PayrollRunType {#RunPayrollEngineInput.PayrollRunType}
<!-- alias: loc:PayrollRunType, dom:PayrollEngine_RunType -->

**What it is.** A regular monthly run, or an off-cycle run for particular employees.

**Example.** Regular

**Why it matters.** It is set for you from the period status: Regular when the period is locked, Off Cycle when it is closed. You can change it, but the run is refused if the period is not in the matching state. Changing it clears the employee codes, because the employees on offer differ.

**Rules.** Required. One of: Regular, Off Cycle.

## EmployeeCode {#RunPayrollEngineInput.EmployeeCodes}
<!-- alias: loc:EmployeeCode, dom:PayrollEngine_EmployeeCodes -->

**What it is.** The codes of the employees to run, separated by commas. Blank runs every eligible employee of the payroll area. The button beside it opens **Select Employees**, which lists the eligible employees and whether each has already been processed.

**Example.** `1001, 1002`

**Why it matters.** Required for an off-cycle run. The picker is available once an area is chosen, and for an off-cycle run once a reason is chosen.

**Rules.** Optional. Maximum 4000 characters.

:::admin
Unless the time setting *Enable Time Engine Autorun* is on, named employees are run even if they have left or are in another payroll area; each is still checked against their own area's control record.
:::

## PayInDate {#RunPayrollEngineInput.PayInDate}
<!-- alias: loc:PayInDate, dom:PayrollEngine_PayInDate -->

**What it is.** The date the pay is for: the last day of the payroll month.

**Example.** `2026-04-30`

**Why it matters.** Filled in and read-only once the area has a control record: the period's end for a regular run, the following period's end for an off-cycle run, and the chosen month's end for a simulation on another period. It can be typed only when the area has no control record, and the run is then refused anyway.

**Rules.** Required. Date value.

## OffCycleReason {#RunPayrollEngineInput.OffCycleReason}
<!-- alias: loc:OffCycleReason, dom:PayrollEngine_OffCycleReason -->

**What it is.** Why the off-cycle run is made. The list comes from **Payroll Reason Types**; until that list has entries, End Of Service, Advance Leave and Leave Encashment are offered. The first reason is chosen for you when the run turns off-cycle.

**Example.** Advance Leave

**Why it matters.** The reason decides who can be run. *Advance Leave* offers active employees with paid leave set to *Pay In Advance*, or a leave encashment, paid in the off-cycle month; *Leave Encashment* offers only those with an encashment; *End Of Service* offers inactive employees with a Full and Final payment in that month and a separation that was not revoked. Changing the reason clears the employee codes.

**Rules.** Required. Maximum 256 characters.

## OffCyclePaymentDate {#RunPayrollEngineInput.OffCyclePaymentDate}
<!-- alias: loc:OffCyclePaymentDate, dom:PayrollEngine_OffCyclePaymentDate -->

**What it is.** The date the off-cycle payment is made.

**Example.** `2026-05-05`

**Why it matters.** It must fall inside the month after the closed period, and the date picker only offers those days. It starts on today when today is in that month and the payroll setting *Pick Offcycle Payment* is *Current Date*, otherwise on that month's end. Not shown when the time setting *Enable Time Engine Autorun* is on, because the engine then ignores it and books the run to the month after the closed period.

**Rules.** Optional. Date value.

## PayrollRunSimulation {#RunPayrollEngineInput.IsSimulation}
<!-- alias: loc:PayrollRunSimulation, dom:PayrollEngine_IsSimulation -->

**What it is.** A test run: the same calculation as a real run, so you can check the results before paying. Offered only when the payroll setting **Allow Simulation Run** is on, and only once a payroll area is chosen.

**Example.** Tick it, run the April payroll for three employees, and compare the results on **Simulation Salary Registers** before the real run.

**Why it matters.** A simulation writes only to **Simulation Salary Registers** (SPY038). It never touches live data: no salary register, loan recovery, fund, insurance, indemnity quota, carry-forward payment or leave mark is written. Before it runs, every earlier simulation of the same employees, for any period, is deleted, so that screen always shows each employee's latest test run. The period checks of a real run are not applied: a regular simulation runs even when the period is not locked.

**Rules.** Yes or no.

:::admin
The run still appears in the run history and the payroll log. When *Enable Time Engine Autorun* is on, the time engine runs for real as part of the simulation, as in the old system. The wage rules are the live ones, so a test shows what the real run would pay.
:::

## PayrollRunOtherPeriod {#RunPayrollEngineInput.SimulationOtherPeriod}
<!-- alias: loc:PayrollRunOtherPeriod, dom:PayrollEngine_SimulationOther -->

**What it is.** Which period the simulation is for: **Current** (the control record's period, the default) or **Other**, a month and year you type.

**Example.** Other, to test how March would have paid.

**Why it matters.** With **Other**, the month and year start on the current period, the screen shows that month's start and end dates from the area's payroll period, and the pay-in date becomes that month's end.

**Rules.** Yes or no.

## Month {#RunPayrollEngineInput.SimulationMonth}
<!-- alias: loc:Month, dom:PayrollEngine_SimulationMonth -->

**What it is.** The payroll month to simulate, when **Other** is chosen.

**Example.** 3

**Why it matters.** The month number of the area's payroll year, as on the control record, not necessarily the calendar month. A month the area's payroll period does not define is refused as an incorrect payroll period.

**Rules.** Required. Between 1 and 12. Whole number.

## Year {#RunPayrollEngineInput.SimulationYear}
<!-- alias: loc:Year, dom:PayrollEngine_SimulationYear -->

**What it is.** The payroll year to simulate, when **Other** is chosen.

**Example.** 2026

**Why it matters.** Together with the month, it decides the period dates and the pay-in date of the test run.

**Rules.** Required. Between 1900 and 9999. Whole number.

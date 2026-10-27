---
helpId: prj.project-cost-engine
lang: en
title: Project Cost Engine
surfaces:
  - angular/src/app/admin/project-management/project-cost-engine/project-cost-engine.component.html
audience: [admin]
permissions: [Pages.ProjectManagement.ProjectCostEngine]
---

## Overview

Spreads the employees' payroll cost over the projects they worked on, for a payroll period, and writes the result as project registers. It runs regular payroll only; an off-cycle run is not supported.

Choose a payroll area (and optionally employees), check the period, then press **Run Project Cost Engine**. You are asked to confirm, with the area, the period end and how many employees will be run. The run works in the background; its progress appears in the panel below and its per-employee results in **Project Logs**.

The engine refuses a period that is not the area's current, locked payroll period ("Payroll Control Record not locked or is not current."), and needs a payroll calendar row for the month. Only one Project Cost Engine run can be in progress at a time for your company, whatever the area.

**The log is shared.** Every run starts by emptying **Project Logs**, as in the old system, and the Cost Engine (costing file) and Post JV screens write their progress to the same log. Starting any of these runs clears what the others left there; export a log you need to keep first.

## Payroll Area {#prj.project-cost-engine.payroll-area}
<!-- alias:  -->

**What it is.** The payroll area to cost. Required, even when employee codes are typed.

**Example.** Kuwait Monthly

**Why it matters.** It supplies the period from its payroll control record.

## Employee Code {#prj.project-cost-engine.employee-codes}
<!-- alias:  -->

**What it is.** The codes of the employees to cost, separated by commas. Blank costs the payroll area's employees.

**Example.** `1001, 1002`

**Why it matters.** Typing codes makes the checks stricter. For each employee the engine checks that they had joined by the period end, that their first contract had started by then and that their last contract had not ended before the period began. When you run the whole area, an employee who fails one of these is skipped quietly. When you typed their code, the same case is an error in the log, because you asked for that employee by name.

## Select Employees {#prj.project-cost-engine.select-employees}
<!-- alias:  -->

**What it is.** The button beside **Employee Code**. It lists the active employees whose org assignment on the period end is in the payroll area, each **Processed** (a project register already exists for the period) or **Pending**.

**Example.** Open **Pending**, press **Select all**, then **Ok**.

**Why it matters.** The tabs count the whole list; search finds by code or name; **Select all** and **Unselect all** apply to every row the tab and search show. Available once an area is chosen and the period has an end date.

## Period {#prj.project-cost-engine.period}
<!-- alias:  -->

**What it is.** **Current** takes the month, year, start and end dates from the payroll area's control record. **Other** lets you type a payroll month and year, and works out the dates from the area's payroll period. The dates are read-only.

**Example.** Current: month 4, year 2026, 01/04/2026 - 30/04/2026.

**Why it matters.** The period end is the pay date the run uses. A period that is not locked shows a warning; the run needs the current, locked period.

## Run Project Cost Engine {#prj.project-cost-engine.run}
<!-- alias:  -->

**What it is.** Checks the entries, asks you to confirm and queues the run.

**Example.** "Project Cost Engine run has been queued. Progress appears in the run history below."

**Why it matters.** Refused while another run is in progress, naming who started it and when. Needs the right to run the Project Cost Engine.

## Stop Project Cost Engine {#prj.project-cost-engine.stop}
<!-- alias:  -->

**What it is.** Asks the running engine to stop, after you confirm.

**Why it matters.** The employee in progress is finished first, then the run stops.

## Clear Status {#prj.project-cost-engine.clear}
<!-- alias:  -->

**What it is.** Clears this engine's progress history and empties **Project Logs**, after you confirm.

**Why it matters.** Not allowed while a run is in progress. Because the log is shared, it also clears what the Cost Engine and Post JV screens left there.

## Project Logs {#prj.project-cost-engine.logs}
<!-- alias:  -->

**What it is.** One line per employee of the last run: code, name, time, status, message and period. **All**, **Successfull** and **Failed** filter it, with counts.

**Example.** Failed: "Contract EndDate on '31/03/2026'. Can't run Payroll for Date : '30/04/2026'"

**Why it matters.** The messages are the engine's own, in English.

## Export to excel {#prj.project-cost-engine.export}
<!-- alias:  -->

**What it is.** Downloads **Project Logs** with the filter you chose, every page.

**Example.** Choose **Failed** first to send the list of problems to HR.

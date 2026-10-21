---
helpId: tm.time-engine
lang: en
title: Time Engine
surfaces:
  - angular/src/app/admin/time-management/time-engine/time-engine.component.html
audience: [admin]
permissions: [Pages.TimeManagement.TimeEngine]
---

## Overview

Evaluates attendance into time results day by day, for the employees of a payroll area or for the employees you name, over a date range. Payroll then reads those results.

Before a run is queued the screen checks, in this order: an end date is given; the start date is not after it; a payroll area or at least one employee code is given; the payroll area's control record exists (with only codes, the area of the first code's org assignment today is used); the end date is not more than the allowed number of days (time setting "TE_MAX_LIMIT_DAYS") past the control record's month end; and the start date is not before the earliest time evaluation date (time setting "TIME_EVAL_EARLIEST_DATE"), which must be set.

The run works in the background. Only one run can be in progress per payroll area at a time; different areas can run together. Progress, per-employee results and the day-by-day log appear below, and each run is added to **Time Engine Run History**.

## Payroll Area {#tm.time-engine.payroll-area}
<!-- alias:  -->

**What it is.** The payroll area whose employees are evaluated: the active employees whose org assignment on the day is in this area.

**Example.** Kuwait Monthly

**Why it matters.** It also decides which run the progress panel below follows. Either an area or employee codes is required. When codes are typed they win: only those employees are run, whatever the area.

## Employee Code {#tm.time-engine.employee-codes}
<!-- alias:  -->

**What it is.** The codes of the employees to evaluate, separated by commas. Blank evaluates the payroll area.

**Example.** `1001, 1002`

**Why it matters.** Named employees are run even if they are inactive, so time can still be evaluated for someone who has left. Up to 4000 characters.

## Select Employees {#tm.time-engine.select-employees}
<!-- alias:  -->

**What it is.** The button beside **Employee Code**. It lists the employees whose org assignment in the chosen payroll area overlaps the date range (start date or today, to end date or the start date), whatever their status. Tick employees and press **Ok** to put their codes in the box.

**Example.** Open **Pending**, press **Select all**, then **Ok**.

**Why it matters.** **Processed** means payroll has already made a regular salary register for the area's current period: re-running that employee's time will show up in payroll as a retro difference. **Pending** means it has not. The tabs **All**, **Processed**, **Pending** and **Selected** count the whole list; search finds by code or name; **Select all** and **Unselect all** apply to every row the tab and search show, on every page. Available only once a payroll area is chosen.

## Start Date {#tm.time-engine.start-date}
<!-- alias:  -->

**What it is.** The first day to evaluate.

**Example.** `2026-04-01`

**Why it matters.** Optional: blank starts on the 1st of the current month. It may not be after the end date, nor before the earliest time evaluation date in Time Settings.

## End Date {#tm.time-engine.end-date}
<!-- alias:  -->

**What it is.** The last day to evaluate.

**Example.** `2026-04-30`

**Why it matters.** Required. It may not be more than the allowed number of days past the payroll control record's month end; the message names the latest date allowed.

## Run Time Engine {#tm.time-engine.run}
<!-- alias:  -->

**What it is.** Checks the entries and queues the run.

**Example.** "Time Engine run has been queued. Progress appears in the run history below."

**Why it matters.** Refused while another run of the same payroll area is in progress, with the name of the user who started it and when. Needs the right to run the Time Engine.

## Run Progress {#common.engine-run-progress}
<!-- alias:  -->

**What it is.** The live panel for the latest run: a progress gauge, the counts of employees (total, processed, failed, skipped), the time taken so far and an estimate of the time left, and a grid of the employees with their result and message. The grid has a search box and **All**, **Succeeded Only** and **Failed Only**.

**Example.** "120 of 400 employees processed", about 6 minutes remaining (an estimate only).

**Why it matters.** **Stop Run** asks the run to stop; it ends after the employee in progress. **Clear Progress** clears the progress of finished runs. When a run stops responding, for example after a server restart, the panel says so and **Abandon Interrupted Runs** marks it as abandoned so it no longer blocks a new run. These actions need the right to run the engine.

## Days {#tm.time-engine.days}
<!-- alias:  -->

**What it is.** The button on each employee row of the progress grid. It opens that employee's day-by-day log for the run: day, status, message and run time, with **All**, **Succeeded Only** and **Failed Only**.

**Example.** One of 30 days failed: that day shows Failed with its error message, and the employee is shown as failed.

**Why it matters.** A failed day does not stop the employee's other days, but the employee is reported **Failed** if any of their days failed. This is where to read why.

## Export to excel {#tm.time-engine.export}
<!-- alias:  -->

**What it is.** Downloads the day-by-day log of the run shown in the panel: one row per employee per day, with the panel's search and success/failure filter applied.

**Example.** Filter on **Failed Only** first to get just the days to fix.

## Time Engine Run History {#tm.time-engine.history}
<!-- alias:  -->

**What it is.** Every run: when, by whom, its status, how many records, how many succeeded and failed, and the parameters it was run with.

**Example.** 2026-05-01 08:15, admin, SUCCESS, 400, 398, 2.

**Why it matters.** While a run is in progress the list refreshes itself every 10 seconds.

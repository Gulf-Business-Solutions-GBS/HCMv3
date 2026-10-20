---
helpId: pay.payroll-retro-review
lang: en
title: Payroll Retro Dates
surfaces:
  - angular/src/app/admin/payroll-management/payroll-engine/payroll-retro-review-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.PayrollEngine]
---

## Overview

Opened with **Show Retro (n)** on the Payroll Engine. It lists the employees of the payroll area with the date from which the next payroll run will recalculate their pay (retro), so you can check it before running, and move or remove it.

The employees are the area's active employees, plus inactive ones whose separation has not been revoked or followed by a rejoin and whose final settlement is not yet done or falls in the current period. The period is the one on the area's control record (with no control record, the current month).

Anyone with the Payroll Engine screen can look; changing dates needs the right to run the payroll engine. Changes are kept in the dialog until you press **Save**, which asks you to confirm and then saves them all together, or none. Each saved change is written to the Retro Reason Log with its reason. As in the old system, the date is not checked against the period, the joining date or the earliest retro accounting date, so check it yourself.

## Status {#pay.payroll-retro-review.tabs}
<!-- alias:  -->

**What it is.** **Retro** when the employee's retro from date is before the current period's start, otherwise **No Retro**. The tabs **All**, **Retro**, **No Retro** and **Changed** show each group with a count; **Changed** holds the rows you have edited but not saved yet. A search box finds employees by code or name.

**Example.** "Retro (4)": four employees will be recalculated back into earlier months.

**Why it matters.** The number on **Show Retro (n)** is the Retro count.

## Retro From Date {#pay.payroll-retro-review.from-date}
<!-- alias:  -->

**What it is.** The date from which the next run recalculates the employee, set when data with an earlier effect date is changed. Blank when none is set.

**Example.** 01/02/2026, after a salary increase back-dated to February.

## Retro Months {#pay.payroll-retro-review.months}
<!-- alias:  -->

**What it is.** How many months back the recalculation goes: the months from the retro from date to the period's payroll date. 0 for an employee with no retro.

**Example.** 2 (February and March, recalculated in the April run)

## Update Selected {#pay.payroll-retro-review.update-selected}
<!-- alias:  -->

**What it is.** Opens the update panel for every ticked employee, to give them all one retro date and reason. The pencil on a row does the same for that employee alone, starting from the date shown.

**Example.** Tick five employees and set 01/03/2026 for all of them.

**Why it matters.** **Apply To Selected** (or **Apply** for one row) changes the rows in the dialog only and moves them to **Changed**; nothing is saved yet. Setting a date back to the saved one is no longer a change. **Total Selected** at the bottom counts the ticked rows.

## Retro Date {#pay.payroll-retro-review.retro-date}
<!-- alias: dom:RetroEditorDate -->

**What it is.** The new retro from date.

**Example.** 01/03/2026

**Why it matters.** Clear it to remove the retro for the employee: the next run then does not go back at all.

## Reason {#pay.payroll-retro-review.reason}
<!-- alias: dom:RetroEditorReason -->

**What it is.** Why the retro date is changed, up to 500 characters.

**Example.** Housing allowance correction approved by HR on 10/04/2026.

**Why it matters.** Required: neither Apply nor Save works without it. It is kept in the Retro Reason Log, and the last reason is shown in the list.

## Save {#pay.payroll-retro-review.save}
<!-- alias:  -->

**What it is.** Saves every change in **Changed**, after a confirmation that says how many employees are affected.

**Example.** "Retro date changes for 5 employee(s) will be saved."

**Why it matters.** Only rows whose date really differs from the saved one are written; with none, the save is refused with "No retro date changes found." The count on **Show Retro (n)** is refreshed afterwards.

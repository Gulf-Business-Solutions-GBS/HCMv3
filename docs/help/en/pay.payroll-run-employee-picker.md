---
helpId: pay.payroll-run-employee-picker
lang: en
title: Payroll Engine — Select Employees
surfaces:
  - angular/src/app/admin/payroll-management/payroll-engine/payroll-run-employee-picker-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.PayrollEngine]
---

## Overview

Opened with the button beside **Employee Code** on the Payroll Engine. It lists the employees the chosen run would process, so you can tick the ones to run instead of typing their codes. The top shows the payroll area, its payroll status and how many are selected.

Who is listed depends on the run. For a **regular** run: the active employees whose org assignment on the period's last day is in this payroll area and who had joined by then. For an **off-cycle** run it depends on the reason: *Advance Leave* lists active employees with leave paid in advance or a leave encashment in the off-cycle month, *Leave Encashment* those with an encashment, and *End Of Service* inactive employees with a Full and Final payment in that month and a separation that was not revoked. For a simulation on another period, the list is worked out for that period.

The list only guides you: the engine still applies its own checks to the codes it is sent.

## Payroll Status {#pay.payroll-run-employee-picker.status}
<!-- alias:  -->

**What it is.** Whether each employee has already been run: **Processed** when a salary register already exists for this run (for a regular run, a regular register for this period; for an off-cycle run, one for the same reason in the off-cycle month), otherwise **Pending**. The tabs **All**, **Processed**, **Pending** and **Selected** show each group, with a count.

**Example.** "Pending (12)": twelve employees still to run this month.

**Why it matters.** The counts are always for the whole list, whatever you have searched.

## Search {#pay.payroll-run-employee-picker.search}
<!-- alias:  -->

**What it is.** Finds employees by code or name within the tab you are on.

**Example.** `Al-Ali`

**Why it matters.** Press Enter or the search button to apply it.

## Select all {#pay.payroll-run-employee-picker.select-all}
<!-- alias:  -->

**What it is.** **Select all** ticks, and **Unselect all** clears, every employee that the current tab and search show, on every page, not only the page in view.

**Example.** Open **Pending**, press **Select all**: every employee not yet run is ticked.

**Why it matters.** Ticks are kept while you page, search and change tabs.

## Ok {#pay.payroll-run-employee-picker.ok}
<!-- alias:  -->

**What it is.** Puts the codes of the ticked employees into **Employee Code**, separated by commas, and closes the dialog. **Cancel** closes it and leaves the codes as they were.

**Example.** Three ticked employees give `1001,1002,1045`.

**Why it matters.** Codes you had typed that are not on the list are dropped when you press **Ok**; ticking nobody empties the box, which runs every eligible employee of a regular run.

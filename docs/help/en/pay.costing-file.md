---
helpId: pay.costing-file
lang: en
title: Cost Engine
surfaces:
  - angular/src/app/admin/payroll-management/costing-file/costing-file.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.CostingFile]
---

## Overview

Turns a period's salary registers into cost registers, split by the employees' accounts and cost elements, and shows the journal voucher (JV) that results, ready to be posted to Dynamics 365 Finance on **Post JV**.

The usual order is: choose the payroll area, run type and period, press **Generate Costing File** and wait for the run to finish, then press **Journal Voucher** to see the JV lines and **Validate JV** to check that debit equals credit. **Export costing file** downloads the same lines as a file.

**Generate Costing File** runs in the background: the status panel shows its progress, and **Show details** lists each employee's result. Only one finance run (Cost Engine or Post JV) can be in progress at a time; a run that has given no sign of life for 20 minutes no longer blocks a new one.

:::admin
The run's log is the same table the Project Cost Engine and Post JV use, and every run starts by emptying it, as in the old system. Starting any of them clears what the others left there.
:::

## Payroll Area {#pay.finance.payroll-area}
<!-- alias:  -->

**What it is.** The payroll area to cost or post. Required on the screen.

**Example.** Kuwait Monthly

**Why it matters.** It supplies the period from its payroll control record. An area without an FI company and an FI bank is marked in red: the costing file can still be generated, but its JV cannot be posted. The list cannot be changed while a run is in progress.

## Employee Code {#pay.finance.employee-code}
<!-- alias:  -->

**What it is.** The codes of the employees to include, separated by commas. Blank takes the payroll area. The button beside it lists the area's employees to tick.

**Example.** `1001, 1002`

**Why it matters.** Codes win over the area. In the list, a **Regular** run shows the active employees of the area on the period end, each **Processed** (a cost register already exists for the period and run type) or **Pending**. For an **Off Cycle** run the list is empty, as in the old system; type the codes instead. Changing the codes clears the JV shown.

## Payroll Run Type {#pay.finance.run-type}
<!-- alias:  -->

**What it is.** **Regular** or **Off Cycle**: which salary registers are costed or posted.

**Example.** Regular

**Why it matters.** On Post JV, a regular run posts the area as one journal (two, salary and provision, when *Separate Provision Posting* is on); an off-cycle run posts one journal per employee.

## Posting Date {#pay.finance.posting-date}
<!-- alias:  -->

**What it is.** The accounting date of the journal.

**Example.** `2026-04-30`

**Why it matters.** Filled with the period end when the box is empty. Required. Changing it clears the JV shown.

## Period {#pay.finance.period}
<!-- alias:  -->

**What it is.** **Current** takes the month, year, start and end dates from the payroll area's control record. **Other** lets you type a payroll month and year and works out the dates from the area's payroll period. The dates are read-only; the end date is the pay date used.

**Example.** Other, month 3, year 2026.

**Why it matters.** Changing the period clears the JV shown.

## Generate Costing File {#pay.costing-file.generate}
<!-- alias:  -->

**What it is.** Starts the cost run for the area or employees, in the background, writing their cost registers for the period.

**Example.** Generate, wait for "Completed", then press **Journal Voucher**.

**Why it matters.** Refused while another finance run is in progress, naming who started it and when. Needs the right to run the Cost Engine.

## Stop Process {#pay.finance.stop}
<!-- alias:  -->

**What it is.** Asks the run in progress to stop.

**Why it matters.** The cost run stops before the next employee. A posting run stops before its next journal (or next employee for an off-cycle run), never in the middle of a journal.

## Clear Status {#pay.finance.clear}
<!-- alias:  -->

**What it is.** Empties the run log and status, after you confirm. Not available while a run is in progress.

**Why it matters.** The log is shared with the Project Cost Engine and the other finance screen, so it is cleared for them too.

## Journal Voucher {#pay.finance.journal-voucher}
<!-- alias:  -->

**What it is.** Shows the JV built from the period's cost registers: one line per account and cost element, with company, account type, main account, description, cost elements 2 to 7, currency, debit, credit, offset account and billing type, and the total debit and total credit above.

**Example.** Total Debit 112,450.500, Total Credit 112,450.500: "JV validated successfully."

**Why it matters.** Line amounts are rounded to 3 decimals. On Post JV the preview is required: what is posted must be exactly what you previewed, so any change to the area, codes, run type, period or posting date clears it.

## Validate JV {#pay.finance.validate-jv}
<!-- alias:  -->

**What it is.** Builds the JV and checks it: there must be lines, and debit must equal credit.

**Example.** "Debit and Credit amount not matched. Please check JV."

**Why it matters.** As in the old system, both totals are rounded to whole units before they are compared, so a difference of a few fils does not count as a mismatch. Post JV applies the same check on the server before posting.

## Export costing file {#pay.costing-file.export}
<!-- alias:  -->

**What it is.** Downloads the JV lines as a spreadsheet, in the old system's column order (without billing type).

**Example.** Send it to finance to import by hand.

**Why it matters.** Needs the right to export the costing file.

## Run Progress {#pay.finance.run-status}
<!-- alias:  -->

**What it is.** The status panel of the latest finance run: its status (Running, Completed, Completed With Errors, Stopped By Operator or Interrupted), when and by whom it started, how many employees are done of the total, the current employee, an estimate of the time left, and the counts of all, successful and failed records.

**Example.** "120 of 400 employees processed"

**Why it matters.** It refreshes itself while a run is in progress.

## Show details {#pay.finance.show-details}
<!-- alias:  -->

**What it is.** Opens the run log: one line per employee (and, for posting, per journal header and line batch), with time, status and message. **All**, **Succeeded Only** and **Failed Only** filter it.

**Example.** A posting failure line naming the journals that had already reached Dynamics 365.

**Why it matters.** **Export to excel** downloads the log as filtered, every page.

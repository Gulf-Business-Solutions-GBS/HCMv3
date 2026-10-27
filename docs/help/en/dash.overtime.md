---
helpId: dash.overtime
lang: en
title: Overtime Dashboard
surfaces:
  - angular/src/app/admin/dashboards/overtime-dashboard/overtime-dashboard.component.html
audience: [end-user]
permissions: [Pages.Dashboards.Overtime]
---

## Overview

How much overtime people worked over a period, what it cost, and where it went: approved hours against what was claimed, the cost payroll paid, and the split by month, overtime type, department and employee. It opens on this year to date.

**Hours** come from the approved overtime entries whose month falls in the period. An entry still waiting for approval is not counted in the hours; it appears only in **Awaiting approval**. **Cost** comes from payroll, not from the hours: it is what each month's payroll paid under the wage type set on each overtime type, so a month payroll has not been run for has hours but no cost yet.

Click an overtime type to show every tile for that type only, a month to list its entries, a department to see the units inside it, and a name to show only that employee. Each choice appears as a chip in the toolbar; remove the chip to go back. A period longer than five years is shortened to the last five.

## Approved overtime hours {#dash.overtime.approved-hours}
<!-- alias: loc:OvertimeDashboard_ApprovedHours -->

**What it is.** The overtime hours approved for the period. The line below it gives the hours claimed. Click it for the list of entries.

**How it is calculated.** `Sum of approved hours of the approved entries dated in the period`; **claimed** = `sum of the hours requested on the same entries`.

- Rounded to one decimal.

**Example.** 1,284.5 approved hours · of 1,410 hours claimed.

**Why it matters.** It is the volume payroll pays for, and the starting point for every other figure.

## Approved of claimed {#dash.overtime.approved-of-claimed}
<!-- alias: loc:OvertimeDashboard_ApprovedOfClaimed -->

**What it is.** The share of the hours people claimed that was approved.

**How it is calculated.** `Approved hours ÷ claimed hours × 100`

- Rounded to one decimal. Shown as "—" when nothing was claimed.

**Example.** 91.1% — 1,284.5 of 1,410 hours.

**Why it matters.** A low figure means claims are routinely cut back, which is worth a word with the managers concerned.

## Employees with overtime {#dash.overtime.employees}
<!-- alias: loc:OvertimeDashboard_Employees -->

**What it is.** How many different employees had overtime approved.

**How it is calculated.** `Distinct employees with approved hours > 0`

- An employee with ten entries counts once. An entry approved at zero hours does not count.

**Example.** 146 employees.

**Why it matters.** Many hours spread over a few people is a workload problem; the same hours spread widely is usually seasonal.

## Hours per employee {#dash.overtime.average}
<!-- alias: loc:OvertimeDashboard_AveragePerEmployee -->

**What it is.** The average approved hours of each employee who had overtime.

**How it is calculated.** `Approved hours ÷ employees with overtime`

- Only employees who had overtime are counted, so it is not diluted by everyone who had none. Shown as "—" when nobody had overtime.

**Example.** 8.8 hours per employee.

**Why it matters.** Compare it with **Most overtime** to see whether a few people are far above the average.

## Overtime cost {#dash.overtime.cost}
<!-- alias: loc:OvertimeDashboard_Cost -->

**What it is.** What payroll paid for overtime in the period.

**How it is calculated.** `Sum of payroll amounts under each overtime type's wage type, from each month's regular payroll result`

- Only the result calculated in its own month is used (in-period equals for-period). A retroactive recalculation of an earlier month is not added again, so a month is never counted twice.
- The wage type is the one set on each overtime type; nothing is hard-coded.
- Shown with three decimals.

**Example.** 18,452.750.

**Why it matters.** It is the real money, including rates that differ by employee and by type, which hours alone cannot show.

## Awaiting approval {#dash.overtime.pending}
<!-- alias: loc:OvertimeDashboard_Pending -->

**What it is.** Overtime requests still waiting for approval, and the hours they ask for. Click it for the list.

**How it is calculated.** `Entries whose approval status is Open`; **hours** = `sum of hours requested`.

- Counted whatever month the request is for, not only the period, because a request left waiting is late whenever it was raised.

**Example.** 12 requests · 47 hours requested.

**Why it matters.** These hours will not be paid until someone approves them; clear them before the payroll run.

## Overtime hours by month {#dash.overtime.trend}
<!-- alias: loc:OvertimeDashboard_ByMonth -->

**What it is.** Approved hours in each month of the period, stacked by overtime type. Click a month to list its entries.

**How it is calculated.** `Sum of approved hours per month and overtime type`

- Every month of the period is shown, including months with no overtime.
- Each type keeps the same colour here and in **Overtime by type**.

**Example.** March: 180 normal-day hours and 64 weekend hours.

**Why it matters.** It shows seasonal peaks, and whether weekend or holiday overtime — the most expensive — is growing.

## Overtime by type {#dash.overtime.by-type}
<!-- alias: loc:OvertimeDashboard_ByType -->

**What it is.** How the approved hours split across overtime types. Click a type to show every tile for that type only.

**How it is calculated.** `Sum of approved hours per overtime type`, largest first; each type's cost comes from its own wage type.

**Example.** Normal day 72%, weekend 21%, public holiday 7%.

**Why it matters.** Types are paid at different rates, so the mix drives the cost as much as the hours.

## Overtime cost by month {#dash.overtime.cost-trend}
<!-- alias: loc:OvertimeDashboard_CostByMonth -->

**What it is.** What payroll paid for overtime in each month.

**How it is calculated.** The **Overtime cost** rule, per month: `sum of overtime wage-type amounts in that month's regular payroll result`

- A month with no cost is usually a month payroll has not been run for yet. When no month has a cost, the tile says so.

**Example.** February: 2,310.500.

**Why it matters.** Set it against the hours chart: cost rising faster than hours means more expensive overtime types or higher-paid people.

## Overtime by department {#dash.overtime.by-department}
<!-- alias: loc:OvertimeDashboard_ByDepartment -->

**What it is.** Approved hours, employees and cost for each department one level below the unit you are looking at. Click a department to see the units inside it.

**How it is calculated.** For each department: `sum of approved hours`, `distinct employees with overtime` and `overtime cost` of the employees in that department or any unit below it.

- An employee is placed by the department they were in on the last day of the period.
- People attached directly to the unit you are looking at, rather than to a unit below it, are shown on their own line.

**Example.** Operations: 610 hours · 58 employees · 8,904.000.

**Why it matters.** It shows which departments rely on overtime, and lets you follow it down to the team.

## Most overtime {#dash.overtime.top-employees}
<!-- alias: loc:OvertimeDashboard_TopEmployees -->

**What it is.** The ten employees with the most approved overtime hours in the period. Click a name to show every tile for that employee only.

**How it is calculated.** `Sum of approved hours per employee`, largest first; each with their overtime cost.

**Example.** 96 hours · 1,520.000.

**Why it matters.** Very high overtime for one person is a fatigue and cost risk, and often a sign the post needs another person.

## Overtime entries {#dash.overtime.entries}
<!-- alias: loc:OvertimeDashboard_Entries -->

**What it is.** The list behind a figure or a clicked month: each entry with its month, employee, department, type, and the hours claimed and approved. Click a row to open the employee.

**How it is calculated.** The same rule as the figure you clicked: **Approved overtime hours** lists the approved entries of the period, a clicked month lists that month's approved entries, and **Awaiting approval** lists the open requests.

**Example.** Click **Awaiting approval**: 12 rows, each with the hours requested.

**Why it matters.** It is the evidence under the numbers, and it downloads as XLSX with the same filters.

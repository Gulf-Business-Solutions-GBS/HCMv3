---
helpId: dash.leave
lang: en
title: Leave Dashboard
surfaces:
  - angular/src/app/admin/dashboards/leave-dashboard/leave-dashboard.component.html
audience: [end-user]
permissions: [Pages.Dashboards.Leave]
---

## Overview

Who is on leave today, who is late back, what is coming up in the next week, and how leave is spread over the months, the departments and the leave types.

The six numbers at the top are about **today**. The charts below are about the **period** you choose in **Filter** (this year to date by default). Click any number to see the people behind it; click a bar or a slice to narrow the whole dashboard to it.

Only full-day leave counts. Hourly absences are short permissions, not days away, and are left out of every figure. Only approved leave counts, except in **Awaiting approval**.

## On leave today {#dash.leave.on-leave-today}

<!-- alias: loc:LeaveDashboard_OnLeaveToday -->
**What it is.** How many employees are away today, and the list of them with the date each is expected back.

**How it is calculated.** `Employees with a leave where Start date ≤ today ≤ End date`

- Each employee counts once, even with two leaves on the same day.
- Only active employees, and only leave not already marked as returned.
- **Expected back** is the day after the leave's end date.
- "of N employees" under the number is the active employees on the dashboard.

**Example.** 12 of 835 employees — 12 people are on leave today.

**Why it matters.** It is the first answer to "who is in today", and the list tells you when each person returns.

## Not returned from leave {#dash.leave.not-returned}

<!-- alias: loc:LeaveDashboard_NotReturned -->
**What it is.** Employees whose leave has ended but who have not been recorded as back at work, longest overdue first.

**How it is calculated.** `Leaves of a type that requires a return date, where End date < today and no return is recorded`

- **Days overdue** = today − the leave's end date.
- The number counts employees; "· N leaves" under it counts leaves, because one person can be overdue from two.
- Only leave types with **Return date required** ticked are checked. This is the same rule payroll uses to treat someone as not returned.
- Not limited to the period: an overdue leave stays here until a return is recorded.

**Example.** 95 employees · 155 leaves.

**Why it matters.** Until the return is recorded, payroll may stop paying the employee. The list is what HR works through to record returns or chase people.

## Starting leave {#dash.leave.starting-soon}

<!-- alias: loc:LeaveDashboard_StartingSoon -->
**What it is.** Employees whose approved leave begins in the next seven days.

**How it is calculated.** `Leaves with tomorrow ≤ Start date ≤ today + 7 days`

**Example.** 4 — four people start leave within the week.

**Why it matters.** It gives managers a week's notice to arrange cover.

## Due back {#dash.leave.due-back}

<!-- alias: loc:LeaveDashboard_ReturningSoon -->
**What it is.** Employees on leave whose first day back is within the next seven days.

**How it is calculated.** `Leaves with today ≤ End date ≤ today + 6 days`

- Only leave types that require a return date, and only leave not yet marked as returned — the same leaves that would move to **Not returned from leave** if the return is not recorded.

**Example.** 3 — three people are due back this week.

**Why it matters.** It warns you who should be back soon, so a missed return is noticed on the first day rather than at payroll.

## Awaiting approval {#dash.leave.pending}

<!-- alias: loc:LeaveDashboard_PendingApproval -->
**What it is.** Leave requests that have been submitted and are still waiting for a decision.

**How it is calculated.** `Leave requests whose approval status is Open`

- This counts requests, not employees, and includes requests for any dates.

**Example.** 6 leave requests waiting.

**Why it matters.** A pending request is not yet leave: it is not in any other figure on this dashboard until it is approved.

## Leave days taken {#dash.leave.leave-days}

<!-- alias: loc:LeaveDashboard_LeaveDaysInPeriod -->
**What it is.** The total leave days that fall inside the period.

**How it is calculated.** `Sum of: Leave days × (days of the leave inside the period ÷ calendar days of the leave)`

- **Leave days** is the leave's own day count, which already leaves out weekends and holidays as its leave type says.
- A leave crossing the start or end of the period counts only the share inside it.
- A leave with no day count recorded counts its calendar days inside the period.
- Rounded to one decimal.

**Example.** A 10-day leave running 25 March to 7 April (14 calendar days, 7 in each month) counts 5 days in March and 5 in April.

**Why it matters.** It is the period's total leave cost in days, and the same figure the charts below break down.

## Leave by month {#dash.leave.trend}

<!-- alias: loc:LeaveDashboard_ByMonth -->
**What it is.** Leave spread across the period, one bar per month, split by leave type. For a period of two months or less it becomes one bar per day.

**How it is calculated.**

- **By month:** each month's bar = the **Leave days taken** formula, applied to that month.
- **By day:** each day's bar = `employees on leave that day`.

**Example.** Click **Mar 2026** and the dashboard narrows to March and shows its days; click **14 Mar** to list who was out that day.

**Why it matters.** It shows the busy months — school holidays, Ramadan, year end — so leave can be planned rather than discovered.

## Leave by department {#dash.leave.by-department}

<!-- alias: loc:LeaveDashboard_ByDepartment -->
**What it is.** Leave days and employees on leave per department, one organization level at a time.

**How it is calculated.**

- **Employees on leave** = employees with a leave inside the period.
- **Leave days** = the **Leave days taken** formula for those employees.
- An employee counts in the unit they are assigned to **today**, not the one they were in when they took the leave.
- "(own staff)" is people assigned directly to the unit you are in rather than to a unit below it. "No department" is people with no current assignment.

**Example.** Click **ABC Software** to see its departments; use the breadcrumb next to **Showing** to go back up.

**Why it matters.** It points to where leave is concentrated, down to the team.

## Leave by type {#dash.leave.by-type}

<!-- alias: loc:LeaveDashboard_ByType -->
**What it is.** How the period's leave days divide between leave types.

**How it is calculated.** `Leave days of each type ÷ all leave days in the period`, using the **Leave days taken** formula.

**Example.** Annual 70%, Sick 8% — click **Sick** to see only sick leave on every tile.

**Why it matters.** A rising share of sick or unpaid leave is often the first sign of a problem worth a conversation.

## Leave in the period {#dash.leave.leave-list}

<!-- alias: loc:LeaveDashboard_LeaveInPeriod -->
**What it is.** The list behind **Leave days taken**, or behind a day you clicked on the daily chart: every leave in it, newest first. Click a row to open the employee.

**How it is calculated.** `Approved full-day leaves that overlap the period` — or that cover the clicked day.

**Example.** Click **14 Mar** on the daily chart: the list shows the 9 leaves that covered that day.

**Why it matters.** It is the evidence under the numbers, and it downloads as XLSX with the same filters.

---
helpId: dash.attendance
lang: en
title: Time & Attendance Dashboard
surfaces:
  - angular/src/app/admin/dashboards/attendance-dashboard/attendance-dashboard.component.html
audience: [end-user]
permissions: [Pages.Dashboards.Attendance]
---

## Overview

How well people are keeping their working hours over a period: the attendance rate, absences, late arrivals, early departures, missing punches and punch requests — by month or day, by department, by weekday and by employee. It opens on this month.

Every figure comes from the **Time Engine's** results for each employee and day, the same results payroll uses. A period the Time Engine has not been run for has nothing to show, and the dashboard says so rather than showing zeros that look like perfect attendance.

**A working day** is a day the employee was scheduled to work: a planned working day that is not leave, a public holiday, a weekly off or a rest day. **Present** is a working day that is not absent. Lateness, early departure and missing punches are only looked for on present days. A period longer than two years is shortened to the last two.

## Attendance rate {#dash.attendance.attendance-rate}

<!-- alias: loc:AttendanceDashboard_AttendanceRate -->
**What it is.** The share of working days on which people turned up.

**How it is calculated.** `Present days ÷ working days × 100`

- Counted across every employee and every day of the period; one employee absent for one day out of 20 working days is 95%.
- Rounded to one decimal. Shown as "—" when nobody was due to work.

**Example.** 96.8% — 4,356 of 4,500 working days present.

**Why it matters.** It is the single figure to compare months and departments by, whatever their size.

## Absent days {#dash.attendance.absent}

<!-- alias: loc:AttendanceDashboard_AbsentDays -->
**What it is.** The working days on which someone was absent. Click it for the list.

**How it is calculated.** `Working days the Time Engine marked absent`

- When your company approves attendance before payroll (time register approval switched on), the **approved** absence is counted, not the engine's first result — the same rule payroll uses.

**Example.** 144 absent days in March.

**Why it matters.** Unpaid absence is deducted in payroll from these same days, so this list is what to check before the payroll run.

## Late arrivals {#dash.attendance.late}

<!-- alias: loc:AttendanceDashboard_LateArrivals -->
**What it is.** How many times people arrived after their shift start, and the hours lost. Click it for the list.

**How it is calculated.** `Present days with minutes late > 0`; **hours** = `sum of minutes late ÷ 60`.

- Minutes late are the Time Engine's, so any grace period in the time policy has already been applied. Flexi-time employees are never late.

**Example.** 212 late arrivals · 64.5 hours in total.

**Why it matters.** Repeated lateness is usually concentrated in a few people or a few days; the charts below show which.

## Early departures {#dash.attendance.early}

<!-- alias: loc:AttendanceDashboard_EarlyDepartures -->
**What it is.** How many times people left before their shift end, and the hours lost. Click it for the list.

**How it is calculated.** `Present days with minutes early > 0`; **hours** = `sum of minutes early ÷ 60`.

**Example.** 58 early departures · 21 hours in total.

**Why it matters.** Together with lateness it is the working time paid for but not worked.

## Missing punches {#dash.attendance.missing-punches}

<!-- alias: loc:AttendanceDashboard_MissingPunches -->
**What it is.** Present days where the employee punched only once. Click it for the list.

**How it is calculated.** `Present days with an out-punch but no in-punch, or an in-punch but no out-punch`

- A missing out-punch is not counted for today — the employee may simply not have left yet.

**Example.** 17 days with one punch only.

**Why it matters.** A day with one punch cannot be timed correctly. Employees can ask for the missing punch through self service; until then the day may be treated as short or absent.

## Punch requests {#dash.attendance.punch-requests}

<!-- alias: loc:AttendanceDashboard_PunchRequests -->
**What it is.** Punches employees asked for through self service because they forgot or could not punch, and how many are still waiting for approval. Click it for the list.

**How it is calculated.** `Punches with an approval request dated in the period`, whatever their status; **awaiting approval** = those still open.

**Example.** 23 punch requests · 5 awaiting approval.

**Why it matters.** A pending request is not yet a punch, so the Time Engine does not use it until it is approved.

## Attendance by month {#dash.attendance.trend}

<!-- alias: loc:AttendanceDashboard_ByMonth -->
**What it is.** Absent days, late arrivals and early departures side by side, one group per month. For a period of two months or less it becomes one group per day.

**How it is calculated.** Each bar uses the formula of its figure above, for that month or day.

**Example.** Click **Mar 2026** and the dashboard narrows to March and shows its days; click **14 Mar** to list that day's exceptions.

**Why it matters.** It shows whether attendance is improving and which days go wrong — the day after a holiday, the start of Ramadan.

## Attendance by department {#dash.attendance.by-department}

<!-- alias: loc:AttendanceDashboard_ByDepartment -->
**What it is.** The attendance rate of each department, lowest first, one organization level at a time, with its working days, absences and late arrivals.

**How it is calculated.** `Present days ÷ working days × 100` for the employees of the department.

- An employee belongs to the unit of their organization assignment in force at the end of the period.

**Example.** Click **ABC Software** to see which of its departments pulls the rate down.

**Why it matters.** A company-wide 97% can hide a department at 85%.

## Late arrivals and absences by weekday {#dash.attendance.weekday}

<!-- alias: loc:AttendanceDashboard_ByWeekday -->
**What it is.** Late arrivals and absent days for each day of the week.

**How it is calculated.** `Late days` and `absent days` of the period, grouped by the weekday they fell on.

**Example.** Sundays and Thursdays standing out.

**Why it matters.** A pattern tied to a weekday points to a cause — traffic, a weekly meeting, long weekends — that a monthly figure hides.

## Most days absent {#dash.attendance.top-absent}

<!-- alias: loc:AttendanceDashboard_TopAbsent -->
**What it is.** The ten employees with the most absent days in the period.

**How it is calculated.** `Absent days per employee`, largest first.

**Example.** Click a name to see every tile for that employee only; remove the chip to go back.

**Why it matters.** It is where a conversation about attendance usually starts.

## Most hours late {#dash.attendance.top-late}

<!-- alias: loc:AttendanceDashboard_TopLate -->
**What it is.** The ten employees who lost the most time to late arrival.

**How it is calculated.** `Sum of minutes late ÷ 60 per employee`, largest first.

**Example.** 6.5 hours over 13 late arrivals.

**Why it matters.** Hours rather than the number of late days shows the real cost: ten minutes a day adds up.

## Most hours left early {#dash.attendance.top-early}

<!-- alias: loc:AttendanceDashboard_TopEarly -->
**What it is.** The ten employees who lost the most time to leaving early.

**How it is calculated.** `Sum of minutes early ÷ 60 per employee`, largest first.

**Example.** 4 hours over 3 early departures.

**Why it matters.** It singles out the few people behind most early departures.

## Attendance exceptions {#dash.attendance.exceptions}

<!-- alias: loc:AttendanceDashboard_Exceptions -->
**What it is.** The list behind a figure or a clicked day: each employee-day with its planned and actual times and what went wrong. Click a row to open the employee. From **Punch requests** it lists the requested punches and their status instead.

**How it is calculated.** The same rule as the figure you clicked, so the list has exactly as many rows as the number said. A clicked day lists every exception of any kind on that day.

**Example.** Click **Late arrivals**: 212 rows, each with the planned and actual in-time.

**Why it matters.** It is the evidence under the numbers, and it downloads as XLSX with the same filters.

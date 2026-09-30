---
helpId: dash.workforce
lang: en
title: Workforce Dashboard
surfaces:
  - angular/src/app/admin/dashboards/workforce-dashboard/workforce-dashboard.component.html
audience: [end-user]
permissions: [Pages.Dashboards.Workforce]
---

## Overview

Who works here and how that is changing: headcount by age, gender, nationality, years of service, qualification, job title and department, and the joiners, leavers and turnover of the period.

Headcount and the profile charts are taken on one day, the **as-of date**: the end of the period, or today if the period ends later. Joiners, leavers and turnover count across the whole period. Click a bar to narrow every tile to those employees; the filters add up, so *Female · 31 - 40 · Kuwaiti* is three clicks.

**Who counts as employed on a day.** Someone who joined on or before that day and has not left yet — their last day in service, from a recorded separation, is that day or later. With no separation on file they count while active. An inactive employee with no separation has no known leaving date, so they are left out of every figure rather than guessed at.

## Headcount {#dash.workforce.headcount}

<!-- alias: loc:WorkforceDashboard_Headcount -->
**What it is.** How many employees are employed on the as-of date, with the split between men and women. Click it for the list.

**How it is calculated.** `Employees employed on the as-of date`

- **% male** = men ÷ headcount × 100, and the same for women, rounded to whole numbers. Employees with no gender recorded are in the headcount but in neither percentage.

**Example.** 835 — 62% male · 38% female.

**Why it matters.** Every other figure on the dashboard is a share of this one.

## Average age {#dash.workforce.average-age}

<!-- alias: loc:WorkforceDashboard_AverageAge -->
**What it is.** The mean age of the headcount on the as-of date.

**How it is calculated.** `Sum of completed years of age ÷ number of employees with a birth date`

- Completed years: a birthday later in the year has not counted yet, so someone born 1 July 1990 is 35 on 30 June 2026, not 36.
- Employees with no usable birth date are left out of the average.
- Rounded to one decimal.

**Example.** 38.4 years.

**Why it matters.** Together with **Age distribution** it shows where retirements and succession planning will be needed.

## Average service {#dash.workforce.average-service}

<!-- alias: loc:WorkforceDashboard_AverageService -->
**What it is.** The mean length of service of the headcount on the as-of date, and how many nationalities they hold.

**How it is calculated.** `Sum of completed years since joining date ÷ headcount`, rounded to one decimal.

- **Nationalities** = the number of different nationalities in the headcount.

**Example.** 6.2 years · Nationalities: 24.

**Why it matters.** Long average service means experience to keep; short means induction and training load.

## Joiners {#dash.workforce.joiners}

<!-- alias: loc:WorkforceDashboard_Joiners -->
**What it is.** Employees who joined during the period. Click it for the list.

**How it is calculated.** `Employees whose joining date is between the start of the period and the as-of date`

**Example.** 41 joiners this year to date.

**Why it matters.** Set against **Leavers**, it tells you whether the workforce is growing or shrinking.

## Leavers {#dash.workforce.leavers}

<!-- alias: loc:WorkforceDashboard_Leavers -->
**What it is.** Employees who left during the period, split into resignations and terminations. Click it for the list.

**How it is calculated.** `Employees whose last day in service is between the start of the period and the as-of date`

- Only recorded separations count, and a revoked separation does not.
- **Resigned** and **terminated** come from the separation type.

**Example.** 18 — 14 resigned · 4 terminated.

**Why it matters.** It is the top half of the turnover formula. Employees made inactive without a separation are not counted, so record separations to keep this true.

## Turnover % {#dash.workforce.turnover}

<!-- alias: loc:WorkforceDashboard_Turnover -->
**What it is.** The share of the workforce that left during the period.

**How it is calculated.** `Leavers ÷ average headcount × 100`

- **Average headcount** = (headcount on the first day of the period + headcount on the as-of date) ÷ 2, shown under the percentage.
- Rounded to one decimal. Shown as "—" when the average headcount is 0.

**Example.** 18 leavers ÷ ((820 + 835) ÷ 2) × 100 = 18 ÷ 827.5 × 100 = **2.2%**.

**Why it matters.** It is the standard HR measure of how many people you lose, comparable between departments and between years whatever their size.

:::admin
Legacy never produced a correct turnover: the ESS Turnover page ran on hard-coded sample employees, and the DevExpress HR Dashboard counted expired contract rows as resignations and divided integers. This figure is new; exits come only from unrevoked `EmployeeSeparation` rows.
:::

## Age distribution {#dash.workforce.age}

<!-- alias: loc:WorkforceDashboard_ByAge -->
**What it is.** The headcount in age groups: Under 21, 21 - 30, 31 - 40, 41 - 50 and Above 50, plus **Unknown** when anyone has no birth date.

**How it is calculated.** `Employees in each group`, by completed years of age on the as-of date.

**Example.** Click **31 - 40** and every tile shows only employees in their thirties.

**Why it matters.** It shows the age profile at a glance — a heavy **Above 50** bar means retirements coming.

## Gender {#dash.workforce.gender}

<!-- alias: loc:WorkforceDashboard_ByGender -->
**What it is.** The headcount split into male, female and not specified.

**How it is calculated.** `Employees of each gender ÷ headcount`

**Example.** Click **Female** to see the rest of the dashboard for women only.

**Why it matters.** It supports diversity reporting and any quota a regulator sets.

## Years of service {#dash.workforce.service}

<!-- alias: loc:WorkforceDashboard_ByService -->
**What it is.** The headcount in length-of-service groups: 0 - 4, 5 - 10, 11 - 15, 16 - 19, and 20 years and above.

**How it is calculated.** `Employees in each group`, by completed years since the joining date, on the as-of date.

**Example.** A tall **0 - 4 years** bar is a young workforce, or a lot of recent hiring.

**Why it matters.** Long-service groups drive end-of-service indemnity; short-service groups drive training.

## Nationality {#dash.workforce.nationality}

<!-- alias: loc:WorkforceDashboard_ByNationality -->
**What it is.** The headcount by nationality: the ten largest, then everyone else together as **Others**.

**How it is calculated.** `Employees of each nationality`, largest first.

**Example.** Click **Kuwaiti** to see every tile for Kuwaiti employees only. **Others** cannot be clicked, because it is many nationalities together.

**Why it matters.** It is the basis for workforce nationalisation targets and for visa and residency planning.

## Headcount by department {#dash.workforce.by-department}

<!-- alias: loc:WorkforceDashboard_ByDepartment -->
**What it is.** Headcount, leavers and turnover per department, one organization level at a time.

**How it is calculated.** Each unit uses the dashboard's own formulas, applied to the employees in it:

- **Headcount** = employed on the as-of date.
- **Leavers** = left during the period.
- **Turnover %** = `leavers ÷ ((headcount on the first day + headcount on the as-of date) ÷ 2) × 100`.
- An employee belongs to the unit of their organization assignment in force on the as-of date. "(own staff)" is people assigned directly to the unit you are in.

**Example.** Click **ABC Software** to see its departments and which one is losing people.

**Why it matters.** Company turnover can look healthy while one department loses a quarter of its staff.

## Joiners and leavers by month {#dash.workforce.movement}

<!-- alias: loc:WorkforceDashboard_ByMonth -->
**What it is.** One pair of bars per month of the period: how many joined and how many left.

**How it is calculated.** For each month: `joiners = joining dates in the month`, `leavers = last days in service in the month`. The first and last months are cut to the period.

**Example.** A spike of leavers in August after the summer.

**Why it matters.** It shows when people leave, which helps time retention efforts and hiring.

## Turnover by month {#dash.workforce.turnover-trend}

<!-- alias: loc:WorkforceDashboard_TurnoverTrend -->
**What it is.** The turnover percentage month by month.

**How it is calculated.** For each month: `leavers in the month ÷ ((headcount on its first day + headcount on its last day) ÷ 2) × 100`

**Example.** 0.4% most months, 1.8% in August.

**Why it matters.** A monthly line shows whether turnover is getting better or worse, which the period's single figure cannot.

## Qualification {#dash.workforce.qualification}

<!-- alias: loc:WorkforceDashboard_ByQualification -->
**What it is.** The headcount by highest qualification recorded: the ten largest, then **Others**. **Not recorded** is employees with no qualification on file.

**How it is calculated.** `Employees with each qualification`, largest first.

**Example.** Click **Bachelor** to narrow every tile to graduates.

**Why it matters.** It shows the skills base, and a large **Not recorded** bar shows where employee records need completing.

## Job title {#dash.workforce.job-title}

<!-- alias: loc:WorkforceDashboard_ByJobTitle -->
**What it is.** The headcount by job title: the ten largest, then **Others**.

**How it is calculated.** `Employees with each job title`, taken from their organization assignment in force on the as-of date.

**Example.** Click **Accountant** to see how accountants split by age, nationality and service.

**Why it matters.** It shows which roles hold the most people, and where one role depends on very few.

## Employees {#dash.workforce.employee-list}

<!-- alias: loc:WorkforceDashboard_EmployeesList -->
**What it is.** The list behind **Headcount**, **Joiners** or **Leavers**, with every filter applied: code, name, department, job title, nationality, gender, age, years of service, joining date, and for leavers the last day in service. Click a row to open the employee.

**How it is calculated.** The same rule as the figure you clicked, so the list always has exactly as many rows as the number said.

**Example.** Click **Leavers** after clicking **Female** on the gender chart: the women who left this year.

**Why it matters.** It is the evidence under the numbers, and it downloads as XLSX with the same filters.

## Employee movements {#dash.workforce.movements}
<!-- alias: loc:WorkforceDashboard_Movements -->

**What it is.** Five counts from legacy's Employee Status screen: pay on hold today, and who rejoined, left, was paid a final settlement or is still waiting for one. Click a bar to see who.

**How it is calculated.**
- **Pay on hold** = `employees with a pay hold whose start and end dates cover the as-of date`.
- **Rejoined** = `employees with a rejoin date in the period`.
- **Separated** = `employees whose last day in service, on an unrevoked separation, falls in the period` — the same people as **Leavers**.
- **Final settlement paid** = `employees with a final-settlement payment dated in the period`.
- **Awaiting final settlement** = `employees whose last day in service has passed, with no final-settlement payment since their separation was submitted and no rejoin since`. This one is not limited to the period: someone who left last year and was never settled still shows.

Each person counts once per bar. The unit and clicked-bar filters apply as on every other tile.

**Example.** Pay on hold 3 · rejoined 2 · separated 11 · final settlement paid 8 · awaiting final settlement 4.

**Why it matters.** Everyone under **Awaiting final settlement** is owed money; the list is the follow-up for payroll.

## Documents expiring {#dash.workforce.documents}
<!-- alias: loc:WorkforceDashboard_Documents -->

**What it is.** Documents of the employees working today — passports, civil IDs, residence permits and so on — that have expired or will soon, by document type. Click a bar segment for that type and status, a legend entry for one status across all types, or **Documents expired or expiring** for the whole list.

**How it is calculated.** For each employee and document type, only the **latest** document counts (the one with the latest expiry), so a renewed passport hides the old one.
- **Expired** = `expiry date before the as-of date`.
- **Expiring** = `expiry date from the as-of date up to the type's notification period ahead`; a type with no notification period set looks 30 days ahead.

Only approved documents count. People who have left are not included.

**Example.** Civil ID: 5 expired · 12 expiring (30 days).

**Why it matters.** An expired residence or work document is a legal risk; this is the renewal list.

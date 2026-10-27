---
helpId: dash.payroll
lang: en
title: Payroll Dashboard
surfaces:
  - angular/src/app/admin/dashboards/payroll-dashboard/payroll-dashboard.component.html
audience: [end-user]
permissions: [Pages.Dashboards.Payroll]
---

## Overview

What payroll paid and what the staff cost the company over a period: gross pay, deductions, net pay, the employer's social security and provisions, and the cost to company they add up to — by month, department, pay item, basic-salary band, payment method and payroll run. Clicking a payslip opens that employee's cost card. It opens on this year to date.

Every figure comes from the **payroll results**. Only the result calculated in its own month is used (in-period equals for-period). A retroactive recalculation of an earlier month is not added again: its difference is already paid as arrears in the later month's own result. **Off-cycle** runs are money paid too, so they count in the totals. A month payroll has not been run for has nothing to show.

**Gross pay** and **deductions** follow the payroll engine's own net-pay rule: every earning or deduction that affects net pay counts, whatever its code. **Employer cost** is what the company pays on top of gross: the employer's social security (PIFSS) share and the month's indemnity, leave and ticket provisions. A period longer than five years is shortened to the last five.

Several tiles are a snapshot of the **latest paid month**: the last month of the period with a regular payroll result. Their titles say which month that is.

## Employees paid {#dash.payroll.employees-paid}
<!-- alias: loc:PayrollDashboard_EmployeesPaid -->

**What it is.** How many employees were paid in the latest paid month. Click it for their payslips.

**How it is calculated.** `Distinct employees with a regular payroll result in the latest paid month`

- Off-cycle payments alone do not count an employee as paid.

**Example.** 742 · in September 2026.

**Why it matters.** It should match the number of people on the payroll; a drop usually means someone was left out of the run.

## Gross pay {#dash.payroll.gross}
<!-- alias: loc:PayrollDashboard_Gross -->

**What it is.** Everything employees earned in the period, before deductions.

**How it is calculated.** `Sum of every earning that affects net pay`

- Net pay itself, time types and provisions are not earnings. Arrears, overtime and off-cycle payments are.

**Example.** 1,284,560.250.

**Why it matters.** It is the salary bill before anything is taken off.

## Deductions {#dash.payroll.deductions}
<!-- alias: loc:PayrollDashboard_Deductions -->

**What it is.** Everything taken off pay in the period. The line below gives it as a share of gross.

**How it is calculated.** `Sum of every deduction that affects net pay`; **share** = `deductions ÷ gross × 100`

- The employer's social security share is not a deduction from pay: it does not touch net pay and is counted under **Employer cost**.

**Example.** 98,410.500 · 7.7% of gross pay.

**Why it matters.** A jump in the share points at new loans, absences or penalties.

## Net pay {#dash.payroll.net}
<!-- alias: loc:PayrollDashboard_Net -->

**What it is.** What was paid out to employees in the period.

**How it is calculated.** `Sum of the net-pay line of every payslip`

- A payslip without a net-pay line (some off-cycle runs) counts `gross − deductions` instead.

**Example.** 1,186,149.750.

**Why it matters.** It is the money that leaves the bank; it should agree with the bank transfer files.

## Employer cost {#dash.payroll.employer-cost}
<!-- alias: loc:PayrollDashboard_EmployerCost -->

**What it is.** What the company pays on top of gross pay.

**How it is calculated.** `Employer social security share + indemnity provision + leave provision + ticket provision`, each month's own amounts.

- These are the monthly provision amounts, not the running balances shown under **Provision balances**.

**Example.** 214,300.000.

**Why it matters.** It is the part of staff cost that never appears on a payslip.

## Cost to company {#dash.payroll.cost-to-company}
<!-- alias: loc:PayrollDashboard_CostToCompany -->

**What it is.** The full cost of the staff for the period. The line below gives the average cost of one employee for one month.

**How it is calculated.** `Gross pay + employer cost`; **per employee a month** = `cost to company ÷ number of regular payslips`

- An employee paid in all 9 months counts as 9 payslips, so the average is a true monthly figure.
- Shown as "—" when nobody was paid.

**Example.** 1,498,860.250 · 673.195 per employee a month.

**Why it matters.** It is the number for budgets and headcount decisions.

## Payroll cost by month {#dash.payroll.trend}
<!-- alias: loc:PayrollDashboard_ByMonth -->

**What it is.** Each month's net pay, deductions and employer cost, stacked. The full height of a bar is that month's cost to company. Click a month to list its payslips.

**How it is calculated.** For each month: `net pay`, `deductions` and `employer cost` by the rules above; `net + deductions = gross`, so the stack is `gross + employer cost`.

- Every month of the period is shown, including months payroll has not been run for.

**Example.** March: 131,200 net · 10,900 deductions · 23,800 employer cost.

**Why it matters.** It shows the months with bonuses, arrears or a hiring wave at a glance.

## Cost make-up {#dash.payroll.cost-mix}
<!-- alias: loc:PayrollDashboard_CostMix -->

**What it is.** What the cost to company is made of.

**How it is calculated.** **Basic salary** = `sum of the basic-salary line`; **allowances and other pay** = `gross − basic salary`; then the employer's social security and each provision. Together they add up to the cost to company.

- Parts with no amount are left out.

**Example.** Basic 62%, allowances and other pay 24%, social security 6%, provisions 8%.

**Why it matters.** Indemnity and leave are worked out from pay, so a larger basic share means larger provisions later.

## Payroll by department {#dash.payroll.by-department}
<!-- alias: loc:PayrollDashboard_ByDepartment -->

**What it is.** Employees, gross, net and cost to company for each department one level below the unit you are looking at, highest cost first. Click a department to see the units inside it.

**How it is calculated.** The rules above, over the payslips of the employees in that department or any unit below it; **employees** = `distinct employees paid in the period`.

- An employee is placed by the department they were in on the last day of the period.
- People attached directly to the unit you are looking at are shown on their own line.

**Example.** Operations: 410 employees · 812,300.000 cost to company.

**Why it matters.** It shows where the salary bill sits, and lets you follow it down to the team.

## Employees by basic salary {#dash.payroll.basic-bands}
<!-- alias: loc:PayrollDashboard_BasicBands -->

**What it is.** How many employees fall in each basic-salary band in the latest paid month. Click a band to see who is in it.

**How it is calculated.** `Basic salary on each employee's regular payslip`, counted in bands: under 300, 300 – 500, 500 – 1,000, 1,000 – 1,500, 1,500 – 3,000, 3,000 and above.

- Each band includes its lower figure and stops just below the next, so every salary falls in exactly one band. Someone with no basic line is in the first band.

**Example.** 500 – 1,000: 318 employees.

**Why it matters.** It shows the pay structure at a glance and where most of the staff sit.

## Largest earnings {#dash.payroll.earnings}
<!-- alias: loc:PayrollDashboard_Earnings -->

**What it is.** The ten pay items that cost the most in the period.

**How it is calculated.** `Sum of each earning's amounts`, largest first, with the number of employees who received it.

**Example.** Housing allowance (1002): 184,000.000 · 690 employees.

**Why it matters.** After basic salary, a few allowances usually make up most of the bill; this names them.

## Largest deductions {#dash.payroll.deduction-types}
<!-- alias: loc:PayrollDashboard_DeductionTypes -->

**What it is.** The ten deductions that took the most from pay in the period.

**How it is calculated.** `Sum of each deduction's amounts`, largest first, with the number of employees it was taken from.

**Example.** Employee PIFSS share (1): 41,200.000 · 120 employees.

**Why it matters.** Loans, absence and penalty deductions that grow month after month show up here first.

## Payment method {#dash.payroll.payment-methods}
<!-- alias: loc:PayrollDashboard_PaymentMethods -->

**What it is.** How the employees paid in the latest paid month receive their pay. Click a method to see who.

**How it is calculated.** For each employee paid that month: `the payment type on their bank details in force at the end of the month`, counted by bank transfer, cash, and **not set**.

- **Not set** means no bank details cover that date: these people cannot be in the bank file.

**Example.** Bank transfer 721 · cash 15 · not set 6.

**Why it matters.** Anyone under **Not set** needs bank details before the next salary transfer.

## Regular and off-cycle {#dash.payroll.run-types}
<!-- alias: loc:PayrollDashboard_RunTypes -->

**What it is.** Net pay split between the regular monthly runs and off-cycle runs (final settlements, advances and other one-off payments). Click a run type to see its payslips.

**How it is calculated.** `Sum of net pay per run type`, with the number of payslips and employees.

**Example.** Regular 1,150,000.000 · off-cycle 36,149.750 over 22 payments.

**Why it matters.** A large off-cycle share means much is paid outside the normal run, which is harder to control.

## Provision balances {#dash.payroll.provisions}
<!-- alias: loc:PayrollDashboard_Provisions -->

**What it is.** What the company owes the staff at the end of the latest paid month for end-of-service indemnity, leave and air tickets.

**How it is calculated.** `Sum of each balance line on the latest paid month's regular payslips`

- These are running totals, so they are taken from one month only; adding months together would count the same liability again.

**Example.** Indemnity 2,410,000.000 · leave 380,500.000 · tickets 92,000.000.

**Why it matters.** It is the liability for the balance sheet, and what final settlements will draw on.

## Payslips {#dash.payroll.payslips}
<!-- alias: loc:PayrollDashboard_Payslips -->

**What it is.** The list behind a figure: one row per payslip with its month, employee, department, run type, basic, gross, deductions, net, cost to company and payment method. Click a row for that employee's cost card.

**How it is calculated.** The same rule as the figure you clicked: a month lists that month's payslips (both run types), a band or payment method lists the latest paid month's regular payslips, and a run type lists that run type's payslips for the whole period.

**Example.** Click **Employees paid**: every payslip of the latest paid month.

**Why it matters.** It is the evidence under the numbers, and it downloads as XLSX with the same filters.

## Cost to company card {#dash.payroll.cost-card}
<!-- alias: loc:PayrollDashboard_CostCard -->

**What it is.** One employee's cost to the company for the month of the payslip you clicked, line by line: each earning, each deduction and each employer cost, with the indemnity balance and the days behind it. The filter button shows every tile for this employee only.

**How it is calculated.** From that month's regular payslip: **cost to company** = `gross + employer cost`; **a year at this rate** = `cost to company × 12`.

- For an off-cycle row the card shows the regular payslip of the same month.

**Example.** Gross 1,250.000 · employer cost 196.500 · cost to company 1,446.500 · a year at this rate 17,358.000.

**Why it matters.** It answers "what does this person really cost us", which a payslip does not show.

---
helpId: pa.employee-rejoin.edit
lang: en
title: Employee Rejoins — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeRejoin
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/rejoins/create-or-edit-employee-rejoin-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeRejoins.Create]
---

## Overview

Records that a separated employee has come back, and brings them back in one of two ways, chosen with **Create New Employee Code**:

- **Unticked: the same employee returns.** The employee record is made Active again under its old code. Every balance whose carry-forward box is not ticked is set to zero on the day before the rejoin date.
- **Ticked: a new employee is created.** The person gets a new employee record and a new code, joining on the rejoin date. The balances whose boxes are ticked are opened on the new record. The old record stays exactly as it was.

Everything happens on save, together: if any part is refused, nothing is changed. Once the rejoin date has been reached, payroll only takes separations whose last day in service is on or after the rejoin date into account, so the earlier exit no longer stops pay.

A rejoin can no longer be added, changed or deleted once a later exit, Full and Final payment, regular payroll run or another rejoin exists for the employee from the rejoin date on. A rejoin that created a new employee can never be changed or deleted. Changing or deleting a rejoin that kept the old code takes the employee back to Inactive and removes the zeroing records it wrote, before the new values are applied.

:::admin
Only the annual leave type with code 1 and the ticket type with code 1 are carried forward or zeroed, and only from the employee's first quota row of each. Indemnity uses the first indemnity quota row. The zeroing records carry the comment "Employee Rejoined with Nill Quota Balance" (loans: "Nill Loan Balance"). Rejoins saved before this behaviour existed were not re-run.
:::

## RejoinDate {#EmployeeRejoin.RejoinDate}
<!-- alias: loc:RejoinDate, dom:EmployeeRejoin_RejoinDate -->

**What it is.** The date the employee rejoined.

**Example.** 1 Feb 2027

**Why it matters.** From this date payroll ignores separations that ended before it. With a new employee code it is also the new record's joining date; with the old code, balances that are not carried forward are zeroed on the day before it (31 Jan 2027 in the example).

**Rules.** Required. Date value.

## CreateNewEmployeeCode {#EmployeeRejoin.CreateNewEmployeeCode}
<!-- alias: loc:CreateNewEmployeeCode, dom:EmployeeRejoin_CreateNewEmployeeCode -->

**What it is.** Whether the person comes back as a new employee with a new code, instead of reactivating the old record.

**Example.** Ticked for a former employee with code 1045 who is re-hired on a fresh contract: a new employee, say 2310, is created, with 1045 in its **Previous Employee Code**.

**Why it matters.** The blue note under the box changes with it, so you can see which way the save will go.

When **ticked**:

- A new, Active employee is created with the next code from the number series of the old employee's employment type, joining on the rejoin date. Probation is zero days.
- Names, title, birth date and birth place, nationality, religion, gender, marital status, country, phones, addresses, qualification, blood group, Civil ID and email are copied from the old record.
- Org assignment, salary, bank, documents, photo, passport and login are **not** copied. Enter them on the new employee as for any new joiner.
- Each ticked balance is opened on the new employee (see the carry-forward boxes). Unticked balances are simply not brought over.
- The old employee is not changed. The rejoin list shows the new employee's code as a link in **New Employee Code**.

When **unticked**, the same employee is made Active again, and every balance whose box is not ticked is zeroed on the day before the rejoin date.

Either way the save counts against your subscription's maximum number of active employees, and is refused when that limit has been reached. If leave is carried forward to a new code, a Time Transfer Type with code 1 must exist under **Time Transfer Types**, or the save is refused.

**Rules.** Yes or no.

## CarryForwardLeaveBalance {#EmployeeRejoin.CarryForwardLeaveBalance}
<!-- alias: loc:CarryForwardLeaveBalance, dom:EmployeeRejoin_CarryForwardLeaveBalance -->

**What it is.** Whether the employee's annual leave balance comes back with them.

**Example.** A balance of 12.5 days at separation, ticked: the employee returns with 12.5 days.

**Why it matters.** With a **new code**, ticked opens the old balance on the new employee as a time transfer on the rejoin date (it needs Time Transfer Type code 1). With the **old code**, unticked writes an absence adjustment on the day before the rejoin date that brings the balance to zero; ticked leaves it as it is.

**Rules.** Yes or no.

## CarryForwardTicketBalance {#EmployeeRejoin.CarryForwardTicketBalance}
<!-- alias: loc:CarryForwardTicketBalance, dom:EmployeeRejoin_CarryForwardTicketBalance -->

**What it is.** Whether the employee's ticket balance comes back with them.

**Example.** 1 ticket outstanding at separation, ticked: the employee returns with 1 ticket.

**Why it matters.** With a **new code**, ticked gives the new employee a ticket quota equal to the old balance, entitled from the rejoin date, from which tickets can be deducted only 9 months after joining. With the **old code**, unticked writes a ticket adjustment on the day before the rejoin date that brings the balance to zero; ticked leaves it as it is.

**Rules.** Yes or no.

## CarryForwardIndemnityBalance {#EmployeeRejoin.CarryForwardIndemnityBalance}
<!-- alias: loc:CarryForwardIndemnityBalance, dom:EmployeeRejoin_CarryForwardIndemnityBalance -->

**What it is.** Whether the indemnity (end of service) days the employee had built up come back with them.

**Example.** 45 indemnity days at separation, ticked: the service before the exit still counts.

**Why it matters.** With a **new code**, ticked gives the new employee an indemnity quota equal to the old balance, from the rejoin date, usable only 9 months after joining. With the **old code**, unticked writes an indemnity record, with reset ticked, dated the day before the rejoin date, that settles the whole balance; ticked leaves the balance as it is.

**Rules.** Yes or no.

## CarryForwardLoanBalance {#EmployeeRejoin.CarryForwardLoanBalance}
<!-- alias: loc:CarryForwardLoanBalance, dom:EmployeeRejoin_CarryForwardLoanBalance -->

**What it is.** Whether loans still owed at separation are recovered after the employee returns.

**Example.** Two personal loans with 300 and 200 KWD still due, ticked: one personal loan of 500 KWD on the new employee.

**Why it matters.** With a **new code**, ticked creates one loan per loan type on the new employee, combining the amounts still due and the instalments of the old loans, starting on the rejoin date and running for 2 years. With the **old code**, unticked records an external recovery of the full amount due on each open loan, dated that loan's end date, so payroll stops deducting it; ticked leaves the loans running.

**Rules.** Yes or no.

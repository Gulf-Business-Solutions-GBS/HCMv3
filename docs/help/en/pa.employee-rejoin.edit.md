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

Records that a separated employee has come back.

Once a rejoin date has been reached, payroll only takes separations whose last day in service is on or after the rejoin date into account, so an earlier exit no longer stops pay. The carry-forward options are recorded for reference; balances are not carried forward automatically.

## RejoinDate {#EmployeeRejoin.RejoinDate}
<!-- alias: loc:RejoinDate, dom:EmployeeRejoin_RejoinDate -->

**What it is.** The date the employee rejoined.

**Example.** 1 Feb 2027

**Why it matters.** From this date payroll ignores separations that ended before it.

**Rules.** Required. Date value.

## CarryForwardLeaveBalance {#EmployeeRejoin.CarryForwardLeaveBalance}
<!-- alias: loc:CarryForwardLeaveBalance, dom:EmployeeRejoin_CarryForwardLeaveBalance -->

**What it is.** Whether the leave balance should carry forward.

**Why it matters.** Recorded only; the balance is not carried forward automatically.

**Rules.** Yes or no.

## CarryForwardTicketBalance {#EmployeeRejoin.CarryForwardTicketBalance}
<!-- alias: loc:CarryForwardTicketBalance, dom:EmployeeRejoin_CarryForwardTicketBalance -->

**What it is.** Whether the ticket balance should carry forward.

**Why it matters.** Recorded only; the balance is not carried forward automatically.

**Rules.** Yes or no.

## CarryForwardIndemnityBalance {#EmployeeRejoin.CarryForwardIndemnityBalance}
<!-- alias: loc:CarryForwardIndemnityBalance, dom:EmployeeRejoin_CarryForwardIndemnityBalance -->

**What it is.** Whether the indemnity balance should carry forward.

**Why it matters.** Recorded only; the balance is not carried forward automatically.

**Rules.** Yes or no.

## CarryForwardLoanBalance {#EmployeeRejoin.CarryForwardLoanBalance}
<!-- alias: loc:CarryForwardLoanBalance, dom:EmployeeRejoin_CarryForwardLoanBalance -->

**What it is.** Whether the loan balance should carry forward.

**Why it matters.** Recorded only; the balance is not carried forward automatically.

**Rules.** Yes or no.

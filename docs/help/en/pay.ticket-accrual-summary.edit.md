---
helpId: pay.ticket-accrual-summary.edit
lang: en
title: Ticket Accrual Summaries — Create / Edit
entity: HCMv3.PayrollManagement.TicketAccrualSummary
surfaces:
  - angular/src/app/admin/payroll-management/ticket-accrual-summaries/create-or-edit-ticket-accrual-summary-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.TicketAccrualSummaries.Create]
---

## Overview

A monthly snapshot of an employee's air ticket entitlement.

Not written at present: no process fills these summaries, and nothing reads them. Balances are worked out when they are needed, from the time register and the employee's records.

## EmployeeId {#TicketAccrualSummary.EmployeeId}
<!-- alias: loc:EmployeeId, dom:TicketAccrualSummary_EmployeeId -->

**What it is.** The employee.

**Rules.** Required. Chosen from the employee list.

## Year {#TicketAccrualSummary.Year}
<!-- alias: loc:Year, dom:TicketAccrualSummary_Year -->

**What it is.** The year of the snapshot.

**Example.** `2026`

**Rules.** Optional. Whole number.

## Month {#TicketAccrualSummary.Month}
<!-- alias: loc:Month, dom:TicketAccrualSummary_Month -->

**What it is.** The month of the snapshot.

**Example.** `4`

**Rules.** Optional. Whole number.

## TicketAccrued {#TicketAccrualSummary.TicketAccrued}
<!-- alias: loc:TicketAccrued, dom:TicketAccrualSummary_TicketAccrued -->

**What it is.** Tickets accrued to date.

**Example.** `0.33`

**Rules.** Optional. Number.

## TicketUsed {#TicketAccrualSummary.TicketUsed}
<!-- alias: loc:TicketUsed, dom:TicketAccrualSummary_TicketUsed -->

**What it is.** Tickets used to date.

**Example.** `0`

**Rules.** Optional. Number.

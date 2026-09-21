---
helpId: pay.employee-sub-group-ticket.edit
lang: en
title: Employee Sub Group Tickets — Create / Edit
entity: HCMv3.PayrollManagement.EmployeeSubGroupTicket
surfaces:
  - angular/src/app/admin/payroll-management/employee-sub-group-detail/tickets/create-or-edit-employee-sub-group-ticket-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.EmployeeSubGroupTickets.Create]
---

## Overview

The sub group's air ticket entitlement for one ticket type -- the employee's own ticket or a dependant's: how many tickets, over what period, and how each ticket is valued.

How a ticket is valued is chosen on the sub group itself (**Air Ticket Pay Type**); this row supplies the amounts that choice uses.

## TicketType {#EmployeeSubGroupTicket.TicketTypeId}
<!-- alias: loc:TicketType, dom:EmployeeSubGroupTicket_TicketTypeId -->

**What it is.** The ticket type this entitlement is for.

**Example.** Employee Ticket

**Why it matters.** Ticket type `1` is the employee's own ticket; the others are for dependants.

**Rules.** Optional. Chosen from a list.

## TicketCount {#EmployeeSubGroupTicket.TicketCount}
<!-- alias: loc:TicketCount, dom:EmployeeSubGroupTicket_TicketCount -->

**What it is.** How many tickets are earned in each accrual period.

**Example.** `1`

**Why it matters.** The time engine accrues it day by day -- over one year, or two years for a *Bi-Annual* period -- unless the ticket type is entitled immediately. Automatic ticket pay uses it as the entitlement.

**Rules.** Optional. Whole number.

## MaxAmount {#EmployeeSubGroupTicket.MaxAmount}
<!-- alias: loc:MaxAmount, dom:EmployeeSubGroupTicket_MaxAmount -->

**What it is.** The most one ticket can be worth.

**Example.** `250`

**Why it matters.** *ActualAmountWithinMaxlimit* never goes above it, and *Actual Amount* uses it when nothing was actually paid.

**Rules.** Optional. Number.

## FixAmount {#EmployeeSubGroupTicket.FixAmount}
<!-- alias: loc:FixAmount, dom:EmployeeSubGroupTicket_FixAmount -->

**What it is.** The value of one ticket, for the *Fix Amount* pay type.

**Example.** `180`

**Rules.** Optional. Number.

## FareType {#EmployeeSubGroupTicket.FareType}
<!-- alias: loc:FareType, dom:EmployeeSubGroupTicket_FareType -->

**What it is.** Whether the one-way or the return fare is used, for the *FareAmount* pay type.

**Example.** Return

**Rules.** Required. One of: OneWay, Return.

## AgeLimit {#EmployeeSubGroupTicket.AgeLimit}
<!-- alias: loc:AgeLimit, dom:EmployeeSubGroupTicket_AgeLimit -->

**What it is.** For a dependant ticket, the oldest age at which the dependant is entitled.

**Example.** `21`

**Why it matters.** Automatic ticket pay skips dependants older than this, and dependants with no birth date recorded, unless they have a ticket exception.

**Rules.** Optional. Number.

:::admin
An age limit of 0 means no dependant qualifies -- it is not "no limit". Set the oldest age that is entitled.
:::

## AccrualPeriod {#EmployeeSubGroupTicket.AccrualPeriod}
<!-- alias: loc:AccrualPeriod, dom:EmployeeSubGroupTicket_AccrualPeriod -->

**What it is.** The period the ticket entitlement runs over.

**Example.** Work Year

**Why it matters.** Automatic ticket pay adds the ticket on the first day of each period. *Bi-Annual* spreads the accrual over two years.

**Rules.** Required. One of: No Accrual, Calendar Year, Fiscal Year, Work Year, Bi-Annual, Per Employment, Custom.

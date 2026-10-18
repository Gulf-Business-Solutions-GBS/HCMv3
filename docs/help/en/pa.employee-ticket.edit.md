---
helpId: pa.employee-ticket.edit
lang: en
title: Employee Tickets — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeTicket
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/tickets/create-or-edit-employee-ticket-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeTickets.Create]
---

## Overview

An air ticket entitlement taken or paid for the employee or a dependant.

When the employee's grade pays air tickets automatically, the time engine creates these records itself on the day each ticket's accrual period starts. Payroll's average ticket calculation reads the tickets on the latest payment date. HCMv3 payroll does not currently calculate an air ticket payment from these records.

## TicketTypeId {#EmployeeTicket.TicketTypeId}
<!-- alias: loc:TicketTypeId, dom:EmployeeTicket_TicketTypeId -->

**What it is.** Whose ticket it is -- the employee's own or a dependant ticket type.

**Example.** Employee

**Rules.** Required. Chosen from Ticket Type master data.

:::admin
Maintained under Payroll Management → Ticket Types.
:::

## TicketDate {#EmployeeTicket.TicketDate}
<!-- alias: loc:TicketDate, dom:EmployeeTicket_TicketDate -->

**What it is.** The date of the ticket.

**Example.** 1 Jul 2026

**Rules.** Required. Date value.

## PaymentDate {#EmployeeTicket.PaymentDate}
<!-- alias: loc:PaymentDate, dom:EmployeeTicket_PaymentDate -->

**What it is.** The date the ticket was paid or used.

**Example.** 1 Jul 2026

**Why it matters.** Tickets paid within an accrual period count as used when the time engine works out the remaining entitlement.

**Rules.** Required. Date value.

## Compensate {#EmployeeTicket.Compensate}
<!-- alias: loc:Compensate, dom:EmployeeTicket_Compensate -->

**What it is.** The number of tickets.

**Example.** 1

**Why it matters.** The time engine subtracts it from the entitlement, and the average ticket calculation multiplies it by the route fare.

**Rules.** Optional. Number.

## TicketPaymentType {#EmployeeTicket.TicketPaymentType}
<!-- alias: loc:TicketPaymentType, dom:EmployeeTicket_TicketPaymentType -->

**What it is.** When the ticket is paid.

**Example.** Pay With Payroll

**Why it matters.** Recorded only; payroll and reports do not read it at present.

**Rules.** Required. One of: Pay With Payroll, Pay In Advance.

## Amount {#EmployeeTicket.Amount}
<!-- alias: loc:Amount, dom:EmployeeTicket_Amount -->

**What it is.** The ticket amount, when it overrides the route fare.

**Example.** 180.000

**Why it matters.** The time engine fills it from a benefit or dependant exception.

**Rules.** Optional. Number.

## AdvancedTicketPaid {#EmployeeTicket.AdvancedTicketPaid}
<!-- alias: loc:AdvancedTicketPaid, dom:EmployeeTicket_AdvancedTicketPaid -->

**What it is.** A marker for an advance ticket payment.

**Why it matters.** Recorded only; payroll and reports do not read it at present.

**Rules.** Optional. Whole number.

## OverrideAmount {#EmployeeTicket.OverrideAmount}
<!-- alias: loc:OverrideAmount, dom:EmployeeTicket_OverrideAmount -->

**What it is.** Whether the amount overrides the route fare.

**Why it matters.** The time engine ticks it when it takes the amount from an exception.

**Rules.** Yes or no.

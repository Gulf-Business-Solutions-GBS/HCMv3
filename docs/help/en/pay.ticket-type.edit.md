---
helpId: pay.ticket-type.edit
lang: en
title: Ticket Types — Create / Edit
entity: HCMv3.PayrollManagement.TicketType
surfaces:
  - angular/src/app/admin/payroll-management/ticket-types/create-or-edit-ticket-type-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.TicketTypes.Create]
---

## Overview

A kind of air ticket -- the employee's own, or one for a kind of dependant -- and how its entitlement builds up and lapses. Ticket type `1` is the employee's own ticket; the others are dependant tickets. How many tickets and their value are set per sub group.

## TicketBasisPeriod {#TicketType.TicketBasisPeriod}
<!-- alias: loc:TicketBasisPeriod, dom:TicketType_TicketBasisPeriod -->

**What it is.** Whether a ticket's value is based on calendar days or a fixed number of days.

**Why it matters.** Not read at present.

**Rules.** Required. One of: Calendar Days, Fixed Days.

## TicketBasisFixedDays {#TicketType.TicketBasisFixedDays}
<!-- alias: loc:TicketBasisFixedDays, dom:TicketType_TicketBasisFixedDays -->

**What it is.** The fixed number of days, for the fixed-days basis.

**Why it matters.** Not read at present.

**Rules.** Optional. Number.

## Lapse {#TicketType.Lapse}
<!-- alias: loc:Lapse, dom:TicketType_Lapse -->

**What it is.** Whether unused ticket entitlement lapses.

**Why it matters.** Ticked, the ticket entitlement runs in cycles of **Lapse After** years from the start of its accrual period, and starts again each cycle. Unticked, the first cycle never ends.

**Rules.** Yes or no.

## LapseAfter {#TicketType.LapseAfter}
<!-- alias: loc:LapseAfter, dom:TicketType_LapseAfter -->

**What it is.** How many years each ticket cycle lasts before unused entitlement lapses.

**Example.** `2`

**Rules.** Optional. Whole number.

:::admin
With **Lapse** ticked, this must be at least 1; the form refuses 0 and shows an error. A ticket type saved with 0 before this rule existed is calculated as 1 year, so check any old ticket types that have **Lapse** ticked.
:::

## StopDependantAccrualAfter {#TicketType.StopDependantAccrualAfter}
<!-- alias: loc:StopDependantAccrualAfter, dom:TicketType_StopDependantAccrualAfter -->

**What it is.** When dependant ticket accrual stops.

**Why it matters.** Not read at present.

**Rules.** Optional. Whole number.

## MinCompensation {#TicketType.MinCompensation}
<!-- alias: loc:MinCompensation, dom:TicketType_MinCompensation -->

**What it is.** The least paid when a ticket is compensated in cash.

**Why it matters.** Not read at present.

**Rules.** Optional. Number.

## DeductionPeriod {#TicketType.DeductionPeriod}
<!-- alias: loc:DeductionPeriod, dom:TicketType_DeductionPeriod -->

**What it is.** Whether a used ticket is taken from the current or the next entitlement.

**Why it matters.** Not read at present.

**Rules.** Required. One of: After Entitlement, Same As Entitlement.

## QuotaEntitlement {#TicketType.QuotaEntitlement}
<!-- alias: loc:QuotaEntitlement, dom:TicketType_QuotaEntitlement -->

**What it is.** Whether tickets build up over the period or are available straight away.

**Example.** Period

**Why it matters.** *Period* makes the time engine accrue the entitlement a little each day. *Immediate* accrues nothing day by day.

**Rules.** Required. One of: Period, Immediate.

## WageTypeId {#TicketType.WageTypeId}
<!-- alias: loc:WageTypeId, dom:TicketType_WageTypeId -->

**What it is.** The wagetype for this ticket type.

**Example.** Air Ticket

**Why it matters.** It must be filled in to save, but nothing reads it at present.

**Rules.** Optional. Chosen from Wagetype master data.

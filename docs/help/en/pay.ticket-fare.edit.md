---
helpId: pay.ticket-fare.edit
lang: en
title: Ticket Fares — Create / Edit
entity: HCMv3.PayrollManagement.TicketFare
surfaces:
  - angular/src/app/admin/payroll-management/ticket-fares/create-or-edit-ticket-fare-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.TicketFares.Create]
---

## Overview

The fare for one route, passenger type and ticket class, from a given date.

Tickets valued by *FareAmount* take the fare for the employee's route, the ticket class of their sub group and the passenger type of the traveller's age. The monthly ticket provision values accrued tickets at the return fare.

## TicketRouteId {#TicketFare.TicketRouteId}
<!-- alias: loc:TicketRouteId, dom:TicketFare_TicketRouteId -->

**What it is.** The route.

**Example.** Kuwait – Cairo

**Rules.** Required. Chosen from Ticket Route master data.

## TicketPaxTypeId {#TicketFare.TicketPaxTypeId}
<!-- alias: loc:TicketPaxTypeId, dom:TicketFare_TicketPaxTypeId -->

**What it is.** The passenger type, by age.

**Example.** Adult

**Rules.** Required. Chosen from Ticket Pax Type master data.

## TicketClassId {#TicketFare.TicketClassId}
<!-- alias: loc:TicketClassId, dom:TicketFare_TicketClassId -->

**What it is.** The ticket class.

**Example.** Economy

**Rules.** Required. Chosen from Ticket Class master data.

## OneWayTicketFare {#TicketFare.OneWayTicketFare}
<!-- alias: loc:OneWayTicketFare, dom:TicketFare_OneWayTicketFare -->

**What it is.** The one-way fare.

**Example.** `95`

**Why it matters.** Used when the sub group's ticket rule is set to one way. It cannot be negative.

**Rules.** Optional. Number.

## ReturnTicketFare {#TicketFare.ReturnTicketFare}
<!-- alias: loc:ReturnTicketFare, dom:TicketFare_ReturnTicketFare -->

**What it is.** The return fare.

**Example.** `170`

**Why it matters.** Used when the sub group's ticket rule is set to return, and for the monthly ticket provision. It cannot be negative.

**Rules.** Optional. Number.

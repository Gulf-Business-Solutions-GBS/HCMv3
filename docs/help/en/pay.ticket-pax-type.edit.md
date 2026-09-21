---
helpId: pay.ticket-pax-type.edit
lang: en
title: Ticket Passenger Types — Create / Edit
entity: HCMv3.PayrollManagement.TicketPaxType
surfaces:
  - angular/src/app/admin/payroll-management/ticket-pax-types/create-or-edit-ticket-pax-type-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.TicketPaxTypes.Create]
---

## Overview

A passenger type by age -- adult, child, infant -- used to pick the fare for a traveller. A dependant's age on the day decides which passenger type's fare is used.

## AgeFrom {#TicketPaxType.AgeFrom}
<!-- alias: loc:AgeFrom, dom:TicketPaxType_AgeFrom -->

**What it is.** The youngest age in this passenger type.

**Example.** `2`

**Rules.** Optional. Number.

## AgeTo {#TicketPaxType.AgeTo}
<!-- alias: loc:AgeTo, dom:TicketPaxType_AgeTo -->

**What it is.** The oldest age in this passenger type.

**Example.** `11`

**Why it matters.** Keep the age ranges of the passenger types from overlapping, or the first match wins.

**Rules.** Optional. Number.

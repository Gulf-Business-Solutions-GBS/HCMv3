---
helpId: pa.employee-ticket-route.edit
lang: en
title: Employee Ticket Routes — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeTicketRoute
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/ticket-routes/create-or-edit-employee-ticket-route-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeTicketRoutes.Create]
---

## Overview

The route the employee's air tickets are priced on, for a period. Routes for the same employee cannot overlap.

The ticket provision and average ticket calculations take the fare for this route.

## TicketRouteId {#EmployeeTicketRoute.TicketRouteId}
<!-- alias: loc:TicketRouteId, dom:EmployeeTicketRoute_TicketRouteId -->

**What it is.** The route.

**Example.** Kuwait – Cairo

**Why it matters.** The ticket fare is looked up for this route; without a fare row for the route, the provision line is left unchanged.

**Rules.** Required. Chosen from Ticket Route master data.

:::admin
Maintained under Payroll Management → Ticket Routes, with fares under Payroll Management → Ticket Fares.
:::

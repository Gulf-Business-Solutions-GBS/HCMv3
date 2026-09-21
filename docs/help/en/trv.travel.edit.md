---
helpId: trv.travel.edit
lang: en
title: Travels — Create / Edit
entity: HCMv3.TravelManagement.Travel
surfaces:
  - angular/src/app/admin/travel-management/travels/create-or-edit-travel-modal.component.html
audience: [admin]
permissions: [Pages.TravelManagement.Travels.Create]
---

## Overview

A business trip for an employee, with its legs and their costs. A trip entered here goes to approval exactly like one an employee requests in self-service, and is judged by the same rules: at least one leg is needed, the end date cannot be before the start date, and the dates cannot overlap another trip of the employee's that is approved or waiting for approval. The approver sees the destination, dates and total cost. Leave the code blank to have the next number given. Trips are not paid through payroll.

## Employee {#Travel.EmployeeId}
<!-- alias: loc:Employee, dom:Travel_EmployeeId -->

**What it is.** The employee travelling.

**Rules.** Required. Chosen from the employee list.

## TravelType {#Travel.TravelTypeId}
<!-- alias: loc:TravelType, dom:Travel_TravelTypeId -->

**What it is.** The kind of trip.

**Example.** Business meeting

**Rules.** Required. Chosen from Travel Type master data.

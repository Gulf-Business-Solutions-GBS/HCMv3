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

A business trip, with its legs and their costs. Leave the code blank to have the next number given. Trips are not paid through payroll.

**Employee or Applicant** at the top says who is travelling: one of your employees, or a job applicant you are flying in, for example for an interview. A trip is for exactly one of the two; switching clears the other.

An employee's trip goes to approval exactly like one the employee requests in self-service, and is judged by the same rules: at least one leg is needed, the end date cannot be before the start date, and the dates cannot overlap another trip of the employee's that is approved or waiting for approval. The approver sees the destination, dates and total cost.

An applicant's trip is saved straight away, without approval: approval is routed through the employee a request is about — their manager, their unit — and an applicant has none. Only the end-date rule applies to it. A trip that has been sent for approval cannot later be moved from the employee to an applicant.

## Employee {#Travel.EmployeeId}
<!-- alias: loc:Employee, dom:Travel_EmployeeId -->

**What it is.** The employee travelling.

**Rules.** Required. Chosen from the employee list.

## Applicant {#Travel.ApplicantId}
<!-- alias: loc:Applicant, dom:Travel_ApplicantId -->

**What it is.** The job applicant travelling. Shown instead of Employee when **Applicant** is chosen above.

**Example.** APP-0042 - Sara Ahmed

**Why it matters.** The list holds every applicant in Recruitment, by code. The trip is recorded at once, with no approval, and the travel report and the claims screen show it against the applicant.

**Rules.** Required. Chosen from Applicant master data.

## TravelType {#Travel.TravelTypeId}
<!-- alias: loc:TravelType, dom:Travel_TravelTypeId -->

**What it is.** The kind of trip.

**Example.** Business meeting

**Rules.** Required. Chosen from Travel Type master data.

## TotalTravelCost {#Travel.TotalTravelCost}
<!-- alias: loc:TotalTravelCost, dom:Travel_TotalTravelCost -->

**What it is.** The cost of the whole trip: the sum of its legs' total amounts, rounded to the decimals of the employee's payroll currency. Shown once the trip has been saved; it cannot be typed.

**Example.** 465.250

**Why it matters.** Kept up to date as legs and their costs are added, changed or deleted, including on trips still waiting for approval. Each leg's total is the sum of its cost lines, added as they are whatever currency each line is in. The travel report shows it.

**Rules.** Optional. Number.

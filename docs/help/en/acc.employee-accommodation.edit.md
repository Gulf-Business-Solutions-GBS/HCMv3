---
helpId: acc.employee-accommodation.edit
lang: en
title: Employee Accommodations — Create / Edit
entity: HCMv3.AccommodationManagement.EmployeeAccommodation
surfaces:
  - angular/src/app/admin/accommodation-management/employee-accommodations/create-or-edit-employee-accommodation-modal.component.html
audience: [admin]
permissions: [Pages.AccommodationManagement.EmployeeAccommodations.Create]
---

## Overview

Where an employee is housed and for how long. Each list below the location shows only the places inside the one chosen above it. The end date cannot be before the start date, and an employee's stays cannot overlap. Nothing stops two employees being given the same bed. Stays appear on the Employee Accommodation report.

## Employee {#EmployeeAccommodation.EmployeeId}
<!-- alias: loc:Employee, dom:EmployeeAccommodation_EmployeeId -->

**What it is.** The employee being housed.

**Rules.** Required. Chosen from the employee list.

## AccommodationType {#EmployeeAccommodation.AccommodationTypeId}
<!-- alias: loc:AccommodationType, dom:EmployeeAccommodation_AccommodationTypeId -->

**What it is.** The kind of housing.

**Example.** Shared

**Rules.** Required. Chosen from Accommodation Type master data.

## DeductionAmount {#EmployeeAccommodation.DeductionAmount}
<!-- alias: loc:DeductionAmount, dom:EmployeeAccommodation_DeductionAmount -->

**What it is.** An amount to deduct from the employee for the housing.

**Example.** 25.000

**Why it matters.** Not read at present: payroll does not deduct it. Use a deduction wagetype on the employee to recover the cost.

**Rules.** Required. Number.

## Location {#EmployeeAccommodation.LocationId}
<!-- alias: loc:Location, dom:EmployeeAccommodation_LocationId -->

**What it is.** The location.

**Rules.** Required. Chosen from Location master data.

## Building {#EmployeeAccommodation.BuildingId}
<!-- alias: loc:Building, dom:EmployeeAccommodation_BuildingId -->

**What it is.** The building.

**Rules.** Required. Chosen from Building master data.

## Floor {#EmployeeAccommodation.FloorId}
<!-- alias: loc:Floor, dom:EmployeeAccommodation_FloorId -->

**What it is.** The floor.

**Rules.** Required. Chosen from Floor master data.

## Flat {#EmployeeAccommodation.FlatId}
<!-- alias: loc:Flat, dom:EmployeeAccommodation_FlatId -->

**What it is.** The flat.

**Rules.** Required. Chosen from Flat master data.

## Room {#EmployeeAccommodation.RoomId}
<!-- alias: loc:Room, dom:EmployeeAccommodation_RoomId -->

**What it is.** The room.

**Rules.** Required. Chosen from Room master data.

## Bed {#EmployeeAccommodation.BedId}
<!-- alias: loc:Bed, dom:EmployeeAccommodation_BedId -->

**What it is.** The bed.

**Rules.** Required. Chosen from Bed master data.

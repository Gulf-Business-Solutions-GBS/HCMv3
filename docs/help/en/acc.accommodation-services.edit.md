---
helpId: acc.accommodation-services.edit
lang: en
title: Accommodation Services — Create / Edit
entity: HCMv3.AccommodationManagement.AccommodationServices
surfaces:
  - angular/src/app/admin/accommodation-management/accommodation-services/create-or-edit-accommodation-service-modal.component.html
audience: [admin]
permissions: [Pages.AccommodationManagement.AccommodationServices.Create]
---

## Overview

A service given to an employee during a stay, for a date range. The end date cannot be before the start date. Services appear on the Employee Accommodation, Employee Post Wise and Salary Benefits reports.

## EmployeeAccommodation {#AccommodationServices.EmployeeAccommodationId}
<!-- alias: loc:EmployeeAccommodation, dom:AccommodationService_EmployeeAccommodationId -->

**What it is.** The stay the service belongs to.

**Rules.** Required. Chosen from a list.

## AccommodationServiceType {#AccommodationServices.AccommodationServiceTypeId}
<!-- alias: loc:AccommodationServiceType, dom:AccommodationService_AccommodationServiceTypeId -->

**What it is.** The service.

**Example.** Laundry

**Rules.** Required. Chosen from Accommodation Service Type master data.

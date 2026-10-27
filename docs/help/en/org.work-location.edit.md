---
helpId: org.work-location.edit
lang: en
title: Work Locations — Create / Edit
entity: HCMv3.OrganizationManagement.WorkLocation
surfaces:
  - angular/src/app/admin/organization-management/work-locations/create-or-edit-work-location-modal.component.html
audience: [admin]
permissions: [Pages.OrganizationManagement.WorkLocations.Create]
---

## Overview

A place employees work, such as an office or site. Employees are placed in one on their organisation assignment. With a position and range set, it is where the self-service geo punch checks the employee is standing.

## Latitude {#WorkLocation.Latitude}
<!-- alias: loc:Latitude, dom:WorkLocation_Latitude -->

**What it is.** The location's latitude.

**Example.** 29.3759

**Rules.** Optional. Number.

## Longitude {#WorkLocation.Longitude}
<!-- alias: loc:Longitude, dom:WorkLocation_Longitude -->

**What it is.** The location's longitude.

**Example.** 47.9774

**Rules.** Optional. Number.

## GeoRange {#WorkLocation.GeoRange}
<!-- alias: loc:GeoRange, dom:WorkLocation_GeoRange -->

**What it is.** How far from the point, in metres, a geo punch still counts as on site.

**Example.** 200

**Why it matters.** The server measures the distance from the employee's phone to the point. Within the range the punch is accepted; beyond it the punch goes through only when the self-service setting AllowPunchOutsideRange is on.

**Rules.** Optional. Number.

## Address {#WorkLocation.Address}
<!-- alias: loc:Address, dom:WorkLocation_Address -->

**What it is.** The street address.

**Rules.** Optional. Maximum 1000 characters.

## GeoFenceCoordinates {#WorkLocation.GeoFenceCoordinates}
<!-- alias: loc:GeoFenceCoordinates, dom:WorkLocation_GeoFenceCoordinates -->

**What it is.** A boundary drawn around the location.

**Why it matters.** Not read at present.

**Rules.** Optional. Maximum 2000 characters.

## Sponsor {#WorkLocation.SponsorId}
<!-- alias: loc:Sponsor, dom:WorkLocation_SponsorId -->

**What it is.** The sponsor -- the licence holder -- this location belongs to.

**Example.** Al-Mutawa Trading Co.

**Why it matters.** Recorded for reference; nothing else uses it.

**Rules.** Optional. Chosen from Sponsor master data.

## IsActive {#WorkLocation.IsActive}
<!-- alias: loc:IsActive, dom:WorkLocation_IsActive -->

**What it is.** Whether the location is in use.

**Why it matters.** Not read at present: inactive locations can still be chosen and punched at.

**Rules.** Yes or no.

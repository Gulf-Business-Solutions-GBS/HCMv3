---
helpId: tm.time-event.edit
lang: en
title: Time Events — Create / Edit
entity: HCMv3.TimeManagement.TimeEvent
surfaces:
  - angular/src/app/admin/time-management/time-events/create-or-edit-time-event-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.TimeEvents.Create]
---

## Overview

_TODO_

## Employee {#TimeEvent.EmployeeId}
<!-- alias: loc:Employee, dom:TimeEvent_EmployeeId -->

**What it is.** _TODO_

**Example.** _TODO_

**Rules.** Required. Chosen from the employee list.

## Pin {#TimeEvent.PIN}
<!-- alias: loc:Pin, dom:TimeEvent_Pin -->

**What it is.** _TODO_

**Example.** _TODO_

**Rules.** Optional. Maximum 50 characters.

## TransType {#TimeEvent.TransTypeId}
<!-- alias: loc:TransType, dom:TimeEvent_TransTypeId -->

**What it is.** _TODO_

**Example.** _TODO_

**Rules.** Optional. Chosen from Trans Type master data.

## ActualTransType {#TimeEvent.ActualTransTypeId}
<!-- alias: loc:ActualTransType, dom:TimeEvent_ActualTransTypeId -->

**What it is.** _TODO_

**Example.** _TODO_

**Rules.** Optional. Chosen from Trans Type master data.

## TransDate {#TimeEvent.TransDate}
<!-- alias: loc:TransDate, dom:TimeEvent_TransDate -->

**What it is.** _TODO_

**Example.** _TODO_

**Rules.** Required. Date value.

## TransTime {#TimeEvent.TransTime}
<!-- alias: loc:TransTime, dom:TimeEvent_TransTime -->

**What it is.** _TODO_

**Example.** _TODO_

**Rules.** Required. Date value.

## TransSource {#TimeEvent.TransSource}
<!-- alias: loc:TransSource, dom:TimeEvent_TransSource -->

**What it is.** _TODO_

**Example.** _TODO_

**Rules.** Required. One of: Machine, Manual, System, ESS, MobileAPI, ExternalAPI.

## AccessUnit {#TimeEvent.AccessUnitId}
<!-- alias: loc:AccessUnit, dom:TimeEvent_AccessUnitId -->

**What it is.** _TODO_

**Example.** _TODO_

**Rules.** Optional. Chosen from Access Unit master data.

## WorkLocation {#TimeEvent.WorkLocationId}
<!-- alias: loc:WorkLocation, dom:TimeEvent_WorkLocationId -->

**What it is.** _TODO_

**Example.** _TODO_

**Rules.** Optional. Chosen from Work Location master data.

## IpAddress {#TimeEvent.IPAddress}
<!-- alias: loc:IpAddress, dom:TimeEvent_IpAddress -->

**What it is.** _TODO_

**Example.** _TODO_

**Rules.** Optional. Maximum 50 characters.

## GeoSelfiePath {#TimeEvent.GeoSelfiePath}
<!-- alias: loc:GeoSelfiePath, dom:TimeEvent_GeoSelfiePath -->

**What it is.** _TODO_

**Example.** _TODO_

**Rules.** Optional. Maximum 1000 characters.

## Latitude {#TimeEvent.Latitude}
<!-- alias: loc:Latitude, dom:TimeEvent_Latitude -->

**What it is.** _TODO_

**Example.** _TODO_

**Rules.** Optional. Number.

## Longitude {#TimeEvent.Longitude}
<!-- alias: loc:Longitude, dom:TimeEvent_Longitude -->

**What it is.** _TODO_

**Example.** _TODO_

**Rules.** Optional. Number.

## RangeDifference {#TimeEvent.RangeDifference}
<!-- alias: loc:RangeDifference, dom:TimeEvent_RangeDifference -->

**What it is.** _TODO_

**Example.** _TODO_

**Rules.** Optional. Number.

## Temperature {#TimeEvent.Temperature}
<!-- alias: loc:Temperature, dom:TimeEvent_Temperature -->

**What it is.** _TODO_

**Example.** _TODO_

**Rules.** Optional. Number.

## PunchInRange {#TimeEvent.PunchInRange}
<!-- alias: loc:PunchInRange, dom:TimeEvent_PunchInRange -->

**What it is.** _TODO_

**Example.** _TODO_

**Rules.** Yes or no.

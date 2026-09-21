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

One punch: an employee clocking in or out, or out and back for a break, at a date and time. Punches arrive from attendance devices, the attendance API, self-service and mobile, or are entered by hand here; the time engine reads them to work out each day's attendance.

## Employee {#TimeEvent.EmployeeId}
<!-- alias: loc:Employee, dom:TimeEvent_EmployeeId -->

**What it is.** The employee who punched.

**Rules.** Required. Chosen from the employee list.

## Pin {#TimeEvent.PIN}
<!-- alias: loc:Pin, dom:TimeEvent_Pin -->

**What it is.** The employee's number on the attendance device.

**Example.** `10457`

**Why it matters.** Device imports use it, with the date and time, to skip punches already imported.

**Rules.** Optional. Maximum 50 characters.

## TransType {#TimeEvent.TransTypeId}
<!-- alias: loc:TransType, dom:TimeEvent_TransTypeId -->

**What it is.** What the punch is: clock in, clock out, break out or break in.

**Example.** Clock In

**Why it matters.** The time engine looks for each kind of punch separately, so a punch of the wrong type is never used for the day.

**Rules.** Optional. Chosen from Trans Type master data.

## ActualTransType {#TimeEvent.ActualTransTypeId}
<!-- alias: loc:ActualTransType, dom:TimeEvent_ActualTransTypeId -->

**What it is.** The punch type the device originally reported, if it was changed.

**Why it matters.** Not read at present.

**Rules.** Optional. Chosen from Trans Type master data.

## TransDate {#TimeEvent.TransDate}
<!-- alias: loc:TransDate, dom:TimeEvent_TransDate -->

**What it is.** The date of the punch.

**Example.** `2026-04-14`

**Rules.** Required. Date value.

## TransTime {#TimeEvent.TransTime}
<!-- alias: loc:TransTime, dom:TimeEvent_TransTime -->

**What it is.** The time of the punch.

**Example.** `07:42`

**Rules.** Required. Date value.

## TransSource {#TimeEvent.TransSource}
<!-- alias: loc:TransSource, dom:TimeEvent_TransSource -->

**What it is.** Where the punch came from: a device, manual entry, the system, self-service, mobile or an external API.

**Example.** Machine

**Rules.** Required. One of: Machine, Manual, System, ESS, MobileAPI, ExternalAPI.

## AccessUnit {#TimeEvent.AccessUnitId}
<!-- alias: loc:AccessUnit, dom:TimeEvent_AccessUnitId -->

**What it is.** The device the punch was made on.

**Why it matters.** Not read at present.

**Rules.** Optional. Chosen from Access Unit master data.

## WorkLocation {#TimeEvent.WorkLocationId}
<!-- alias: loc:WorkLocation, dom:TimeEvent_WorkLocationId -->

**What it is.** The work location of the punch.

**Example.** Head Office

**Why it matters.** Used by the daily man-hours summary report.

**Rules.** Optional. Chosen from Work Location master data.

## IpAddress {#TimeEvent.IPAddress}
<!-- alias: loc:IpAddress, dom:TimeEvent_IpAddress -->

**What it is.** The address of the device that sent the punch.

**Example.** `10.0.4.21`

**Why it matters.** Device imports use it to find or create the access unit.

**Rules.** Optional. Maximum 50 characters.

## GeoSelfiePath {#TimeEvent.GeoSelfiePath}
<!-- alias: loc:GeoSelfiePath, dom:TimeEvent_GeoSelfiePath -->

**What it is.** Where the selfie taken with a mobile punch is stored.

**Why it matters.** Shown on the late/early attendance report with photos.

**Rules.** Optional. Maximum 1000 characters.

## Latitude {#TimeEvent.Latitude}
<!-- alias: loc:Latitude, dom:TimeEvent_Latitude -->

**What it is.** The latitude where a mobile punch was made.

**Why it matters.** Not read at present.

**Rules.** Optional. Number.

## Longitude {#TimeEvent.Longitude}
<!-- alias: loc:Longitude, dom:TimeEvent_Longitude -->

**What it is.** The longitude where a mobile punch was made.

**Why it matters.** Not read at present.

**Rules.** Optional. Number.

## RangeDifference {#TimeEvent.RangeDifference}
<!-- alias: loc:RangeDifference, dom:TimeEvent_RangeDifference -->

**What it is.** How far a mobile punch was from the work location.

**Why it matters.** Not read at present.

**Rules.** Optional. Number.

## Temperature {#TimeEvent.Temperature}
<!-- alias: loc:Temperature, dom:TimeEvent_Temperature -->

**What it is.** The body temperature recorded at the punch.

**Why it matters.** Not read at present.

**Rules.** Optional. Number.

## PunchInRange {#TimeEvent.PunchInRange}
<!-- alias: loc:PunchInRange, dom:TimeEvent_PunchInRange -->

**What it is.** Whether a mobile punch was within range of the work location.

**Why it matters.** Not read at present.

**Rules.** Yes or no.

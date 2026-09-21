---
helpId: trv.travel-detail.edit
lang: en
title: Travel Details — Create / Edit
entity: HCMv3.TravelManagement.TravelDetail
surfaces:
  - angular/src/app/admin/travel-management/travel-details/create-or-edit-travel-detail-modal.component.html
audience: [admin]
permissions: [Pages.TravelManagement.TravelDetails.Create]
---

## Overview

One leg of a trip: from where to where, when, and where the employee stays. A leg cannot be deleted while it still has costs. The form can work out the expected per diem for the leg from the per-diem rates: choose the employee group and sub group, and it multiplies the nights by the rate for this route.

## Travel {#TravelDetail.TravelId}
<!-- alias: loc:Travel, dom:TravelDetail_TravelId -->

**What it is.** The trip.

**Rules.** Required. Chosen from Travel master data.

## Departure {#TravelDetail.DepartureId}
<!-- alias: loc:Departure, dom:TravelDetail_DepartureId -->

**What it is.** Where the leg starts.

**Example.** Kuwait

**Rules.** Required. Chosen from Travel Destination master data.

## Arrival {#TravelDetail.ArrivalId}
<!-- alias: loc:Arrival, dom:TravelDetail_ArrivalId -->

**What it is.** Where the leg ends.

**Example.** Dubai

**Rules.** Required. Chosen from Travel Destination master data.

## DepartureDate {#TravelDetail.DepartureDate}
<!-- alias: loc:DepartureDate, dom:TravelDetail_DepartureDate -->

**What it is.** The departure date.

**Rules.** Required. Date value.

## DepartureTime {#TravelDetail.DepartureTime}
<!-- alias: loc:DepartureTime, dom:TravelDetail_DepartureTime -->

**What it is.** The departure time.

**Example.** 09:45

**Rules.** Required. Date value.

## ArrivalDate {#TravelDetail.ArrivalDate}
<!-- alias: loc:ArrivalDate, dom:TravelDetail_ArrivalDate -->

**What it is.** The arrival date.

**Rules.** Required. Date value.

## ArrivalTime {#TravelDetail.ArrivalTime}
<!-- alias: loc:ArrivalTime, dom:TravelDetail_ArrivalTime -->

**What it is.** The arrival time.

**Example.** 09:45

**Rules.** Required. Date value.

## Hotel {#TravelDetail.HotelId}
<!-- alias: loc:Hotel, dom:TravelDetail_HotelId -->

**What it is.** The hotel, if any.

**Rules.** Optional. Chosen from Hotel master data.

## TravelAirlines {#TravelDetail.TravelAirlineId}
<!-- alias: loc:TravelAirlines, dom:TravelDetail_TravelAirlineId -->

**What it is.** The airline, if any.

**Rules.** Optional. Chosen from Travel Airline master data.

## TotalNights {#TravelDetail.TotalNights}
<!-- alias: loc:TotalNights, dom:TravelDetail_TotalNights -->

**What it is.** How many nights are spent at the destination. Used for the expected per diem.

**Example.** 3

**Rules.** Optional. Whole number.

## NightStay {#TravelDetail.NightStay}
<!-- alias: loc:NightStay, dom:TravelDetail_NightStay -->

**What it is.** Whether the leg includes an overnight stay.

**Rules.** Yes or no.

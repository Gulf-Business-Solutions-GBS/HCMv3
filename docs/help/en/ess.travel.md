---
helpId: ess.travel
lang: en
title: My Travel
surfaces:
  - angular/src/app/main/ess/my-travel/my-travel.component.html
audience: [end-user]
permissions: [Ess.MyTravel]
---

## Overview

Request a business trip: where you are going, when, how you are getting there and what it will cost.

A trip is built from **legs** — one for each flight or journey — and each leg carries its own dates, times, airline, hotel and costs. A one-city trip is one leg out and one back; a tour of three cities is more. What you put here is what gets approved, and it is what your travel claims are later checked against, so an honest estimate is worth more than a tidy one.

## Travel type {#ess.travel.travel-type}
<!-- alias: loc:TravelType -->

**What it is.** What kind of trip this is — a business trip, a training course, a conference, a transfer — from the types your company has set up.

**Example.** Business Trip.

**Why it matters.** The type decides the allowance you are paid per day and often who has to approve the trip. A training course and a client visit are not funded the same way.

## Start date {#ess.travel.start-date}
<!-- alias: loc:StartDate -->

**What it is.** The first day of the trip.

**Example.** 12 May 2026.

**Why it matters.** With the end date it sets how long you are away, which is what the daily allowance is worked out from and what tells your manager how long your work needs covering.

## End date {#ess.travel.end-date}
<!-- alias: loc:EndDate -->

**What it is.** The day you are back.

**Example.** 16 May 2026.

**Why it matters.** It must be on or after the start date, and it should be the day you are actually back — not the day you land at midnight, if the next morning is not a working one.

## Purpose {#ess.travel.travel-purpose}
<!-- alias: loc:TravelPurpose -->

**What it is.** Why you are going, in your own words.

**Example.** Implementation kick-off with the client in Dubai; three days on site.

**Why it matters.** It is the case for the trip, and it is what the person paying for it reads first. It also ends up on the record if anybody asks later why the company sent you.

## Travel legs {#ess.travel.departure}
<!-- alias: loc:Departure -->

**What it is.** Where each part of the journey starts. A leg is one flight or journey; add one for each.

**Example.** Kuwait.

**Why it matters.** The legs are the itinerary. Getting them right is what makes the booking possible and the cost believable — a Kuwait–Dubai–Doha–Kuwait trip is three legs, and leaving one out understates both the time and the fare.

## Departure date {#ess.travel.departure-date}
<!-- alias: loc:DepartureDate -->

**What it is.** The day that leg departs.

**Example.** 12 May 2026.

**Why it matters.** It must sit inside the trip's own start and end dates. It is also what orders the legs, so a wrong date can put your return before your outbound.

## Departure time {#ess.travel.departure-time}
<!-- alias: loc:DepartureTime -->

**What it is.** The time that leg departs.

**Example.** 08:45

**Why it matters.** It is what tells whoever books the flight which one you mean, and an early departure is often what justifies a hotel the night before.

## Arrival {#ess.travel.arrival}
<!-- alias: loc:Arrival -->

**What it is.** Where that leg lands.

**Example.** Dubai.

**Why it matters.** The arrival of one leg should be the departure of the next; where it is not, a leg is missing.

## Arrival date {#ess.travel.arrival-date}
<!-- alias: loc:ArrivalDate -->

**What it is.** The day that leg lands. It can be the day after departure on a long or overnight flight.

**Example.** 12 May 2026.

**Why it matters.** It is what a red-eye is recorded as, and it decides which day the nights are counted from.

## Arrival time {#ess.travel.arrival-time}
<!-- alias: loc:ArrivalTime -->

**What it is.** The time that leg lands.

**Example.** 11:20

**Why it matters.** A late arrival usually means a hotel night on arrival, and an early one means the day still counts as a working day.

## Airline {#ess.travel.travel-airlines}
<!-- alias: loc:TravelAirlines -->

**What it is.** Who you are flying with on that leg, where you know or have a preference.

**Example.** Kuwait Airways.

**Why it matters.** Optional. Companies with a negotiated fare or a preferred carrier expect it to be used, and a leg left blank is booked at whoever's discretion does the booking.

## Hotel {#ess.travel.hotels}
<!-- alias: loc:Hotels -->

**What it is.** Where you will stay for that leg.

**Example.** A hotel near the client's office in Deira.

**Why it matters.** It is what the accommodation cost is based on, and the detail your company checks against its own rate for that city.

## Nights {#ess.travel.total-nights}
<!-- alias: loc:TotalNights -->

**What it is.** How many nights you stay at that destination.

**Example.** 3

**Why it matters.** Nights, not days, are what a hotel charges and what a night allowance is paid on, and they are one fewer than the days for a normal trip. This is where a trip's cost most often comes out wrong.

## Night stay {#ess.travel.night-stay}
<!-- alias: loc:NightStay -->

**What it is.** A tick saying you stay overnight at this destination rather than travelling on.

**Example.** Off for a connection through Doha; on for the three nights in Dubai.

**Why it matters.** It is what tells a stop apart from a stopover. Untick it for a leg you only pass through, or the trip claims nights you never spent.

## Visa required {#ess.travel.visa-required}
<!-- alias: loc:VisaRequired -->

**What it is.** A tick saying you need a visa for this destination, with a box for the details.

**Example.** On, with "Apply through the embassy — allow ten working days."

**Why it matters.** It is the part of a trip with the longest lead time and the one most likely to stop it happening. Ticking it early is what gets somebody started on it; realising it a week before is how trips get cancelled.

## Hotel required {#ess.travel.hotel-required}
<!-- alias: loc:HotelRequired -->

**What it is.** A tick saying the company should book and pay for the hotel, with a box for the details.

**Example.** Off when you are staying with family; on with "Near the client's office, breakfast included."

**Why it matters.** It is what puts the booking on somebody's list. Leaving it off does not mean you will not stay anywhere — it means nobody is arranging it.

## Any other expense {#ess.travel.any-other-expense}
<!-- alias: loc:AnyOtherExpense -->

**What it is.** A tick for anything else this leg will cost that the fields above do not cover, with a box for what it is.

**Example.** Airport transfers, a conference fee, excess baggage for equipment.

**Why it matters.** An expense approved now is an expense you can claim later without an argument. This is the place to flag an unusual cost, before the trip rather than after it.

---
helpId: tm.roster-duty-period-detail.edit
lang: en
title: Roster Duty Period Details — Create / Edit
entity: HCMv3.TimeManagement.RosterDutyPeriodDetail
surfaces:
  - angular/src/app/admin/time-management/roster-duty-period-details/create-or-edit-roster-duty-period-detail-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.RosterDutyPeriodDetails.Create]
---

## Overview

One week of a roster duty period.

Not read at present: the time engine and payroll take employees' duties from duty periods and roster groups, not from this list. The roster generator that would have used it was never live in the legacy system and was not ported.

## RosterDutyPeriod {#RosterDutyPeriodDetail.RosterDutyPeriodId}
<!-- alias: loc:RosterDutyPeriod, dom:RosterDutyPeriodDetail_RosterDutyPeriodId -->

**What it is.** The roster duty period this week belongs to.

**Rules.** Required. Chosen from Roster Duty Period master data.

## Week {#RosterDutyPeriodDetail.Week}
<!-- alias: loc:Week, dom:RosterDutyPeriodDetail_Week -->

**What it is.** The week's number.

**Example.** `1`

**Why it matters.** Must be greater than 0.

**Rules.** Required. Whole number.

## Day1 {#RosterDutyPeriodDetail.Day1DutyId}
<!-- alias: loc:Day1, dom:RosterDutyPeriodDetail_Day1DutyId -->

**What it is.** The daily duty on day 1 of this week.

**Rules.** Optional. Chosen from Daily Duty master data.

## Day2 {#RosterDutyPeriodDetail.Day2DutyId}
<!-- alias: loc:Day2, dom:RosterDutyPeriodDetail_Day2DutyId -->

**What it is.** The daily duty on day 2 of this week.

**Rules.** Optional. Chosen from Daily Duty master data.

## Day3 {#RosterDutyPeriodDetail.Day3DutyId}
<!-- alias: loc:Day3, dom:RosterDutyPeriodDetail_Day3DutyId -->

**What it is.** The daily duty on day 3 of this week.

**Rules.** Optional. Chosen from Daily Duty master data.

## Day4 {#RosterDutyPeriodDetail.Day4DutyId}
<!-- alias: loc:Day4, dom:RosterDutyPeriodDetail_Day4DutyId -->

**What it is.** The daily duty on day 4 of this week.

**Rules.** Optional. Chosen from Daily Duty master data.

## Day5 {#RosterDutyPeriodDetail.Day5DutyId}
<!-- alias: loc:Day5, dom:RosterDutyPeriodDetail_Day5DutyId -->

**What it is.** The daily duty on day 5 of this week.

**Rules.** Optional. Chosen from Daily Duty master data.

## Day6 {#RosterDutyPeriodDetail.Day6DutyId}
<!-- alias: loc:Day6, dom:RosterDutyPeriodDetail_Day6DutyId -->

**What it is.** The daily duty on day 6 of this week.

**Rules.** Optional. Chosen from Daily Duty master data.

## Day7 {#RosterDutyPeriodDetail.Day7DutyId}
<!-- alias: loc:Day7, dom:RosterDutyPeriodDetail_Day7DutyId -->

**What it is.** The daily duty on day 7 of this week.

**Rules.** Optional. Chosen from Daily Duty master data.

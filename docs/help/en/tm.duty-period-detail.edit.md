---
helpId: tm.duty-period-detail.edit
lang: en
title: Duty Period Details — Create / Edit
entity: HCMv3.TimeManagement.DutyPeriodDetail
surfaces:
  - angular/src/app/admin/time-management/duty-period-details/create-or-edit-duty-period-detail-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.DutyPeriodDetails.Create]
---

## Overview

One week of a duty period's rotation. A rotation of N weeks needs weeks numbered 1 to N; each day of the cycle takes the duty for that week and day.

The employee's **start point** says which day of the cycle their assignment starts on, and each later day moves one step through the cycle, wrapping round at the end. A missing week number stops the time engine with "wrong week number assigned".

## DutyPeriod {#DutyPeriodDetail.DutyPeriodId}
<!-- alias: loc:DutyPeriod, dom:DutyPeriodDetail_DutyPeriodId -->

**What it is.** The duty period this week belongs to.

**Example.** 4-on 4-off Rotation

**Rules.** Required. Chosen from Duty Period master data.

## Week {#DutyPeriodDetail.Week}
<!-- alias: loc:Week, dom:DutyPeriodDetail_Week -->

**What it is.** The week's number in the rotation.

**Example.** `2`

**Why it matters.** Must be greater than 0, and each number can be used once per duty period.

**Rules.** Required. Whole number.

## Day1 {#DutyPeriodDetail.Day1DutyId}
<!-- alias: loc:Day1, dom:DutyPeriodDetail_Day1DutyId -->

**What it is.** The duty on day 1 of this week of the rotation.

**Example.** Night Shift

**Rules.** Optional. Chosen from Duty master data.

## Day2 {#DutyPeriodDetail.Day2DutyId}
<!-- alias: loc:Day2, dom:DutyPeriodDetail_Day2DutyId -->

**What it is.** The duty on day 2 of this week of the rotation.

**Example.** Night Shift

**Rules.** Optional. Chosen from Duty master data.

## Day3 {#DutyPeriodDetail.Day3DutyId}
<!-- alias: loc:Day3, dom:DutyPeriodDetail_Day3DutyId -->

**What it is.** The duty on day 3 of this week of the rotation.

**Example.** Night Shift

**Rules.** Optional. Chosen from Duty master data.

## Day4 {#DutyPeriodDetail.Day4DutyId}
<!-- alias: loc:Day4, dom:DutyPeriodDetail_Day4DutyId -->

**What it is.** The duty on day 4 of this week of the rotation.

**Example.** Night Shift

**Rules.** Optional. Chosen from Duty master data.

## Day5 {#DutyPeriodDetail.Day5DutyId}
<!-- alias: loc:Day5, dom:DutyPeriodDetail_Day5DutyId -->

**What it is.** The duty on day 5 of this week of the rotation.

**Example.** Night Shift

**Rules.** Optional. Chosen from Duty master data.

## Day6 {#DutyPeriodDetail.Day6DutyId}
<!-- alias: loc:Day6, dom:DutyPeriodDetail_Day6DutyId -->

**What it is.** The duty on day 6 of this week of the rotation.

**Example.** Weekly Off

**Rules.** Optional. Chosen from Duty master data.

## Day7 {#DutyPeriodDetail.Day7DutyId}
<!-- alias: loc:Day7, dom:DutyPeriodDetail_Day7DutyId -->

**What it is.** The duty on day 7 of this week of the rotation.

**Example.** Weekly Off

**Rules.** Optional. Chosen from Duty master data.

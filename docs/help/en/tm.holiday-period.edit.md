---
helpId: tm.holiday-period.edit
lang: en
title: Holiday Periods — Create / Edit
entity: HCMv3.TimeManagement.HolidayPeriod
surfaces:
  - angular/src/app/admin/time-management/holiday-periods/create-or-edit-holiday-period-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.HolidayPeriods.Create]
---

## Overview

One date of a holiday. A holiday lasting three days has three holiday periods. The time engine treats the date as a public holiday for employees whose duty period uses the holiday's calendar.

## Holiday {#HolidayPeriod.HolidayId}
<!-- alias: loc:Holiday, dom:HolidayPeriod_HolidayId -->

**What it is.** The holiday.

**Example.** Eid al-Fitr

**Rules.** Required. Chosen from Holiday master data.

## HolidayDate {#HolidayPeriod.HolidayDate}
<!-- alias: loc:HolidayDate, dom:HolidayPeriod_HolidayDate -->

**What it is.** The date of the holiday.

**Example.** `2026-03-20`

**Why it matters.** A holiday can have each date once.

**Rules.** Required. Date value.

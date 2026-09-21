---
helpId: tm.duty-period.edit
lang: en
title: Duty Periods — Create / Edit
entity: HCMv3.TimeManagement.DutyPeriod
surfaces:
  - angular/src/app/admin/time-management/duty-periods/create-or-edit-duty-period-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.DutyPeriods.Create]
---

## Overview

A working pattern made of duties, and the holiday calendar that goes with it. Employees get a duty period directly (employee duty periods) or through their roster group.

It works one of two ways. Without a start point on the employee's assignment, each weekday takes the duty set for it here -- Sunday's duty every Sunday. With a start point, the pattern is the rotation of weeks set in the duty period's **details**, and the weekday duties here are not used.

## HolidayCalendar {#DutyPeriod.HolidayCalendarId}
<!-- alias: loc:HolidayCalendar, dom:DutyPeriod_HolidayCalendarId -->

**What it is.** The public holidays that apply to employees on this duty period.

**Example.** Kuwait Public Holidays

**Why it matters.** A day is a public holiday for the employee only if it is in this calendar.

**Rules.** Optional. Chosen from Holiday Calendar master data.

## Sunday {#DutyPeriod.SundayDutyId}
<!-- alias: loc:Sunday, dom:DutyPeriod_SundayDutyId -->

**What it is.** The duty on Sundays.

**Example.** Morning Shift

**Why it matters.** Left blank, the employee has no duty that day.

**Rules.** Optional. Chosen from Duty master data.

## Monday {#DutyPeriod.MondayDutyId}
<!-- alias: loc:Monday, dom:DutyPeriod_MondayDutyId -->

**What it is.** The duty on Mondays.

**Example.** Morning Shift

**Why it matters.** Left blank, the employee has no duty that day.

**Rules.** Optional. Chosen from Duty master data.

## Tuesday {#DutyPeriod.TuesdayDutyId}
<!-- alias: loc:Tuesday, dom:DutyPeriod_TuesdayDutyId -->

**What it is.** The duty on Tuesdays.

**Example.** Morning Shift

**Why it matters.** Left blank, the employee has no duty that day.

**Rules.** Optional. Chosen from Duty master data.

## Wednesday {#DutyPeriod.WednesdayDutyId}
<!-- alias: loc:Wednesday, dom:DutyPeriod_WednesdayDutyId -->

**What it is.** The duty on Wednesdays.

**Example.** Morning Shift

**Why it matters.** Left blank, the employee has no duty that day.

**Rules.** Optional. Chosen from Duty master data.

## Thursday {#DutyPeriod.ThursdayDutyId}
<!-- alias: loc:Thursday, dom:DutyPeriod_ThursdayDutyId -->

**What it is.** The duty on Thursdays.

**Example.** Morning Shift

**Why it matters.** Left blank, the employee has no duty that day.

**Rules.** Optional. Chosen from Duty master data.

## Friday {#DutyPeriod.FridayDutyId}
<!-- alias: loc:Friday, dom:DutyPeriod_FridayDutyId -->

**What it is.** The duty on Fridays.

**Example.** Morning Shift

**Why it matters.** Left blank, the employee has no duty that day.

**Rules.** Optional. Chosen from Duty master data.

## Saturday {#DutyPeriod.SaturdayDutyId}
<!-- alias: loc:Saturday, dom:DutyPeriod_SaturdayDutyId -->

**What it is.** The duty on Saturdays.

**Example.** Morning Shift

**Why it matters.** Left blank, the employee has no duty that day.

**Rules.** Optional. Chosen from Duty master data.

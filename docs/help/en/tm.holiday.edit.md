---
helpId: tm.holiday.edit
lang: en
title: Holidays — Create / Edit
entity: HCMv3.TimeManagement.Holiday
surfaces:
  - angular/src/app/admin/time-management/holidays/create-or-edit-holiday-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.Holidays.Create]
---

## Overview

A named public holiday in a holiday calendar -- Eid al-Fitr, National Day. Its actual dates each year are entered as holiday periods.

## HolidayCalendar {#Holiday.HolidayCalendarId}
<!-- alias: loc:HolidayCalendar, dom:Holiday_HolidayCalendarId -->

**What it is.** The calendar the holiday belongs to.

**Example.** Kuwait Public Holidays

**Rules.** Required. Chosen from Holiday Calendar master data.

## OvertimeType {#Holiday.OvertimeTypeId}
<!-- alias: loc:OvertimeType, dom:Holiday_OvertimeTypeId -->

**What it is.** The overtime type for work done on this holiday.

**Example.** Holiday Overtime

**Why it matters.** On the holiday, overtime is recorded under this type instead of the duty's.

**Rules.** Optional. Chosen from Overtime Type master data.

:::admin
The holiday is found by date only, not through the employee's own holiday calendar. If holidays in two calendars share a date, one of their overtime types is used for everyone -- give them the same overtime type. Legacy behaviour, kept on purpose.
:::

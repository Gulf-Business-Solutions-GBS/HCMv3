---
helpId: tm.holiday-period.edit
lang: ar
title: فترات العطلات — إنشاء / تحرير
entity: HCMv3.TimeManagement.HolidayPeriod
surfaces:
  - angular/src/app/admin/time-management/holiday-periods/create-or-edit-holiday-period-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.HolidayPeriods.Create]
---

## Overview

تاريخ واحد لعطلة. فالعطلة التي تستمر ثلاثة أيام لها ثلاث فترات عطلة. ويعامل محرك الوقت التاريخ عطلةً رسمية للموظفين الذين تستخدم فترة دوامهم تقويم العطلة.

## Holiday {#HolidayPeriod.HolidayId}
<!-- alias: loc:Holiday, dom:HolidayPeriod_HolidayId -->

**ما هو.** العطلة.

**مثال.** عيد الفطر

**القواعد.** إلزامي. يُختار من قائمة العطلات.

## HolidayDate {#HolidayPeriod.HolidayDate}
<!-- alias: loc:HolidayDate, dom:HolidayPeriod_HolidayDate -->

**ما هو.** تاريخ العطلة.

**مثال.** `2026-03-20`

**لماذا يهم.** لا يكون للعطلة التاريخ نفسه إلا مرة واحدة.

**القواعد.** إلزامي. تاريخ.

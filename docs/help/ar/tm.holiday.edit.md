---
helpId: tm.holiday.edit
lang: ar
title: العطلات — إنشاء / تحرير
entity: HCMv3.TimeManagement.Holiday
surfaces:
  - angular/src/app/admin/time-management/holidays/create-or-edit-holiday-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.Holidays.Create]
---

## Overview

عطلة رسمية مسمّاة في تقويم العطلات -- عيد الفطر أو العيد الوطني. وتُدخل تواريخها الفعلية كل سنة على شكل فترات عطلة.

## HolidayCalendar {#Holiday.HolidayCalendarId}
<!-- alias: loc:HolidayCalendar, dom:Holiday_HolidayCalendarId -->

**ما هو.** التقويم الذي تتبعه العطلة.

**مثال.** العطلات الرسمية في الكويت

**القواعد.** إلزامي. يُختار من قائمة تقاويم العطلات.

## OvertimeType {#Holiday.OvertimeTypeId}
<!-- alias: loc:OvertimeType, dom:Holiday_OvertimeTypeId -->

**ما هو.** نوع العمل الإضافي للعمل في هذه العطلة.

**مثال.** عمل إضافي في العطلات

**لماذا يهم.** في يوم العطلة يُسجَّل العمل الإضافي تحت هذا النوع بدلاً من نوع الدوام.

**القواعد.** اختياري. يُختار من قائمة أنواع العمل الإضافي.

:::admin
يُبحث عن العطلة بالتاريخ فقط، لا عبر تقويم العطلات الخاص بالموظف. فإذا اشتركت عطلتان في تقويمين في التاريخ نفسه يُستخدم نوع عمل إضافي واحد منهما للجميع -- فأعطهما نوع العمل الإضافي نفسه. وهذا سلوك النظام السابق وقد أُبقي عليه عمداً.
:::

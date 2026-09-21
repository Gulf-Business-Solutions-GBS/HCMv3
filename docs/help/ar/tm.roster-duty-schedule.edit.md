---
helpId: tm.roster-duty-schedule.edit
lang: ar
title: جداول ورديات العمل — إنشاء / تحرير
entity: HCMv3.TimeManagement.RosterDutySchedule
surfaces:
  - angular/src/app/admin/time-management/roster-duty-schedules/create-or-edit-roster-duty-schedule-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.RosterDutySchedules.Create]
---

## Overview

جدول مناوبات: فترة دوام مع نقطة بداية وتقويم عطلات، مخصص لتوليد الدوامات اليومية للمناوبات.

لا يُقرأ حالياً: يأخذ محرك الوقت ونظام الرواتب دوامات الموظفين من فترات الدوام ومجموعات المناوبة، لا من هذه القائمة. ومولّد المناوبات الذي كان سيستخدمها لم يعمل قط في النظام السابق ولم يُنقل.

## DutyPeriod {#RosterDutySchedule.DutyPeriodId}
<!-- alias: loc:DutyPeriod, dom:RosterDutySchedule_DutyPeriodId -->

**ما هو.** فترة الدوام التي يتبعها الجدول.

**القواعد.** إلزامي. يُختار من قائمة فترات الدوام.

## StartPoint {#RosterDutySchedule.StartPoint}
<!-- alias: loc:StartPoint, dom:RosterDutySchedule_StartPoint -->

**ما هو.** يوم الدورة الذي يبدأ منه الجدول.

**مثال.** `1`

**القواعد.** إلزامي. رقم صحيح.

## HolidayCalendar {#RosterDutySchedule.HolidayCalendarId}
<!-- alias: loc:HolidayCalendar, dom:RosterDutySchedule_HolidayCalendarId -->

**ما هو.** تقويم العطلات للجدول.

**القواعد.** اختياري. يُختار من قائمة تقاويم العطلات.

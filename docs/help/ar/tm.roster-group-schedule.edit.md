---
helpId: tm.roster-group-schedule.edit
lang: ar
title: جداول مجموعات الورديات — إنشاء / تحرير
entity: HCMv3.TimeManagement.RosterGroupSchedule
surfaces:
  - angular/src/app/admin/time-management/roster-group-schedules/create-or-edit-roster-group-schedule-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.RosterGroupSchedules.Create]
---

## Overview

فترة الدوام التي تتبعها مجموعة المناوبة لفترة، ومن أين تبدأ في الدورة.

## RosterGroup {#RosterGroupSchedule.RosterGroupId}
<!-- alias: loc:RosterGroup, dom:RosterGroupSchedule_RosterGroupId -->

**ما هو.** مجموعة المناوبة.

**مثال.** الفريق أ

**القواعد.** إلزامي. يُختار من قائمة مجموعات الجداول.

## DutyPeriod {#RosterGroupSchedule.DutyPeriodId}
<!-- alias: loc:DutyPeriod, dom:RosterGroupSchedule_DutyPeriodId -->

**ما هو.** فترة الدوام التي تتبعها المجموعة.

**مثال.** دورة 4 أيام عمل و4 أيام راحة

**القواعد.** إلزامي. يُختار من قائمة فترات الدوام.

## StartPoint {#RosterGroupSchedule.StartPoint}
<!-- alias: loc:StartPoint, dom:RosterGroupSchedule_StartPoint -->

**ما هو.** يوم الدورة الذي تكون فيه المجموعة في تاريخ بداية الجدول.

**مثال.** `5` -- يبدأ الفريق ب في منتصف دورة من 8 أيام.

**لماذا يهم.** إذا كان أكبر من صفر تتبع المجموعة دورة أسابيع فترة الدوام من هذا اليوم. والقيمة 0 تستخدم دوامات أيام الأسبوع في فترة الدوام بدلاً من ذلك.

**القواعد.** إلزامي. رقم صحيح.

---
helpId: tm.roster-daily-duty.edit
lang: ar
title: الدوامات اليومية للورديات — إنشاء / تحرير
entity: HCMv3.TimeManagement.RosterDailyDuty
surfaces:
  - angular/src/app/admin/time-management/roster-daily-duties/create-or-edit-roster-daily-duty-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.RosterDailyDuties.Create]
---

## Overview

دوام يومي في تاريخ ضمن جدول مناوبات.

لا يُقرأ حالياً: يأخذ محرك الوقت ونظام الرواتب دوامات الموظفين من فترات الدوام ومجموعات المناوبة، لا من هذه القائمة. ومولّد المناوبات الذي كان سيستخدمها لم يعمل قط في النظام السابق ولم يُنقل.

## CalendarDate {#RosterDailyDuty.CalendarDate}
<!-- alias: loc:CalendarDate, dom:RosterDailyDuty_CalendarDate -->

**ما هو.** التاريخ.

**مثال.** `2026-04-14`

**القواعد.** إلزامي. تاريخ.

## RosterDutySchedule {#RosterDailyDuty.RosterDutyScheduleId}
<!-- alias: loc:RosterDutySchedule, dom:RosterDailyDuty_RosterDutyScheduleId -->

**ما هو.** جدول المناوبات الذي يتبعه التاريخ.

**القواعد.** إلزامي. يُختار من قائمة جداول ورديات العمل.

## DailyDuty {#RosterDailyDuty.DailyDutyId}
<!-- alias: loc:DailyDuty, dom:RosterDailyDuty_DailyDutyId -->

**ما هو.** الدوام اليومي في ذلك التاريخ.

**القواعد.** إلزامي. يُختار من قائمة الدوامات اليومية.

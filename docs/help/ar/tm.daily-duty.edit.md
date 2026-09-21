---
helpId: tm.daily-duty.edit
lang: ar
title: الدوامات اليومية — إنشاء / تحرير
entity: HCMv3.TimeManagement.DailyDuty
surfaces:
  - angular/src/app/admin/time-management/daily-duties/create-or-edit-daily-duty-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.DailyDuties.Create]
---

## Overview

دوام لقوائم المناوبات (فترات دوام المناوبات والدوامات اليومية للمناوبات)، مع أوقات مناوبته.

لا يُقرأ حالياً: يأخذ محرك الوقت ونظام الرواتب دوامات الموظفين من فترات الدوام ومجموعات المناوبة، لا من هذه القائمة. ومولّد المناوبات الذي كان سيستخدمها لم يعمل قط في النظام السابق ولم يُنقل. ويعرض عارض جدول المناوبات أوقاته عندما يظهر في مناوبة.

## StartTime {#DailyDuty.StartTime}
<!-- alias: loc:StartTime, dom:DailyDuty_StartTime -->

**ما هو.** وقت بدء المناوبة.

**مثال.** `07:00`

**القواعد.** اختياري. وقت من اليوم.

## EndTime {#DailyDuty.EndTime}
<!-- alias: loc:EndTime, dom:DailyDuty_EndTime -->

**ما هو.** وقت انتهاء المناوبة.

**مثال.** `19:00`

**القواعد.** اختياري. وقت من اليوم.

## BreakShift {#DailyDuty.BreakShiftId}
<!-- alias: loc:BreakShift, dom:DailyDuty_BreakShiftId -->

**ما هو.** الاستراحة التي تُؤخذ خلال هذا الدوام.

**القواعد.** اختياري. يُختار من قائمة استراحات الوردية.

## IsNightShift {#DailyDuty.IsNightShift}
<!-- alias: loc:IsNightShift, dom:DailyDuty_IsNightShift -->

**ما هو.** ما إذا كانت المناوبة تنتهي في اليوم التالي.

**القواعد.** نعم أو لا.

## AllowTransStart {#DailyDuty.AllowTransStart}
<!-- alias: loc:AllowTransStart, dom:DailyDuty_AllowTransStart -->

**ما هو.** ما إذا كان يجوز أن تبدأ الإجازة في أيام الراحة لهذا الدوام.

**القواعد.** نعم أو لا.

## AllowTransEnd {#DailyDuty.AllowTransEnd}
<!-- alias: loc:AllowTransEnd, dom:DailyDuty_AllowTransEnd -->

**ما هو.** ما إذا كان يمكن احتساب أيام الراحة لهذا الدوام ضمن الإجازة.

**القواعد.** نعم أو لا.

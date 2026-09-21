---
helpId: tm.break-shift.edit
lang: ar
title: استراحات الوردية — إنشاء / تحرير
entity: HCMv3.TimeManagement.BreakShift
surfaces:
  - angular/src/app/admin/time-management/break-shifts/create-or-edit-break-shift-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.BreakShifts.Create]
---

## Overview

استراحة ضمن الدوام -- للغداء أو الصلاة -- مع وقتي بدايتها ونهايتها. والدوام الذي فيه استراحة يحتسب وقت العمل قبل الاستراحة وبعدها، لا خلالها.

## StartTime {#BreakShift.StartTime}
<!-- alias: loc:StartTime, dom:BreakShift_StartTime -->

**ما هو.** وقت بدء الاستراحة.

**مثال.** `12:00`

**القواعد.** اختياري. وقت من اليوم.

## EndTime {#BreakShift.EndTime}
<!-- alias: loc:EndTime, dom:BreakShift_EndTime -->

**ما هو.** وقت انتهاء الاستراحة.

**مثال.** `12:30`

**القواعد.** اختياري. وقت من اليوم.

## IsPaid {#BreakShift.IsPaid}
<!-- alias: loc:IsPaid, dom:BreakShift_IsPaid -->

**ما هو.** ما إذا كانت الاستراحة مدفوعة.

**لماذا يهم.** لا يُقرأ حالياً: تُستبعد الاستراحة من وقت العمل سواء أُشِّر عليه أم لا.

**القواعد.** نعم أو لا.

## IsBreakStartNextDay {#BreakShift.IsBreakStartNextDay}
<!-- alias: loc:IsBreakStartNextDay, dom:BreakShift_IsBreakStartNextDay -->

**ما هو.** في المناوبة الليلية، ما إذا كانت الاستراحة تبدأ في اليوم التالي.

**مثال.** مؤشَّر عليه لاستراحة الساعة 02:00 في مناوبة 22:00–06:00.

**القواعد.** نعم أو لا.

## IsBreakEndNextDay {#BreakShift.IsBreakEndNextDay}
<!-- alias: loc:IsBreakEndNextDay, dom:BreakShift_IsBreakEndNextDay -->

**ما هو.** ما إذا كانت الاستراحة تنتهي في اليوم التالي.

**لماذا يهم.** أشّر عليه للاستراحة التي تتجاوز منتصف الليل. والاستراحة التي تبدأ في اليوم التالي تُعامل دائماً على أنها تنتهي فيه.

**القواعد.** نعم أو لا.

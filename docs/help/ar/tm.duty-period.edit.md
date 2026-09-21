---
helpId: tm.duty-period.edit
lang: ar
title: فترات الدوام — إنشاء / تحرير
entity: HCMv3.TimeManagement.DutyPeriod
surfaces:
  - angular/src/app/admin/time-management/duty-periods/create-or-edit-duty-period-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.DutyPeriods.Create]
---

## Overview

نمط عمل مكوّن من دوامات، مع تقويم العطلات المرافق له. ويحصل الموظفون على فترة الدوام مباشرة (فترات دوام الموظف) أو عبر مجموعة المناوبة.

ويعمل بإحدى طريقتين: فبدون نقطة بداية في تعيين الموظف يأخذ كل يوم من أيام الأسبوع الدوام المحدد له هنا -- دوام الأحد في كل أحد. ومع نقطة بداية يكون النمط دورة الأسابيع المحددة في **تفاصيل** فترة الدوام، ولا تُستخدم دوامات أيام الأسبوع هنا.

ولا تنطبق الدورات ومجموعات المناوبة إلا إذا كان إعداد الوقت *EnableRosterDuty* مفعّلاً، وإلا فيحصل كل موظف على دوامات أيام الأسبوع في فترة دوامه.

## HolidayCalendar {#DutyPeriod.HolidayCalendarId}
<!-- alias: loc:HolidayCalendar, dom:DutyPeriod_HolidayCalendarId -->

**ما هو.** العطلات الرسمية التي تنطبق على الموظفين في فترة الدوام هذه.

**مثال.** العطلات الرسمية في الكويت

**لماذا يهم.** لا يكون اليوم عطلة رسمية للموظف إلا إذا كان في هذا التقويم.

**القواعد.** اختياري. يُختار من قائمة تقاويم العطلات.

## Sunday {#DutyPeriod.SundayDutyId}
<!-- alias: loc:Sunday, dom:DutyPeriod_SundayDutyId -->

**ما هو.** الدوام يوم الأحد.

**مثال.** الدوام الصباحي

**لماذا يهم.** إذا تُرك فارغاً لا يكون للموظف دوام في ذلك اليوم.

**القواعد.** اختياري. يُختار من قائمة الدوامات.

## Monday {#DutyPeriod.MondayDutyId}
<!-- alias: loc:Monday, dom:DutyPeriod_MondayDutyId -->

**ما هو.** الدوام يوم الاثنين.

**مثال.** الدوام الصباحي

**لماذا يهم.** إذا تُرك فارغاً لا يكون للموظف دوام في ذلك اليوم.

**القواعد.** اختياري. يُختار من قائمة الدوامات.

## Tuesday {#DutyPeriod.TuesdayDutyId}
<!-- alias: loc:Tuesday, dom:DutyPeriod_TuesdayDutyId -->

**ما هو.** الدوام يوم الثلاثاء.

**مثال.** الدوام الصباحي

**لماذا يهم.** إذا تُرك فارغاً لا يكون للموظف دوام في ذلك اليوم.

**القواعد.** اختياري. يُختار من قائمة الدوامات.

## Wednesday {#DutyPeriod.WednesdayDutyId}
<!-- alias: loc:Wednesday, dom:DutyPeriod_WednesdayDutyId -->

**ما هو.** الدوام يوم الأربعاء.

**مثال.** الدوام الصباحي

**لماذا يهم.** إذا تُرك فارغاً لا يكون للموظف دوام في ذلك اليوم.

**القواعد.** اختياري. يُختار من قائمة الدوامات.

## Thursday {#DutyPeriod.ThursdayDutyId}
<!-- alias: loc:Thursday, dom:DutyPeriod_ThursdayDutyId -->

**ما هو.** الدوام يوم الخميس.

**مثال.** الدوام الصباحي

**لماذا يهم.** إذا تُرك فارغاً لا يكون للموظف دوام في ذلك اليوم.

**القواعد.** اختياري. يُختار من قائمة الدوامات.

## Friday {#DutyPeriod.FridayDutyId}
<!-- alias: loc:Friday, dom:DutyPeriod_FridayDutyId -->

**ما هو.** الدوام يوم الجمعة.

**مثال.** الدوام الصباحي

**لماذا يهم.** إذا تُرك فارغاً لا يكون للموظف دوام في ذلك اليوم.

**القواعد.** اختياري. يُختار من قائمة الدوامات.

## Saturday {#DutyPeriod.SaturdayDutyId}
<!-- alias: loc:Saturday, dom:DutyPeriod_SaturdayDutyId -->

**ما هو.** الدوام يوم السبت.

**مثال.** الدوام الصباحي

**لماذا يهم.** إذا تُرك فارغاً لا يكون للموظف دوام في ذلك اليوم.

**القواعد.** اختياري. يُختار من قائمة الدوامات.

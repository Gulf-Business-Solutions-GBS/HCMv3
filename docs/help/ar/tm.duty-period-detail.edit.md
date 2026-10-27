---
helpId: tm.duty-period-detail.edit
lang: ar
title: تفاصيل فترات الدوام — إنشاء / تحرير
entity: HCMv3.TimeManagement.DutyPeriodDetail
surfaces:
  - angular/src/app/admin/time-management/duty-period-details/create-or-edit-duty-period-detail-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.DutyPeriodDetails.Create]
---

## Overview

أسبوع واحد من دورة فترة الدوام. والدورة المكوّنة من N أسابيع تحتاج إلى أسابيع مرقّمة من 1 إلى N، ويأخذ كل يوم من الدورة دوام ذلك الأسبوع وذلك اليوم.

وتحدد **نقطة البداية** للموظف أي يوم من الدورة يبدأ فيه تعيينه، وكل يوم لاحق يتقدم خطوة واحدة في الدورة ثم يعود إلى أولها. ورقم الأسبوع الناقص يوقف محرك الوقت برسالة "wrong week number assigned".

## DutyPeriod {#DutyPeriodDetail.DutyPeriodId}
<!-- alias: loc:DutyPeriod, dom:DutyPeriodDetail_DutyPeriodId -->

**ما هو.** فترة الدوام التي يتبعها هذا الأسبوع.

**مثال.** دورة 4 أيام عمل و4 أيام راحة

**القواعد.** إلزامي. يُختار من قائمة فترات الدوام.

## Week {#DutyPeriodDetail.Week}
<!-- alias: loc:Week, dom:DutyPeriodDetail_Week -->

**ما هو.** رقم الأسبوع في الدورة.

**مثال.** `2`

**لماذا يهم.** يجب أن يكون أكبر من صفر، ولا يُستخدم الرقم إلا مرة واحدة في فترة الدوام.

**القواعد.** إلزامي. رقم صحيح.

## Day1 {#DutyPeriodDetail.Day1DutyId}
<!-- alias: loc:Day1, dom:DutyPeriodDetail_Day1DutyId -->

**ما هو.** الدوام في اليوم 1 من هذا الأسبوع في الدورة.

**مثال.** الدوام الليلي

**القواعد.** اختياري. يُختار من قائمة الدوامات.

## Day2 {#DutyPeriodDetail.Day2DutyId}
<!-- alias: loc:Day2, dom:DutyPeriodDetail_Day2DutyId -->

**ما هو.** الدوام في اليوم 2 من هذا الأسبوع في الدورة.

**مثال.** الدوام الليلي

**القواعد.** اختياري. يُختار من قائمة الدوامات.

## Day3 {#DutyPeriodDetail.Day3DutyId}
<!-- alias: loc:Day3, dom:DutyPeriodDetail_Day3DutyId -->

**ما هو.** الدوام في اليوم 3 من هذا الأسبوع في الدورة.

**مثال.** الدوام الليلي

**القواعد.** اختياري. يُختار من قائمة الدوامات.

## Day4 {#DutyPeriodDetail.Day4DutyId}
<!-- alias: loc:Day4, dom:DutyPeriodDetail_Day4DutyId -->

**ما هو.** الدوام في اليوم 4 من هذا الأسبوع في الدورة.

**مثال.** الدوام الليلي

**القواعد.** اختياري. يُختار من قائمة الدوامات.

## Day5 {#DutyPeriodDetail.Day5DutyId}
<!-- alias: loc:Day5, dom:DutyPeriodDetail_Day5DutyId -->

**ما هو.** الدوام في اليوم 5 من هذا الأسبوع في الدورة.

**مثال.** الدوام الليلي

**القواعد.** اختياري. يُختار من قائمة الدوامات.

## Day6 {#DutyPeriodDetail.Day6DutyId}
<!-- alias: loc:Day6, dom:DutyPeriodDetail_Day6DutyId -->

**ما هو.** الدوام في اليوم 6 من هذا الأسبوع في الدورة.

**مثال.** راحة أسبوعية

**القواعد.** اختياري. يُختار من قائمة الدوامات.

## Day7 {#DutyPeriodDetail.Day7DutyId}
<!-- alias: loc:Day7, dom:DutyPeriodDetail_Day7DutyId -->

**ما هو.** الدوام في اليوم 7 من هذا الأسبوع في الدورة.

**مثال.** راحة أسبوعية

**القواعد.** اختياري. يُختار من قائمة الدوامات.

---
helpId: tm.monthly-time-register-detail.edit
lang: ar
title: تفاصيل سجلات الوقت الشهرية — إنشاء / تحرير
entity: HCMv3.TimeManagement.MonthlyTimeRegisterDetail
surfaces:
  - angular/src/app/admin/time-management/monthly-time-register-details/create-or-edit-monthly-time-register-detail-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.MonthlyTimeRegisterDetails.Create]
---

## Overview

قيمة نوع وقت واحد في السجل الشهري.

لا تُكتب حالياً: لا توجد عملية تملأ هذه السجلات ولا شيء يقرؤها. ويعمل نظام الرواتب والتقارير من سجل الوقت اليومي.

## MonthlyTimeRegister {#MonthlyTimeRegisterDetail.MonthlyTimeRegisterId}
<!-- alias: loc:MonthlyTimeRegister, dom:MonthlyTimeRegisterDetail_MonthlyTimeRegisterId -->

**ما هو.** السجل الشهري الذي يتبعه هذا السطر.

**القواعد.** إلزامي. يُختار من قائمة سجلات الوقت الشهرية.

## TimeType {#MonthlyTimeRegisterDetail.TimeTypeId}
<!-- alias: loc:TimeType, dom:MonthlyTimeRegisterDetail_TimeTypeId -->

**ما هو.** نوع الوقت الذي تخصه القيمة.

**القواعد.** إلزامي. يُختار من قائمة أنواع الوقت.

## TimeValue {#MonthlyTimeRegisterDetail.TimeValue}
<!-- alias: loc:TimeValue, dom:MonthlyTimeRegisterDetail_TimeValue -->

**ما هو.** القيمة.

**مثال.** `2.5`

**القواعد.** اختياري.

---
helpId: tm.yearly-time-register-detail.edit
lang: ar
title: تفاصيل سجلات الوقت السنوية — إنشاء / تحرير
entity: HCMv3.TimeManagement.YearlyTimeRegisterDetail
surfaces:
  - angular/src/app/admin/time-management/yearly-time-register-details/create-or-edit-yearly-time-register-detail-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.YearlyTimeRegisterDetails.Create]
---

## Overview

قيمة نوع وقت واحد في السجل السنوي.

لا تُكتب حالياً: لا توجد عملية تملأ هذه السجلات ولا شيء يقرؤها. ويعمل نظام الرواتب والتقارير من سجل الوقت اليومي.

## YearlyTimeRegister {#YearlyTimeRegisterDetail.YearlyTimeRegisterId}
<!-- alias: loc:YearlyTimeRegister, dom:YearlyTimeRegisterDetail_YearlyTimeRegisterId -->

**ما هو.** السجل السنوي الذي يتبعه هذا السطر.

**القواعد.** إلزامي. يُختار من قائمة سجلات الوقت السنوية.

## TimeType {#YearlyTimeRegisterDetail.TimeTypeId}
<!-- alias: loc:TimeType, dom:YearlyTimeRegisterDetail_TimeTypeId -->

**ما هو.** نوع الوقت الذي تخصه القيمة.

**القواعد.** إلزامي. يُختار من قائمة أنواع الوقت.

## TimeValue {#YearlyTimeRegisterDetail.TimeValue}
<!-- alias: loc:TimeValue, dom:YearlyTimeRegisterDetail_TimeValue -->

**ما هو.** القيمة.

**مثال.** `2.5`

**القواعد.** اختياري.

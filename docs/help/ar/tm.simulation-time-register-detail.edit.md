---
helpId: tm.simulation-time-register-detail.edit
lang: ar
title: تفاصيل سجلات الوقت التجريبية — إنشاء / تحرير
entity: HCMv3.TimeManagement.SimulationTimeRegisterDetail
surfaces:
  - angular/src/app/admin/time-management/simulation-time-register-details/create-or-edit-simulation-time-register-detail-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.SimulationTimeRegisterDetails.Create]
---

## Overview

قيمة نوع وقت واحد ليوم محاكاة.

لا تُكتب حالياً: لا توجد عملية تملأ هذه السجلات ولا شيء يقرؤها. ويعمل نظام الرواتب والتقارير من سجل الوقت اليومي.

## SimulationTimeRegister {#SimulationTimeRegisterDetail.SimulationTimeRegisterId}
<!-- alias: loc:SimulationTimeRegister, dom:SimulationTimeRegisterDetail_SimulationTimeRegisterId -->

**ما هو.** يوم المحاكاة الذي يتبعه هذا السطر.

**القواعد.** إلزامي. يُختار من قائمة سجلات الوقت التجريبية.

## TimeType {#SimulationTimeRegisterDetail.TimeTypeId}
<!-- alias: loc:TimeType, dom:SimulationTimeRegisterDetail_TimeTypeId -->

**ما هو.** نوع الوقت الذي تخصه القيمة.

**القواعد.** إلزامي. يُختار من قائمة أنواع الوقت.

## TimeValue {#SimulationTimeRegisterDetail.TimeValue}
<!-- alias: loc:TimeValue, dom:SimulationTimeRegisterDetail_TimeValue -->

**ما هو.** القيمة.

**مثال.** `2.5`

**القواعد.** اختياري.

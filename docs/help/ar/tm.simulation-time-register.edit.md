---
helpId: tm.simulation-time-register.edit
lang: ar
title: سجلات الوقت التجريبية — إنشاء / تحرير
entity: HCMv3.TimeManagement.SimulationTimeRegister
surfaces:
  - angular/src/app/admin/time-management/simulation-time-registers/create-or-edit-simulation-time-register-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.SimulationTimeRegisters.Create]
---

## Overview

يوم من تشغيل تجريبي لمحرك الوقت.

لا تُكتب حالياً: لا توجد عملية تملأ هذه السجلات ولا شيء يقرؤها. ويعمل نظام الرواتب والتقارير من سجل الوقت اليومي.

## Employee {#SimulationTimeRegister.EmployeeId}
<!-- alias: loc:Employee, dom:SimulationTimeRegister_EmployeeId -->

**ما هو.** الموظف.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.

## DayDate {#SimulationTimeRegister.DayDate}
<!-- alias: loc:DayDate, dom:SimulationTimeRegister_DayDate -->

**ما هو.** اليوم.

**مثال.** `2026-04-14`

**القواعد.** إلزامي. تاريخ.

## DutyPeriod {#SimulationTimeRegister.DutyPeriodId}
<!-- alias: loc:DutyPeriod, dom:SimulationTimeRegister_DutyPeriodId -->

**ما هو.** فترة الدوام المطبقة.

**القواعد.** اختياري. يُختار من قائمة فترات الدوام.

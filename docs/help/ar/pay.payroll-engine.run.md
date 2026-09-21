---
helpId: pay.payroll-engine.run
lang: ar
title: Payroll Engine — Run
entity: HCMv3.PayrollManagement.Dto.RunPayrollEngineInput
surfaces:
  - angular/src/app/admin/payroll-management/payroll-engine/payroll-engine.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.PayrollEngine]
---

## Overview

يشغّل الرواتب لمنطقة رواتب أو لموظفين محددين، ويعرض سجل التشغيلات.

ويُرفض التشغيل ما لم يكن لمنطقة الرواتب سجل ضبط، ووُجد في تقويم الرواتب سطر لشهر وسنة تاريخ الدفع. ويحتاج التشغيل **العادي** أيضاً إلى أن تكون الفترة مفتوحة لتشغيل الرواتب وأن تكون نهاية شهرها هي تاريخ الدفع. أما التشغيل **خارج الدورة** فيحتاج إلى أن تكون الفترة مقفلة وإلى رمز موظف واحد على الأقل.

ويعمل التشغيل في الخلفية، ويظهر تقدمه ونتيجته في سجل التشغيلات أدناه.

## PayrollAreaId {#RunPayrollEngineInput.PayrollAreaName}
<!-- alias: loc:PayrollAreaId, dom:PayrollEngine_PayrollArea -->

**ما هو.** منطقة الرواتب المطلوب تشغيلها.

**مثال.** Kuwait Monthly

**لماذا يهم.** بدون منطقة رواتب لا يُعالَج أحد.

**القواعد.** إلزامي. الحد الأقصى 256 حرفاً.

## PayrollRunType {#RunPayrollEngineInput.PayrollRunType}
<!-- alias: loc:PayrollRunType, dom:PayrollEngine_RunType -->

**ما هو.** تشغيل شهري عادي، أو تشغيل خارج الدورة لموظفين معيّنين.

**مثال.** عادي

**لماذا يهم.** يحتاج التشغيل العادي إلى أن تكون الفترة مفتوحة لتشغيل الرواتب، ويحتاج التشغيل خارج الدورة إلى أن تكون مقفلة.

**القواعد.** إلزامي. إحدى القيم: اعتيادي، خارج الدورة.

## EmployeeCode {#RunPayrollEngineInput.EmployeeCodes}
<!-- alias: loc:EmployeeCode, dom:PayrollEngine_EmployeeCodes -->

**ما هو.** رموز الموظفين المطلوب تشغيلهم، مفصولة بفواصل. وتركها فارغة يشغّل كل الموظفين النشطين في منطقة الرواتب.

**مثال.** `1001, 1002`

**لماذا يهم.** إلزامي للتشغيل خارج الدورة.

**القواعد.** اختياري. الحد الأقصى 4000 حرف.

:::admin
ما لم يكن إعداد الوقت *EnableTimeEngineAutorun* مفعّلاً، يُشغَّل الموظفون المحددون حتى لو كانوا قد تركوا العمل أو كانوا في منطقة رواتب أخرى، ويُتحقق من كل منهم مقابل سجل الضبط لمنطقته.
:::

## PayInDate {#RunPayrollEngineInput.PayInDate}
<!-- alias: loc:PayInDate, dom:PayrollEngine_PayInDate -->

**ما هو.** التاريخ الذي يخصه الأجر -- آخر يوم في شهر الرواتب.

**مثال.** `2026-04-30`

**لماذا يهم.** في التشغيل العادي يجب أن يكون نهاية شهر الفترة في سجل الضبط.

**القواعد.** إلزامي. تاريخ.

## OffCycleReason {#RunPayrollEngineInput.OffCycleReason}
<!-- alias: loc:OffCycleReason, dom:PayrollEngine_OffCycleReason -->

**ما هو.** سبب التشغيل خارج الدورة: نهاية الخدمة أو الإجازة المقدمة أو صرف الإجازة نقداً.

**مثال.** الإجازة المقدمة

**لماذا يهم.** *الإجازة المقدمة* تلتقط الإجازات المدفوعة المضبوطة على *الدفع مقدماً* التي تاريخ دفعها في الشهر.

**القواعد.** إلزامي. الحد الأقصى 256 حرفاً.

## OffCyclePaymentDate {#RunPayrollEngineInput.OffCyclePaymentDate}
<!-- alias: loc:OffCyclePaymentDate, dom:PayrollEngine_OffCyclePaymentDate -->

**ما هو.** تاريخ صرف الدفعة خارج الدورة.

**مثال.** `2026-05-05`

**لماذا يهم.** إلزامي للتشغيل خارج الدورة، ويجب أن يقع بعد الفترة المقفلة. ثم يُسجَّل التشغيل في نهاية شهر هذا التاريخ -- فـ5 مايو يُسجَّل في 31 مايو.

**القواعد.** اختياري. تاريخ.

:::admin
إذا كان إعداد الوقت *EnableTimeEngineAutorun* مفعّلاً يُتجاهل هذا التاريخ ويُسجَّل التشغيل في الشهر الذي يلي الفترة المقفلة.
:::

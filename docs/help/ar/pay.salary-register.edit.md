---
helpId: pay.salary-register.edit
lang: ar
title: سجلات الرواتب — إنشاء / تحرير
entity: HCMv3.PayrollManagement.SalaryRegister
surfaces:
  - angular/src/app/admin/payroll-management/salary-registers/create-or-edit-salary-register-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.SalaryRegisters.Create]
---

## Overview

كشف راتب الموظف لشهر واحد وتشغيل واحد، كما احتسبه نظام الرواتب. يكتبه محرك الرواتب، وأسطره هي تفاصيل سجل الرواتب.

ولا يمكن تعديل كشف الراتب **المقفل** ولا حذفه. وإذا كان إعداد الرواتب *إقفال سجل الرواتب عند إقفال الرواتب* مفعّلاً، فإن إقفال الفترة يُقفل كشوف الرواتب العادية لذلك الشهر، وفتحها لتشغيل الرواتب يُقفل كشوفه خارج الدورة.

## EmployeeId {#SalaryRegister.EmployeeId}
<!-- alias: loc:EmployeeId, dom:SalaryRegister_EmployeeId -->

**ما هو.** الموظف الذي يخصه كشف الراتب.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.

## ForPeriod {#SalaryRegister.ForPeriod}
<!-- alias: loc:ForPeriod, dom:SalaryRegister_ForPeriod -->

**ما هو.** الشهر الذي يخصه الأجر.

**مثال.** `2026-03-31`

**لماذا يهم.** في إعادة الاحتساب بأثر رجعي يكون هذا شهراً أسبق من **في الفترة**.

**القواعد.** إلزامي. تاريخ.

## InPeriod {#SalaryRegister.InPeriod}
<!-- alias: loc:InPeriod, dom:SalaryRegister_InPeriod -->

**ما هو.** الشهر الذي يُصرف فيه الأجر -- تاريخ الدفع للتشغيل.

**مثال.** `2026-04-30`

**القواعد.** إلزامي. تاريخ.

## PayrollRunType {#SalaryRegister.PayrollRunType}
<!-- alias: loc:PayrollRunType, dom:SalaryRegister_PayrollRunType -->

**ما هو.** ما إذا كان كشف الراتب ناتجاً عن تشغيل عادي أو خارج الدورة.

**مثال.** عادي

**القواعد.** إلزامي. إحدى القيم: اعتيادي، خارج الدورة.

## PayrollResultType {#SalaryRegister.PayrollResultType}
<!-- alias: loc:PayrollResultType, dom:SalaryRegister_PayrollResultType -->

**ما هو.** أي نتيجة هذه لشهرها.

**مثال.** الحالي

**لماذا يهم.** يكتب التشغيل العادي *الحالي*. وعندما يُعاد احتساب الشهر نفسه في تشغيل لاحق، تصبح كشوف ذلك الشهر الأقدم *الأخير* (أحدثها قبل ذلك) و*الأرشيف* (البقية).

**القواعد.** إلزامي. إحدى القيم: الحالي، الأخير، الأرشيف.

## PayrollReasonTypeId {#SalaryRegister.PayrollReasonTypeId}
<!-- alias: loc:PayrollReasonTypeId, dom:SalaryRegister_PayrollReasonTypeId -->

**ما هو.** سبب التشغيل خارج الدورة، لكشف الراتب خارج الدورة.

**مثال.** نهاية الخدمة

**لماذا يهم.** فارغ في كشوف الرواتب العادية.

**القواعد.** إلزامي. يُختار من قائمة أنواع أسباب الرواتب.

## OffCyclePaymentDate {#SalaryRegister.OffCyclePaymentDate}
<!-- alias: loc:OffCyclePaymentDate, dom:SalaryRegister_OffCyclePaymentDate -->

**ما هو.** تاريخ الدفع للتشغيل خارج الدورة.

**مثال.** `2026-05-05`

**القواعد.** اختياري. تاريخ.

## Locked {#SalaryRegister.Locked}
<!-- alias: loc:Locked, dom:SalaryRegister_Locked -->

**ما هو.** ما إذا كان كشف الراتب مقفلاً.

**لماذا يهم.** لا يمكن تعديل كشف الراتب المقفل وأسطره ولا حذفها.

**القواعد.** نعم أو لا.

---
helpId: pay.simulation-salary-register.edit
lang: ar
title: سجلات الرواتب التجريبية — إنشاء / تحرير
entity: HCMv3.PayrollManagement.SimulationSalaryRegister
surfaces:
  - angular/src/app/admin/payroll-management/simulation-salary-registers/create-or-edit-simulation-salary-register-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.SimulationSalaryRegisters.Create]
---

## Overview

كشف راتب من محاكاة -- تشغيل تجريبي يبيّن ما سيصرفه نظام الرواتب دون صرفه. وتُحفظ كشوف المحاكاة منفصلة عن الكشوف الفعلية ولا تُقفل أبداً.

يفتح خيار التفاصيل على أي كشف في القائمة صفحته الخاصة: الموظف والفترات ونوع التشغيل ونوع النتيجة في الأعلى مع زر التحرير، وتحتها بنود أنواع الأجور، ويمكن إضافتها وتعديلها وحذفها من هناك.

## EmployeeId {#SimulationSalaryRegister.EmployeeId}
<!-- alias: loc:EmployeeId, dom:SimulationSalaryRegister_EmployeeId -->

**ما هو.** الموظف الذي يخصه كشف الراتب.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.

## ForPeriod {#SimulationSalaryRegister.ForPeriod}
<!-- alias: loc:ForPeriod, dom:SimulationSalaryRegister_ForPeriod -->

**ما هو.** الشهر الذي يخصه الأجر.

**مثال.** `2026-03-31`

**لماذا يهم.** في إعادة الاحتساب بأثر رجعي يكون هذا شهراً أسبق من **في الفترة**.

**القواعد.** إلزامي. تاريخ.

## InPeriod {#SimulationSalaryRegister.InPeriod}
<!-- alias: loc:InPeriod, dom:SimulationSalaryRegister_InPeriod -->

**ما هو.** الشهر الذي يُصرف فيه الأجر -- تاريخ الدفع للتشغيل.

**مثال.** `2026-04-30`

**القواعد.** إلزامي. تاريخ.

## PayrollRunType {#SimulationSalaryRegister.PayrollRunType}
<!-- alias: loc:PayrollRunType, dom:SimulationSalaryRegister_PayrollRunType -->

**ما هو.** ما إذا كان كشف الراتب ناتجاً عن تشغيل عادي أو خارج الدورة.

**مثال.** عادي

**القواعد.** إلزامي. إحدى القيم: اعتيادي، خارج الدورة.

## PayrollResultType {#SimulationSalaryRegister.PayrollResultType}
<!-- alias: loc:PayrollResultType, dom:SimulationSalaryRegister_PayrollResultType -->

**ما هو.** أي نتيجة هذه لشهرها.

**مثال.** الحالي

**لماذا يهم.** يكتب التشغيل العادي *الحالي*. وعندما يُعاد احتساب الشهر نفسه في تشغيل لاحق، تصبح كشوف ذلك الشهر الأقدم *الأخير* (أحدثها قبل ذلك) و*الأرشيف* (البقية).

**القواعد.** إلزامي. إحدى القيم: الحالي، الأخير، الأرشيف.

## PayrollReasonTypeId {#SimulationSalaryRegister.PayrollReasonTypeId}
<!-- alias: loc:PayrollReasonTypeId, dom:SimulationSalaryRegister_PayrollReasonTypeId -->

**ما هو.** سبب التشغيل خارج الدورة، لكشف الراتب خارج الدورة.

**مثال.** نهاية الخدمة

**لماذا يهم.** فارغ في كشوف الرواتب العادية.

**القواعد.** إلزامي. يُختار من قائمة أنواع أسباب الرواتب.

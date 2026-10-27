---
helpId: pa.employee-non-payroll-trans.edit
lang: ar
title: معاملات الموظف خارج الرواتب — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeNonPayrollTrans
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/non-payroll-trans/create-or-edit-employee-non-payroll-tran-modal.component.html
audience: [admin]
permissions: []
---

## Overview

دفعة تُصرف للموظف خارج الرواتب -- كمكافأة تُدفع بشيك.

لا تصرفها الرواتب. لكن احتساب متوسط المكافأة يحتسب المبالغ المصروفة خارج الرواتب لنوع أجر المكافأة ضمن الفترة التي يحسب متوسطها، وتظهر في تقريري المكافآت والمعاملات خارج الرواتب.

## WageTypeId {#EmployeeNonPayrollTrans.WageTypeId}
<!-- alias: loc:WageTypeId, dom:EmployeeNonPayrollTran_WageTypeId -->

**ما هو.** عنصر الأجر الذي تُسجَّل عليه الدفعة.

**مثال.** مكافأة سنوية

**القواعد.** إلزامي. يُختار من قائمة أنواع الأجور.

:::admin
تُدار من إدارة الرواتب ← أنواع الأجور.
:::

## PaymentDate {#EmployeeNonPayrollTrans.PaymentDate}
<!-- alias: loc:PaymentDate, dom:EmployeeNonPayrollTran_PaymentDate -->

**ما هو.** تاريخ صرف الدفعة.

**مثال.** 15 مارس 2026

**القواعد.** إلزامي. تاريخ.

## Amount {#EmployeeNonPayrollTrans.Amount}
<!-- alias: loc:Amount, dom:EmployeeNonPayrollTran_Amount -->

**ما هو.** المبلغ المصروف.

**مثال.** 1000.000

**القواعد.** إلزامي. رقم.

## CurrencyId {#EmployeeNonPayrollTrans.CurrencyId}
<!-- alias: loc:CurrencyId, dom:EmployeeNonPayrollTran_CurrencyId -->

**ما هو.** عملة الدفعة.

**مثال.** KWD

**القواعد.** اختياري. يُختار من قائمة العملات.

## PayableBy {#EmployeeNonPayrollTrans.PayableBy}
<!-- alias: loc:PayableBy, dom:EmployeeNonPayrollTran_PayableBy -->

**ما هو.** الجهة التي تدفعها، كنص حر.

**مثال.** مالية المكتب الرئيسي

**لماذا يهم.** يظهر في تقرير المعاملات خارج الرواتب.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## PaymentMethod {#EmployeeNonPayrollTrans.PaymentMethod}
<!-- alias: loc:PaymentMethod, dom:EmployeeNonPayrollTran_PaymentMethod -->

**ما هو.** طريقة الدفع، كنص حر.

**مثال.** شيك

**لماذا يهم.** يظهر في تقرير المعاملات خارج الرواتب.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## IsLocked {#EmployeeNonPayrollTrans.IsLocked}
<!-- alias: loc:IsLocked, dom:EmployeeNonPayrollTran_IsLocked -->

**ما هو.** يحدد السجل مقفلاً.

**لماذا يهم.** يظهر في تقرير المعاملات خارج الرواتب، ولا يمنع تعديل السجل.

**القواعد.** نعم أو لا.

## NotificationSent {#EmployeeNonPayrollTrans.NotificationSent}
<!-- alias: loc:NotificationSent, dom:EmployeeNonPayrollTran_NotificationSent -->

**ما هو.** يحدد أن الموظف قد أُبلغ.

**لماذا يهم.** يُسجَّل فقط، ولا يرسله أو يقرؤه شيء حالياً.

**القواعد.** نعم أو لا.

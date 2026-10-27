---
helpId: pa.employee-payment.edit
lang: ar
title: مدفوعات الموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeePayment
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/payments/create-or-edit-employee-payment-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeePayments.Create]
---

## Overview

دفعة أو اقتطاع لمرة واحدة للموظف في تاريخ معين -- كمكافأة أو عمولة أو بدل منفرد. وتصرف الرواتب الدفعات التي يقع تاريخها في الفترة التي تعالجها.

كما تنشئ الرواتب سجلات دفعات بنفسها للدفعات المقدمة وترحيل صافي الراتب، وتنشئها أيضاً دفعات الموظفين المجمعة المعتمدة.

## WageTypeId {#EmployeePayment.WageTypeId}
<!-- alias: loc:WageTypeId, dom:EmployeePayment_WageTypeId -->

**ما هو.** عنصر الأجر المدفوع.

**مثال.** مكافأة أداء

**القواعد.** إلزامي. يُختار من قائمة أنواع الأجور.

:::admin
تُدار من إدارة الرواتب ← أنواع الأجور.
:::

## PaymentDate {#EmployeePayment.PaymentDate}
<!-- alias: loc:PaymentDate, dom:EmployeePayment_PaymentDate -->

**ما هو.** تاريخ استحقاق الدفعة.

**مثال.** 30 سبتمبر 2026

**لماذا يهم.** تُدرج الرواتب الدفعة في المسيرة التي تغطي فترتها هذا التاريخ.

**القواعد.** إلزامي. تاريخ.

## Amount {#EmployeePayment.Amount}
<!-- alias: loc:Amount, dom:EmployeePayment_Amount -->

**ما هو.** المبلغ، أو عدد الأيام أو الساعات، حسب نوع الوحدة.

**مثال.** 250.000

**القواعد.** إلزامي. رقم.

## CurrencyId {#EmployeePayment.CurrencyId}
<!-- alias: loc:CurrencyId, dom:EmployeePayment_CurrencyId -->

**ما هو.** عملة الدفعة.

**مثال.** KWD

**القواعد.** اختياري. يُختار من قائمة العملات.

## UnitType {#EmployeePayment.UnitType}
<!-- alias: loc:UnitType, dom:EmployeePayment_UnitType -->

**ما هو.** هل المقدار مبلغ أم أيام أم ساعات.

**مثال.** مبلغ

**لماذا يهم.** يُصرف المبلغ كما أُدخل. أما الأيام والساعات فتُحوَّل إلى أيام وتُصرف بمعدل الموظف لأنواع الأجور المشمولة بالأجر الجزئي؛ وتُحتسب الساعات على أساس ساعات العمل الافتراضية في اليوم.

**القواعد.** إلزامي. إحدى القيم: المبلغ، اليوم، ساعة.

## CostCenterId {#EmployeePayment.CostCenterId}
<!-- alias: loc:CostCenterId, dom:EmployeePayment_CostCenterId -->

**ما هو.** مركز تكلفة يُسجَّل مع الدفعة.

**مثال.** CC-100 المالية

**لماذا يهم.** يُسجَّل فقط، ولا تقرؤه قيود التكلفة حالياً.

**القواعد.** اختياري. يُختار من قائمة مراكز التكلفة.
